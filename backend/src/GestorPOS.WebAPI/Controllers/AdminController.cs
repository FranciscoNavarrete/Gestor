using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminPanel")]
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

    [HttpGet("fluxo/planes")]
    public async Task<ActionResult<IReadOnlyList<FluxoPlanDto>>> ListarPlanesFluxo(CancellationToken ct)
        => Ok(await _adminService.ListarPlanesFluxoAsync(ct));

    [HttpGet("tenants/{tenantId:guid}/features")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<IReadOnlyList<TenantFeatureDto>>> ListarFeatures(Guid tenantId, CancellationToken ct)
        => Ok(await _adminService.ListarFeaturesAsync(tenantId, ct));

    [HttpPut("tenants/{tenantId:guid}/features/{clave}")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<TenantFeatureDto>> ActivarFeature(Guid tenantId, string clave, CancellationToken ct)
        => Ok(await _adminService.ActivarFeatureAsync(tenantId, clave, ct));

    [HttpDelete("tenants/{tenantId:guid}/features/{clave}")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<TenantFeatureDto>> DesactivarFeature(Guid tenantId, string clave, CancellationToken ct)
        => Ok(await _adminService.DesactivarFeatureAsync(tenantId, clave, ct));

    [HttpGet("usuarios")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<IReadOnlyList<AdminUsuarioDto>>> ListarUsuarios(CancellationToken ct)
        => Ok(await _adminService.ListarUsuariosAsync(ct));

    [HttpPost("usuarios")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<AdminUsuarioDto>> CrearUsuario(CrearAdminUsuarioRequest request, CancellationToken ct)
        => Ok(await _adminService.CrearUsuarioAsync(request, ct));

    [HttpPost("usuarios/{id:guid}/desactivar")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<AdminUsuarioDto>> DesactivarUsuario(Guid id, CancellationToken ct)
        => Ok(await _adminService.DesactivarUsuarioAsync(id, ct));

    [HttpPost("usuarios/{id:guid}/activar")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<AdminUsuarioDto>> ActivarUsuario(Guid id, CancellationToken ct)
        => Ok(await _adminService.ActivarUsuarioAsync(id, ct));
}
