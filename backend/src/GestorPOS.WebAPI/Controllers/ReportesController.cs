using GestorPOS.Application.Reportes;
using GestorPOS.Application.Reportes.Dtos;
using GestorPOS.Domain.Common;
using GestorPOS.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;
    private readonly IReportePdfService _reportePdfService;

    public ReportesController(IReporteService reporteService, IReportePdfService reportePdfService)
    {
        _reporteService = reporteService;
        _reportePdfService = reportePdfService;
    }

    [HttpGet("ranking-productos")]
    public async Task<ActionResult<IReadOnlyList<RankingProductoDto>>> RankingProductos(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] int top, CancellationToken ct)
        => Ok(await _reporteService.RankingProductosAsync(desde, hasta, top == 0 ? 10 : top, ct));

    [HttpGet("ranking-clientes")]
    public async Task<ActionResult<IReadOnlyList<RankingClienteDto>>> RankingClientes(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] int top, CancellationToken ct)
        => Ok(await _reporteService.RankingClientesAsync(desde, hasta, top == 0 ? 10 : top, ct));

    [HttpGet("ganancias")]
    public async Task<ActionResult<GananciasDto>> Ganancias(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
        => Ok(await _reporteService.GananciasAsync(desde, hasta, ct));

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct)
        => Ok(await _reporteService.DashboardAsync(ct));

    [HttpGet("deudas")]
    public async Task<ActionResult<DeudasDto>> Deudas(CancellationToken ct)
        => Ok(await _reporteService.DeudasAsync(ct));

    [HttpGet("deudas/pdf")]
    public async Task<IActionResult> DeudasPdf(CancellationToken ct)
    {
        var bytes = await _reportePdfService.GenerarDeudasPdfAsync(ct);
        return File(bytes, "application/pdf", "reporte-deudas.pdf");
    }

    [HttpGet("ventas/pdf")]
    public async Task<IActionResult> VentasPdf([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
    {
        var bytes = await _reportePdfService.GenerarVentasPdfAsync(desde, hasta, ct);
        return File(bytes, "application/pdf", "reporte-ventas.pdf");
    }

    [HttpGet("stock/pdf")]
    public async Task<IActionResult> StockPdf([FromQuery] string? busqueda, [FromQuery] bool bajoStock, CancellationToken ct)
    {
        var bytes = await _reportePdfService.GenerarStockPdfAsync(busqueda, bajoStock, ct);
        return File(bytes, "application/pdf", "reporte-stock.pdf");
    }

    [HttpGet("facturas-proveedor/pdf")]
    [RequireFeature(FacturasProveedorFeature.Clave)]
    public async Task<IActionResult> FacturasProveedorPdf(
        [FromQuery] Guid? proveedorId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] bool incluirPagadas, CancellationToken ct)
    {
        var bytes = await _reportePdfService.GenerarFacturasProveedorPdfAsync(proveedorId, desde, hasta, incluirPagadas, ct);
        return File(bytes, "application/pdf", "reporte-facturas-proveedor.pdf");
    }
}
