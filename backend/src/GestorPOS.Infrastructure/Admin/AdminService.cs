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

    public AdminService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
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
        _db.Tenants.Add(tenant);

        var passwordHash = _passwordHasher.Hash(request.Password);
        var admin = Usuario.Crear(tenant.Id, request.NombreAdmin, emailNormalizado, passwordHash, RolUsuario.Admin);
        _db.Usuarios.Add(admin);

        await _db.SaveChangesAsync(ct);

        return new TenantResumenDto(tenant.Id, tenant.Nombre, tenant.Slug, tenant.Activo, tenant.FechaCreacion);
    }

    public async Task<IReadOnlyList<TenantResumenDto>> ListarTenantsAsync(CancellationToken ct = default)
    {
        // Cruza todos los negocios a propósito: este panel es para el operador de la plataforma,
        // no para un negocio en particular, así que acá sí corresponde saltar el filtro por tenant.
        return await _db.Tenants.IgnoreQueryFilters()
            .OrderBy(t => t.Nombre)
            .Select(t => new TenantResumenDto(t.Id, t.Nombre, t.Slug, t.Activo, t.FechaCreacion))
            .ToListAsync(ct);
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
