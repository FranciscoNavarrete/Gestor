using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

/// <summary>Panel interno para el operador de GestorPOS (no para los negocios/tenants).
/// Permite ver todos los negocios y activar/desactivar features puntuales por negocio.</summary>
public interface IAdminService
{
    Task<TenantResumenDto> CrearNegocioAsync(CrearNegocioRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TenantResumenDto>> ListarTenantsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TenantFeatureDto>> ListarFeaturesAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantFeatureDto> ActivarFeatureAsync(Guid tenantId, string clave, CancellationToken ct = default);
    Task<TenantFeatureDto> DesactivarFeatureAsync(Guid tenantId, string clave, CancellationToken ct = default);
}
