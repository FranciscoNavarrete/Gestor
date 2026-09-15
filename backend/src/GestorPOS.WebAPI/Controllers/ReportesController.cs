using GestorPOS.Application.Reportes;
using GestorPOS.Application.Reportes.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet("ranking-productos")]
    public async Task<ActionResult<IReadOnlyList<RankingProductoDto>>> RankingProductos(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] int top, CancellationToken ct)
        => Ok(await _reporteService.RankingProductosAsync(desde, hasta, top == 0 ? 10 : top, ct));

    [HttpGet("ganancias")]
    public async Task<ActionResult<GananciasDto>> Ganancias(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
        => Ok(await _reporteService.GananciasAsync(desde, hasta, ct));

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct)
        => Ok(await _reporteService.DashboardAsync(ct));
}
