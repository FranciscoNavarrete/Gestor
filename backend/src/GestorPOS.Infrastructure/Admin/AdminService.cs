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

    public AdminService(AppDbContext db, IPasswordHasher passwordHasher, ICurrentAdminContext currentAdmin)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentAdmin = currentAdmin;
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

        var tenant = Tenant.Crear(request.NombreNegocio, slug);

        // Un Vendedor solo se atribuye negocios a sí mismo, nunca a otro vendedor (aunque lo mande
        // en el body); el Operador sí puede elegir a quién atribuírselo, o dejarlo sin vendedor.
        var vendedorId = _currentAdmin.EsOperador ? request.VendedorId : _currentAdmin.AdminId;
        tenant.AsignarVendedor(vendedorId);

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

        return new TenantResumenDto(tenant.Id, tenant.Nombre, tenant.Slug, tenant.Activo, tenant.FechaCreacion, vendedorId, vendedorNombre);
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

        return tenants
            .Select(t => new TenantResumenDto(
                t.Id, t.Nombre, t.Slug, t.Activo, t.FechaCreacion,
                t.VendedorId, t.VendedorId is null ? null : nombresPorVendedor.GetValueOrDefault(t.VendedorId.Value)))
            .ToList();
    }

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
