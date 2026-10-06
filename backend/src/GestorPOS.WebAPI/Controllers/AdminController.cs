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
    private readonly IReportesVentasService _reportesVentas;
    private readonly IMovimientosAdminService _movimientos;
    private readonly IResumenFinancieroService _resumenFinanciero;
    private readonly ILiquidacionesService _liquidaciones;
    private readonly IEfectivoService _efectivo;

    public AdminController(
        IAdminService adminService, IReportesVentasService reportesVentas, IMovimientosAdminService movimientos,
        IResumenFinancieroService resumenFinanciero, ILiquidacionesService liquidaciones, IEfectivoService efectivo)
    {
        _efectivo = efectivo;
        _liquidaciones = liquidaciones;
        _adminService = adminService;
        _reportesVentas = reportesVentas;
        _movimientos = movimientos;
        _resumenFinanciero = resumenFinanciero;
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<IReadOnlyList<TenantResumenDto>>> ListarTenants(CancellationToken ct)
        => Ok(await _adminService.ListarTenantsAsync(ct));

    [HttpPost("tenants")]
    public async Task<ActionResult<TenantResumenDto>> CrearNegocio(CrearNegocioRequest request, CancellationToken ct)
        => Ok(await _adminService.CrearNegocioAsync(
            request with { Ip = HttpContext.Connection.RemoteIpAddress?.ToString() }, ct));

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

    [HttpPost("tenants/{tenantId:guid}/desactivar")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<TenantResumenDto>> DesactivarTenant(Guid tenantId, CancellationToken ct)
        => Ok(await _adminService.DesactivarTenantAsync(tenantId, ct));

    [HttpPost("tenants/{tenantId:guid}/activar")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<TenantResumenDto>> ActivarTenant(Guid tenantId, CancellationToken ct)
        => Ok(await _adminService.ActivarTenantAsync(tenantId, ct));

    [HttpPost("tenants/{tenantId:guid}/confirmar-pago")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<TenantResumenDto>> ConfirmarPago(Guid tenantId, ConfirmarPagoRequest request, CancellationToken ct)
        => Ok(await _adminService.ConfirmarPagoAsync(tenantId, request, ct));

    [HttpGet("tenants/{tenantId:guid}/link-pago")]
    public async Task<ActionResult<LinkPagoDto>> ObtenerLinkPago(Guid tenantId, CancellationToken ct)
        => Ok(await _adminService.ObtenerLinkPagoAsync(tenantId, ct));

    [HttpPut("tenants/{tenantId:guid}/plan")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<TenantResumenDto>> CambiarPlan(Guid tenantId, CambiarPlanRequest request, CancellationToken ct)
        => Ok(await _adminService.CambiarPlanAsync(tenantId, request, ct));

    [HttpGet("tenants/{tenantId:guid}/cobros")]
    public async Task<ActionResult<CobrosNegocioDto>> ObtenerCobros(Guid tenantId, CancellationToken ct)
        => Ok(await _adminService.ObtenerCobrosAsync(tenantId, ct));

    [HttpGet("tenants/{tenantId:guid}/terminos")]
    public async Task<ActionResult<IReadOnlyList<GestorPOS.Application.Terminos.AceptacionTerminosDto>>> ObtenerTerminos(Guid tenantId, CancellationToken ct)
        => Ok(await _adminService.ObtenerTerminosAsync(tenantId, ct));

    [HttpGet("reportes/ventas")]
    public async Task<ActionResult<ReporteVentasDto>> ReporteVentas(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] Guid? vendedorId, CancellationToken ct)
        => Ok(await _reportesVentas.ObtenerVentasAsync(desde, hasta, vendedorId, ct));

    [HttpGet("efectivo")]
    public async Task<ActionResult<EfectivoResumenDto>> ResumenEfectivo(CancellationToken ct)
        => Ok(await _efectivo.ResumenAsync(ct));

    [HttpGet("efectivo/{vendedorId:guid}/movimientos")]
    public async Task<ActionResult<IReadOnlyList<EfectivoMovimientoDto>>> MovimientosEfectivo(Guid vendedorId, CancellationToken ct)
        => Ok(await _efectivo.MovimientosAsync(vendedorId, ct));

    [HttpPost("efectivo/entregas")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<EfectivoVendedorDto>> RegistrarEntrega(RegistrarEntregaRequest request, CancellationToken ct)
        => Ok(await _efectivo.RegistrarEntregaAsync(request, ct));

    [HttpDelete("efectivo/entregas/{id:guid}")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<IActionResult> AnularEntrega(Guid id, CancellationToken ct)
    {
        await _efectivo.AnularEntregaAsync(id, ct);
        return NoContent();
    }

    [HttpGet("liquidaciones/resumen")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<LiquidacionesMesDto>> ResumenLiquidaciones(
        [FromQuery] int anio, [FromQuery] int mes, CancellationToken ct)
        => Ok(await _liquidaciones.ResumenAsync(anio, mes, ct));

    [HttpGet("liquidaciones/previsualizar")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<PrevisualizacionLiquidacionDto>> PrevisualizarLiquidacion(
        [FromQuery] Guid vendedorId, [FromQuery] int anio, [FromQuery] int mes, CancellationToken ct)
        => Ok(await _liquidaciones.PrevisualizarAsync(vendedorId, anio, mes, ct));

    [HttpGet("liquidaciones")]
    public async Task<ActionResult<IReadOnlyList<LiquidacionDto>>> ListarLiquidaciones(
        [FromQuery] Guid? vendedorId, CancellationToken ct)
        => Ok(await _liquidaciones.ListarAsync(vendedorId, ct));

    [HttpPost("liquidaciones")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<LiquidacionDto>> Liquidar(LiquidarRequest request, CancellationToken ct)
        => Ok(await _liquidaciones.LiquidarAsync(request, ct));

    [HttpPost("liquidaciones/{id:guid}/pagar")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<LiquidacionDto>> PagarLiquidacion(Guid id, PagarLiquidacionRequest request, CancellationToken ct)
        => Ok(await _liquidaciones.PagarAsync(id, request, ct));

    [HttpDelete("liquidaciones/{id:guid}")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<IActionResult> AnularLiquidacion(Guid id, CancellationToken ct)
    {
        await _liquidaciones.AnularAsync(id, ct);
        return NoContent();
    }

    [HttpGet("resumen")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<ResumenFinancieroDto>> Resumen(CancellationToken ct)
        => Ok(await _resumenFinanciero.ObtenerAsync(ct));

    [HttpGet("movimientos")]
    [Authorize(Policy = "AdminOperador")]
    public async Task<ActionResult<MovimientosAdminDto>> Movimientos(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] Guid? adminId,
        [FromQuery] string? accion, [FromQuery] string? texto, CancellationToken ct)
        => Ok(await _movimientos.ListarAsync(desde, hasta, adminId, accion, texto, ct));

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
