using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Domain.Enums;
using GestorPOS.Infrastructure.Persistence;
using GestorPOS.Infrastructure.Suscripcion;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Admin;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentAdminContext _currentAdmin;
    private readonly IFluxoService _fluxo;

    public AdminService(AppDbContext db, IPasswordHasher passwordHasher, ICurrentAdminContext currentAdmin, IFluxoService fluxo)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentAdmin = currentAdmin;
        _fluxo = fluxo;
    }

    public async Task<TenantResumenDto> CrearNegocioAsync(CrearNegocioRequest request, CancellationToken ct = default)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var emailEnUso = await _db.Usuarios.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == emailNormalizado, ct);
        if (emailEnUso)
            throw new AppException("Ya existe una cuenta registrada con ese email.");

        var slug = GenerarSlug(request.NombreNegocio);
        var slugEnUso = await _db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == slug, ct);
        if (slugEnUso)
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

        // La suscripción en Fluxo se intenta ANTES de tocar la base de GestorPOS: si Fluxo la
        // rechaza (p. ej. el email no tiene cuenta real en Mercado Pago), no tiene sentido crear
        // acá un negocio sin ninguna forma de cobrarle — IniciarSuscripcionAsync tira AppException
        // en ese caso, y no queda nada a medio crear.
        var (nombrePila, apellido) = SepararNombreApellido(request.NombreAdmin);
        var cardToken = string.IsNullOrWhiteSpace(request.CardToken) ? null : request.CardToken.Trim();
        if (cardToken is not null && request.MpPlanId is null)
            throw new AppException("Para cobrar con tarjeta hay que elegir un plan.");

        // Primer pago por fuera de Mercado Pago (efectivo del vendedor o transferencia al operador): la suscripción
        // mensual (tarjeta o link) es obligatoria igual y arranca un período después.
        var primerPago = ParsearPrimerPago(request.PrimerPago);
        if (primerPago is not null)
        {
            if (request.MpPlanId is null)
                throw new AppException("Para registrar el primer pago hay que elegir un plan.");
            if (request.PrimerPagoMonto is not > 0)
                throw new AppException("Ingresá el monto del primer pago.");
        }

        var fluxo = await _fluxo.IniciarSuscripcionAsync(
            nombrePila, apellido, emailNormalizado, request.MpPlanId, cardToken, primerPago is not null, ct);

        var tenant = Tenant.Crear(request.NombreNegocio, slug);

        // Un Vendedor solo se atribuye negocios a sí mismo, nunca a otro vendedor (aunque lo mande
        // en el body); el Operador sí puede elegir a quién atribuírselo, o dejarlo sin vendedor.
        var vendedorId = _currentAdmin.EsOperador ? request.VendedorId : _currentAdmin.AdminId;
        tenant.AsignarVendedor(vendedorId);
        tenant.AsignarFluxo(fluxo.ClienteId, fluxo.SuscripcionId);

        _db.Tenants.Add(tenant);

        // Todo negocio arranca con estos tres medios de pago — "Efectivo" queda protegido
        // porque CajaService lo usa por nombre para calcular el efectivo esperado en caja.
        _db.MediosPago.Add(MedioPagoConfiguracion.Crear(tenant.Id, "Efectivo", esProtegido: true));
        _db.MediosPago.Add(MedioPagoConfiguracion.Crear(tenant.Id, "Tarjeta"));
        _db.MediosPago.Add(MedioPagoConfiguracion.Crear(tenant.Id, "Otro"));

        var passwordHash = _passwordHasher.Hash(request.Password);
        var admin = Usuario.Crear(tenant.Id, request.NombreAdmin, emailNormalizado, passwordHash, RolUsuario.Admin);
        _db.Usuarios.Add(admin);

        PagoManual? pago = null;
        if (primerPago is not null)
        {
            var hoyAr = DateOnly.FromDateTime(CalculadoraComisiones.AHoraArgentina(DateTime.UtcNow));
            pago = PagoManual.Registrar(
                tenant.Id, request.PrimerPagoMonto!.Value, primerPago.Value, hoyAr, request.PrimerPagoNota,
                _currentAdmin.AdminId, await NombreDeAdminActualAsync(ct), _currentAdmin.Rol);
            _db.PagosManuales.Add(pago);

            // Se activa cuando el primer pago está confirmado Y la suscripción autorizada; si falta algo, espera.
            var suscripcionAutorizada = fluxo.Estado == "authorized";
            if (!(pago.Estado == EstadoPagoManual.Confirmado && suscripcionAutorizada))
                tenant.EsperarActivacion();
        }

        var detalleAlta = new List<string>
        {
            request.MpPlanId is null ? "Sin plan"
                : primerPago is not null ? $"Primer pago en {primerPago.Value.ToString().ToLowerInvariant()}, suscripción {(cardToken is not null ? "con tarjeta" : "con link de pago")}"
                : cardToken is not null ? "Cobro con tarjeta" : "Cobro con link de pago",
        };
        if (_currentAdmin.EsOperador && vendedorId is not null) detalleAlta.Add("con vendedor asignado");
        await RegistrarMovimientoAsync("negocio.alta", "negocio", tenant.Id, tenant.Nombre, string.Join(" · ", detalleAlta), ct);

        await _db.SaveChangesAsync(ct);

        var vendedorNombre = vendedorId is null
            ? null
            : await _db.AdminUsuarios.Where(v => v.Id == vendedorId).Select(v => v.Nombre).FirstOrDefaultAsync(ct);

        return new TenantResumenDto(
            tenant.Id, tenant.Nombre, tenant.Slug, tenant.Activo, tenant.FechaCreacion, vendedorId, vendedorNombre,
            tenant.FluxoClienteId, tenant.FluxoSuscripcionId, fluxo.InitPoint, fluxo.Estado,
            FormaPrimerPago: pago?.Metodo.ToString(), PagoManualEstado: pago?.Estado.ToString(),
            PagoManualMonto: pago?.Monto, PendienteActivacion: tenant.PendienteActivacion);
    }

    private static MetodoPagoManual? ParsearPrimerPago(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        // Al dar de alta solo se puede recibir en efectivo (lo recibe quien da el alta) o por transferencia (al operador).
        return Enum.TryParse<MetodoPagoManual>(valor, true, out var metodo) && metodo is MetodoPagoManual.Efectivo or MetodoPagoManual.Transferencia
            ? metodo
            : throw new AppException("El primer pago tiene que ser en efectivo o por transferencia.");
    }

    private async Task<string> NombreDeAdminActualAsync(CancellationToken ct) =>
        await _db.AdminUsuarios.Where(a => a.Id == _currentAdmin.AdminId).Select(a => a.Nombre).FirstOrDefaultAsync(ct) ?? "Admin";

    public async Task<TenantResumenDto> ConfirmarPagoAsync(Guid tenantId, ConfirmarPagoRequest request, CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(tenantId, ct);
        var pago = await _db.PagosManuales
            .Where(p => p.TenantId == tenantId && p.Estado == EstadoPagoManual.Pendiente)
            .OrderBy(p => p.FechaRegistroUtc)
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException("Este negocio no tiene un pago pendiente de confirmar.");

        if (!Enum.TryParse<MetodoPagoManual>(request.Metodo, true, out var metodo))
            throw new AppException("La forma de pago no es válida.");
        var monto = request.Monto ?? pago.Monto;
        if (monto <= 0)
            throw new AppException("El monto tiene que ser mayor a cero.");
        var hoy = DateOnly.FromDateTime(CalculadoraComisiones.AHoraArgentina(DateTime.UtcNow));
        var fecha = request.FechaRecepcion ?? hoy;
        if (fecha > hoy)
            throw new AppException("La fecha de recepción no puede ser mayor a hoy.");
        if (request.Nota is { Length: > 300 })
            throw new AppException("La nota no puede superar los 300 caracteres.");

        var adminNombre = await NombreDeAdminActualAsync(ct);
        pago.Confirmar(monto, metodo, fecha, request.Nota, _currentAdmin.AdminId, adminNombre);
        await RegistrarMovimientoAsync("pago.confirmado", "negocio", tenant.Id, tenant.Nombre,
            $"{metodo} · ${Math.Round(monto).ToString("N0", new System.Globalization.CultureInfo("es-AR"))}", ct);

        // Con el pago confirmado, el negocio se activa si la suscripción ya está autorizada; si falta que el
        // cliente abra el link, se activa solo apenas lo haga.
        if (tenant.PendienteActivacion && tenant.FluxoSuscripcionId is { } suscripcionId)
        {
            var estados = await _fluxo.ObtenerConfirmacionesAsync([suscripcionId], ct);
            if (estados?.GetValueOrDefault(suscripcionId)?.Confirmada == true)
            {
                tenant.ConfirmarActivacion();
                await RegistrarMovimientoAsync("negocio.activado", "negocio", tenant.Id, tenant.Nombre,
                    "Pago confirmado y suscripción autorizada", ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return await ArmarResumenAsync(tenant, ct);
    }

    public async Task<IReadOnlyList<FluxoPlanDto>> ListarPlanesFluxoAsync(CancellationToken ct = default)
    {
        var planes = await _fluxo.ListarPlanesAsync(ct);
        return planes
            .Select(p => new FluxoPlanDto(p.MpPlanId, p.Nombre, p.Monto, p.Moneda, p.TipoFrecuencia, p.Frecuencia, p.DiasGratis, p.MontoPrimerCobro))
            .ToList();
    }

    // Deja anotada la acción para el historial de movimientos. Se agrega al mismo SaveChanges de la acción,
    // así queda registrada si y solo si la acción se guardó.
    private async Task RegistrarMovimientoAsync(
        string accion, string entidad, Guid? entidadId, string entidadNombre, string? detalle, CancellationToken ct)
    {
        var nombre = await _db.AdminUsuarios.Where(a => a.Id == _currentAdmin.AdminId)
            .Select(a => a.Nombre).FirstOrDefaultAsync(ct) ?? "Admin";
        _db.MovimientosAdmin.Add(MovimientoAdmin.Crear(
            _currentAdmin.AdminId, nombre, _currentAdmin.Rol, accion, entidad, entidadId, entidadNombre, detalle));
    }

    private async Task<string> NombreDeTenantAsync(Guid tenantId, CancellationToken ct) =>
        await _db.Tenants.IgnoreQueryFilters().Where(t => t.Id == tenantId).Select(t => t.Nombre).FirstOrDefaultAsync(ct)
        ?? "Negocio";

    private static (string Nombre, string Apellido) SepararNombreApellido(string nombreCompleto)
    {
        var partes = nombreCompleto.Trim().Split(' ', 2);
        return partes.Length == 2 ? (partes[0], partes[1]) : (partes[0], string.Empty);
    }

    public async Task<IReadOnlyList<TenantResumenDto>> ListarTenantsAsync(CancellationToken ct = default)
    {
        // Cruza todos los negocios a propósito: este panel es para el operador de la plataforma,
        // no para un negocio en particular, así que acá sí corresponde saltar el filtro por tenant.
        // Un Vendedor solo ve los negocios que él mismo cargó; el Operador ve todos.
        var query = _db.Tenants.IgnoreQueryFilters().AsQueryable();
        if (!_currentAdmin.EsOperador)
            query = query.Where(t => t.VendedorId == _currentAdmin.AdminId);

        var tenants = await query.OrderBy(t => t.Nombre).ToListAsync(ct);

        var vendedorIds = tenants.Where(t => t.VendedorId != null).Select(t => t.VendedorId!.Value).Distinct().ToList();
        var nombresPorVendedor = await _db.AdminUsuarios
            .Where(v => vendedorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Nombre, ct);

        // Si Fluxo no responde, estadosPorSuscripcion queda vacío y el negocio se lista igual,
        // solo sin el estado de suscripción -- nunca debe romper este listado.
        var suscripcionIds = tenants.Where(t => t.FluxoSuscripcionId != null).Select(t => t.FluxoSuscripcionId!.Value);
        var estadosPorSuscripcion = await _fluxo.ObtenerConfirmacionesAsync(suscripcionIds, ct)
            ?? new Dictionary<int, FluxoEstadoSuscripcion>();

        var idsTenants = tenants.Select(t => t.Id).ToList();
        var pagosPorTenant = (await _db.PagosManuales.AsNoTracking()
                .Where(p => idsTenants.Contains(p.TenantId)).ToListAsync(ct))
            .GroupBy(p => p.TenantId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.FechaRegistroUtc).First());

        return tenants
            .Select(t => new TenantResumenDto(
                t.Id, t.Nombre, t.Slug, t.Activo, t.FechaCreacion,
                t.VendedorId, t.VendedorId is null ? null : nombresPorVendedor.GetValueOrDefault(t.VendedorId.Value),
                t.FluxoClienteId, t.FluxoSuscripcionId,
                FluxoEstado: t.FluxoSuscripcionId is null
                    ? null
                    : estadosPorSuscripcion.GetValueOrDefault(t.FluxoSuscripcionId.Value)?.Estado,
                FluxoPrimerCobroAprobado: t.FluxoSuscripcionId is not null
                    && estadosPorSuscripcion.GetValueOrDefault(t.FluxoSuscripcionId.Value)?.PrimerCobroAprobado == true,
                FluxoAjustePendiente: t.FluxoSuscripcionId is not null
                    && estadosPorSuscripcion.GetValueOrDefault(t.FluxoSuscripcionId.Value)?.AjusteMontoPendiente == true,
                FluxoCobroRechazado: t.FluxoSuscripcionId is not null
                    && estadosPorSuscripcion.GetValueOrDefault(t.FluxoSuscripcionId.Value)?.CobroRechazado == true,
                FluxoMotivoRechazo: t.FluxoSuscripcionId is null
                    ? null
                    : estadosPorSuscripcion.GetValueOrDefault(t.FluxoSuscripcionId.Value)?.MotivoRechazo,
                FluxoAccesoHasta: t.Activo && t.FluxoSuscripcionId is not null
                    && estadosPorSuscripcion.GetValueOrDefault(t.FluxoSuscripcionId.Value) is { } est
                    ? CorteAccesoPorSuscripcionService.AccesoVigenteHasta(
                        est, pagosPorTenant.GetValueOrDefault(t.Id)?.Estado == EstadoPagoManual.Confirmado)
                    : null,
                FormaPrimerPago: pagosPorTenant.GetValueOrDefault(t.Id)?.Metodo.ToString(),
                PagoManualEstado: pagosPorTenant.GetValueOrDefault(t.Id)?.Estado.ToString(),
                PagoManualMonto: pagosPorTenant.GetValueOrDefault(t.Id)?.Monto,
                PendienteActivacion: t.PendienteActivacion,
                FluxoProximoCobro: t.FluxoSuscripcionId is null
                    ? null
                    : estadosPorSuscripcion.GetValueOrDefault(t.FluxoSuscripcionId.Value)?.ProximoCobro))
            .ToList();
    }

    public async Task<TenantResumenDto> DesactivarTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(tenantId, ct);
        tenant.Desactivar();

        // Corta el acceso de todos los usuarios del negocio (no solo el admin) y libera sus
        // emails, para que se pueda volver a dar de alta un negocio nuevo con el mismo email.
        var usuarios = await _db.Usuarios.IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(ct);
        foreach (var usuario in usuarios)
        {
            usuario.Desactivar();
            usuario.LiberarEmail();
        }

        await RegistrarMovimientoAsync("negocio.desactivado", "negocio", tenant.Id, tenant.Nombre, null, ct);
        await _db.SaveChangesAsync(ct);
        return await ArmarResumenAsync(tenant, ct);
    }

    public async Task<TenantResumenDto> ActivarTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(tenantId, ct);

        if (tenant.PendienteActivacion)
            throw new AppException("Este negocio espera la confirmación del primer pago y de la suscripción: se activa solo apenas se cumplan las dos cosas.");

        // Un negocio con la suscripción cancelada no se reactiva: el sistema lo volvería a desactivar y,
        // además, no hay con qué cobrarle. Primero hay que darle una suscripción nueva.
        if (tenant.FluxoSuscripcionId is { } suscripcionId)
        {
            var estados = await _fluxo.ObtenerConfirmacionesAsync([suscripcionId], ct)
                ?? throw new AppException("No se pudo verificar la suscripción del negocio. Probá de nuevo en un momento.");
            if (estados.GetValueOrDefault(suscripcionId)?.Estado == "cancelled")
                throw new AppException("No se puede reactivar: la suscripción de este negocio está cancelada. Primero hay que crearle una suscripción nueva.");
        }

        // Al desactivar se liberó el email y se cortó el acceso de los usuarios; al reactivar hay que
        // devolverles ambas cosas, si no el cliente no podría entrar con su email de siempre. Si otro
        // negocio ya tomó ese email, no se reactiva (y no se toca nada).
        var usuarios = await _db.Usuarios.IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(ct);
        var aRestaurar = usuarios
            .Select(u => (Usuario: u, Original: u.EmailOriginalLiberado()))
            .Where(x => x.Original is not null)
            .ToList();

        foreach (var (usuario, original) in aRestaurar)
        {
            var enUso = await _db.Usuarios.IgnoreQueryFilters()
                .AnyAsync(x => x.Email == original && x.Id != usuario.Id, ct);
            if (enUso)
                throw new AppException($"No se puede reactivar el negocio: el email {original} ya lo usa otro negocio.");
        }

        tenant.Activar();
        foreach (var (usuario, original) in aRestaurar)
        {
            usuario.RestaurarEmail(original!);
            usuario.Activar();
        }

        await RegistrarMovimientoAsync("negocio.activado", "negocio", tenant.Id, tenant.Nombre, null, ct);
        await _db.SaveChangesAsync(ct);
        return await ArmarResumenAsync(tenant, ct);
    }

    public async Task<LinkPagoDto> ObtenerLinkPagoAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(tenantId, ct);

        // Un Vendedor solo puede ver los links de los negocios que él mismo cargó.
        if (!_currentAdmin.EsOperador && tenant.VendedorId != _currentAdmin.AdminId)
            throw new AppException("El negocio no existe.");

        if (tenant.FluxoSuscripcionId is null)
            throw new AppException("Este negocio no tiene una suscripción para cobrar.");

        var link = await _fluxo.ObtenerLinkPagoAsync(tenant.FluxoSuscripcionId.Value, ct);
        return new LinkPagoDto(link.Estado, link.InitPoint);
    }

    public async Task<CobrosNegocioDto> ObtenerCobrosAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(tenantId, ct);

        // Un Vendedor solo puede ver los cobros de los negocios que él mismo cargó.
        if (!_currentAdmin.EsOperador && tenant.VendedorId != _currentAdmin.AdminId)
            throw new AppException("El negocio no existe.");

        if (tenant.FluxoSuscripcionId is null)
            throw new AppException("Este negocio no tiene una suscripción.");

        var cobros = await _fluxo.ObtenerCobrosAsync(tenant.FluxoSuscripcionId.Value, ct);
        return new CobrosNegocioDto(
            cobros.Estado, cobros.MontoMensual, cobros.ProximoCobro, cobros.ProximoMonto,
            cobros.Cobros.Select(c => new CobroNegocioDto(c.Fecha, c.Monto, c.Estado, c.Motivo, c.Intento, c.ProximoReintento, c.EsPrimerCobro)).ToList());
    }

    private async Task<TenantResumenDto> ArmarResumenAsync(Tenant tenant, CancellationToken ct)
    {
        var vendedorNombre = tenant.VendedorId is null
            ? null
            : await _db.AdminUsuarios.Where(v => v.Id == tenant.VendedorId).Select(v => v.Nombre).FirstOrDefaultAsync(ct);

        FluxoEstadoSuscripcion? fluxoEstado = null;
        if (tenant.FluxoSuscripcionId is not null)
        {
            var estados = await _fluxo.ObtenerConfirmacionesAsync([tenant.FluxoSuscripcionId.Value], ct);
            fluxoEstado = estados?.GetValueOrDefault(tenant.FluxoSuscripcionId.Value);
        }

        var pagoManual = await _db.PagosManuales.AsNoTracking()
            .Where(p => p.TenantId == tenant.Id).OrderBy(p => p.FechaRegistroUtc).FirstOrDefaultAsync(ct);

        return new TenantResumenDto(
            tenant.Id, tenant.Nombre, tenant.Slug, tenant.Activo, tenant.FechaCreacion, tenant.VendedorId, vendedorNombre,
            tenant.FluxoClienteId, tenant.FluxoSuscripcionId, null, fluxoEstado?.Estado,
            fluxoEstado?.PrimerCobroAprobado == true, fluxoEstado?.AjusteMontoPendiente == true,
            fluxoEstado?.CobroRechazado == true, fluxoEstado?.MotivoRechazo,
            FormaPrimerPago: pagoManual?.Metodo.ToString(), PagoManualEstado: pagoManual?.Estado.ToString(),
            PagoManualMonto: pagoManual?.Monto, PendienteActivacion: tenant.PendienteActivacion,
            FluxoProximoCobro: fluxoEstado?.ProximoCobro);
    }

    private async Task<Tenant> ObtenerTenantAsync(Guid tenantId, CancellationToken ct) =>
        await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new AppException("El negocio no existe.");

    public async Task<IReadOnlyList<TenantFeatureDto>> ListarFeaturesAsync(Guid tenantId, CancellationToken ct = default)
    {
        await AsegurarTenantExisteAsync(tenantId, ct);

        return await _db.TenantFeatures.IgnoreQueryFilters()
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.Clave)
            .Select(f => new TenantFeatureDto(f.Id, f.Clave, f.Habilitado))
            .ToListAsync(ct);
    }

    public async Task<TenantFeatureDto> ActivarFeatureAsync(Guid tenantId, string clave, CancellationToken ct = default)
    {
        await AsegurarTenantExisteAsync(tenantId, ct);

        var feature = await _db.TenantFeatures.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Clave == clave, ct);

        if (feature is null)
        {
            feature = TenantFeature.Crear(tenantId, clave, habilitado: true);
            _db.TenantFeatures.Add(feature);
        }
        else
        {
            feature.Habilitar();
        }

        await RegistrarMovimientoAsync("feature.activada", "negocio", tenantId, await NombreDeTenantAsync(tenantId, ct), clave, ct);
        await _db.SaveChangesAsync(ct);
        return new TenantFeatureDto(feature.Id, feature.Clave, feature.Habilitado);
    }

    public async Task<TenantFeatureDto> DesactivarFeatureAsync(Guid tenantId, string clave, CancellationToken ct = default)
    {
        var feature = await _db.TenantFeatures.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Clave == clave, ct)
            ?? throw new AppException($"El negocio no tiene el feature '{clave}'.");

        feature.Deshabilitar();
        await RegistrarMovimientoAsync("feature.desactivada", "negocio", tenantId, await NombreDeTenantAsync(tenantId, ct), clave, ct);
        await _db.SaveChangesAsync(ct);
        return new TenantFeatureDto(feature.Id, feature.Clave, feature.Habilitado);
    }

    public async Task<IReadOnlyList<AdminUsuarioDto>> ListarUsuariosAsync(CancellationToken ct = default)
    {
        return await _db.AdminUsuarios
            .OrderBy(u => u.Nombre)
            .Select(u => new AdminUsuarioDto(u.Id, u.Email, u.Nombre, u.Rol.ToString(), u.Activo, u.CreadoUtc))
            .ToListAsync(ct);
    }

    public async Task<AdminUsuarioDto> CrearUsuarioAsync(CrearAdminUsuarioRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AdminRol>(request.Rol, out var rol))
            throw new AppException("El rol tiene que ser Operador o Vendedor.");

        var emailNormalizado = request.Email.Trim().ToLowerInvariant();
        var emailEnUso = await _db.AdminUsuarios.AnyAsync(u => u.Email == emailNormalizado, ct);
        if (emailEnUso)
            throw new AppException("Ya existe un usuario admin con ese email.");

        var usuario = AdminUsuario.Crear(emailNormalizado, _passwordHasher.Hash(request.Password), request.Nombre, rol);
        _db.AdminUsuarios.Add(usuario);
        await RegistrarMovimientoAsync("usuario.creado", "usuario", usuario.Id, usuario.Nombre, rol.ToString(), ct);
        await _db.SaveChangesAsync(ct);

        return new AdminUsuarioDto(usuario.Id, usuario.Email, usuario.Nombre, usuario.Rol.ToString(), usuario.Activo, usuario.CreadoUtc);
    }

    public async Task<AdminUsuarioDto> DesactivarUsuarioAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await ObtenerAdminUsuarioAsync(id, ct);
        usuario.Desactivar();
        await RegistrarMovimientoAsync("usuario.desactivado", "usuario", usuario.Id, usuario.Nombre, usuario.Rol.ToString(), ct);
        await _db.SaveChangesAsync(ct);
        return new AdminUsuarioDto(usuario.Id, usuario.Email, usuario.Nombre, usuario.Rol.ToString(), usuario.Activo, usuario.CreadoUtc);
    }

    public async Task<AdminUsuarioDto> ActivarUsuarioAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await ObtenerAdminUsuarioAsync(id, ct);
        usuario.Activar();
        await RegistrarMovimientoAsync("usuario.activado", "usuario", usuario.Id, usuario.Nombre, usuario.Rol.ToString(), ct);
        await _db.SaveChangesAsync(ct);
        return new AdminUsuarioDto(usuario.Id, usuario.Email, usuario.Nombre, usuario.Rol.ToString(), usuario.Activo, usuario.CreadoUtc);
    }

    private async Task<AdminUsuario> ObtenerAdminUsuarioAsync(Guid id, CancellationToken ct) =>
        await _db.AdminUsuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new AppException("El usuario admin no existe.");

    private async Task AsegurarTenantExisteAsync(Guid tenantId, CancellationToken ct)
    {
        var existe = await _db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == tenantId, ct);
        if (!existe)
            throw new AppException("El negocio no existe.");
    }

    private static string GenerarSlug(string nombre)
    {
        var normalizado = nombre.Trim().ToLowerInvariant();
        var caracteres = normalizado.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(caracteres);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
