using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Admin;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
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
}
