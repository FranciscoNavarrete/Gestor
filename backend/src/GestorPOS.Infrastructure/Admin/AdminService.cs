using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Domain.Enums;
using GestorPOS.Infrastructure.Persistence;
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

        var fluxo = await _fluxo.IniciarSuscripcionAsync(
            nombrePila, apellido, emailNormalizado, request.MpPlanId, cardToken, ct);

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

        await _db.SaveChangesAsync(ct);

        var vendedorNombre = vendedorId is null
            ? null
            : await _db.AdminUsuarios.Where(v => v.Id == vendedorId).Select(v => v.Nombre).FirstOrDefaultAsync(ct);

        return new TenantResumenDto(
            tenant.Id, tenant.Nombre, tenant.Slug, tenant.Activo, tenant.FechaCreacion, vendedorId, vendedorNombre,
            tenant.FluxoClienteId, tenant.FluxoSuscripcionId, fluxo.InitPoint, fluxo.Estado);
    }

    public async Task<IReadOnlyList<FluxoPlanDto>> ListarPlanesFluxoAsync(CancellationToken ct = default)
    {
        var planes = await _fluxo.ListarPlanesAsync(ct);
        return planes
            .Select(p => new FluxoPlanDto(p.MpPlanId, p.Nombre, p.Monto, p.Moneda, p.TipoFrecuencia, p.Frecuencia, p.DiasGratis))
            .ToList();
    }

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
        var estadosPorSuscripcion = await _fluxo.ObtenerEstadosSuscripcionesAsync(suscripcionIds, ct);

        return tenants
            .Select(t => new TenantResumenDto(
                t.Id, t.Nombre, t.Slug, t.Activo, t.FechaCreacion,
                t.VendedorId, t.VendedorId is null ? null : nombresPorVendedor.GetValueOrDefault(t.VendedorId.Value),
                t.FluxoClienteId, t.FluxoSuscripcionId,
                FluxoEstado: t.FluxoSuscripcionId is null
                    ? null
                    : estadosPorSuscripcion.GetValueOrDefault(t.FluxoSuscripcionId.Value)))
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

        await _db.SaveChangesAsync(ct);
        return await ArmarResumenAsync(tenant, ct);
    }

    public async Task<TenantResumenDto> ActivarTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(tenantId, ct);
        tenant.Activar();
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

    private async Task<TenantResumenDto> ArmarResumenAsync(Tenant tenant, CancellationToken ct)
    {
        var vendedorNombre = tenant.VendedorId is null
            ? null
            : await _db.AdminUsuarios.Where(v => v.Id == tenant.VendedorId).Select(v => v.Nombre).FirstOrDefaultAsync(ct);

        string? fluxoEstado = null;
        if (tenant.FluxoSuscripcionId is not null)
        {
            var estados = await _fluxo.ObtenerEstadosSuscripcionesAsync([tenant.FluxoSuscripcionId.Value], ct);
            fluxoEstado = estados.GetValueOrDefault(tenant.FluxoSuscripcionId.Value);
        }

        return new TenantResumenDto(
            tenant.Id, tenant.Nombre, tenant.Slug, tenant.Activo, tenant.FechaCreacion, tenant.VendedorId, vendedorNombre,
            tenant.FluxoClienteId, tenant.FluxoSuscripcionId, null, fluxoEstado);
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

        await _db.SaveChangesAsync(ct);
        return new TenantFeatureDto(feature.Id, feature.Clave, feature.Habilitado);
    }

    public async Task<TenantFeatureDto> DesactivarFeatureAsync(Guid tenantId, string clave, CancellationToken ct = default)
    {
        var feature = await _db.TenantFeatures.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Clave == clave, ct)
            ?? throw new AppException($"El negocio no tiene el feature '{clave}'.");

        feature.Deshabilitar();
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
        await _db.SaveChangesAsync(ct);

        return new AdminUsuarioDto(usuario.Id, usuario.Email, usuario.Nombre, usuario.Rol.ToString(), usuario.Activo, usuario.CreadoUtc);
    }

    public async Task<AdminUsuarioDto> DesactivarUsuarioAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await ObtenerAdminUsuarioAsync(id, ct);
        usuario.Desactivar();
        await _db.SaveChangesAsync(ct);
        return new AdminUsuarioDto(usuario.Id, usuario.Email, usuario.Nombre, usuario.Rol.ToString(), usuario.Activo, usuario.CreadoUtc);
    }

    public async Task<AdminUsuarioDto> ActivarUsuarioAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await ObtenerAdminUsuarioAsync(id, ct);
        usuario.Activar();
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
