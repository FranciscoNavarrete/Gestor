using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

/// <summary>Panel interno para el operador de GestorPOS (no para los negocios/tenants).
/// Permite ver todos los negocios y activar/desactivar features puntuales por negocio.</summary>
public interface IAdminService
{
    Task<TenantResumenDto> CrearNegocioAsync(CrearNegocioRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TenantResumenDto>> ListarTenantsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FluxoPlanDto>> ListarPlanesFluxoAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TenantFeatureDto>> ListarFeaturesAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantFeatureDto> ActivarFeatureAsync(Guid tenantId, string clave, CancellationToken ct = default);
    Task<TenantFeatureDto> DesactivarFeatureAsync(Guid tenantId, string clave, CancellationToken ct = default);
    Task<TenantResumenDto> DesactivarTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantResumenDto> ActivarTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<LinkPagoDto> ObtenerLinkPagoAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantResumenDto> ConfirmarPagoAsync(Guid tenantId, ConfirmarPagoRequest request, CancellationToken ct = default);
    Task<CobrosNegocioDto> ObtenerCobrosAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantResumenDto> NuevaSuscripcionAsync(Guid tenantId, NuevaSuscripcionRequest request, CancellationToken ct = default);
    Task<TenantResumenDto> CambiarPlanAsync(Guid tenantId, CambiarPlanRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<GestorPOS.Application.Terminos.AceptacionTerminosDto>> ObtenerTerminosAsync(Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<AdminUsuarioDto>> ListarUsuariosAsync(CancellationToken ct = default);
    Task<AdminUsuarioDto> CrearUsuarioAsync(CrearAdminUsuarioRequest request, CancellationToken ct = default);
    Task<AdminUsuarioDto> DesactivarUsuarioAsync(Guid id, CancellationToken ct = default);
    Task<AdminUsuarioDto> ActivarUsuarioAsync(Guid id, CancellationToken ct = default);
}
