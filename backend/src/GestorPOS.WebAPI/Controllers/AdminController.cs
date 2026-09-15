using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

/// <summary>Panel interno del operador de GestorPOS, no de los negocios/tenants.
/// Protegido por API key (ver Middleware/AdminApiKeyMiddleware), no por JWT de tenant.</summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<IReadOnlyList<TenantResumenDto>>> ListarTenants(CancellationToken ct)
        => Ok(await _adminService.ListarTenantsAsync(ct));

    [HttpPost("tenants")]
    public async Task<ActionResult<TenantResumenDto>> CrearNegocio(CrearNegocioRequest request, CancellationToken ct)
        => Ok(await _adminService.CrearNegocioAsync(request, ct));

    [HttpGet("tenants/{tenantId:guid}/features")]
    public async Task<ActionResult<IReadOnlyList<TenantFeatureDto>>> ListarFeatures(Guid tenantId, CancellationToken ct)
        => Ok(await _adminService.ListarFeaturesAsync(tenantId, ct));

    [HttpPut("tenants/{tenantId:guid}/features/{clave}")]
    public async Task<ActionResult<TenantFeatureDto>> ActivarFeature(Guid tenantId, string clave, CancellationToken ct)
        => Ok(await _adminService.ActivarFeatureAsync(tenantId, clave, ct));

    [HttpDelete("tenants/{tenantId:guid}/features/{clave}")]
    public async Task<ActionResult<TenantFeatureDto>> DesactivarFeature(Guid tenantId, string clave, CancellationToken ct)
        => Ok(await _adminService.DesactivarFeatureAsync(tenantId, clave, ct));
}
