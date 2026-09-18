using GestorPOS.Application.Common.Dtos;
using GestorPOS.Application.MovimientosStock;
using GestorPOS.Application.MovimientosStock.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/movimientos-stock")]
[Authorize]
public class MovimientosStockController : ControllerBase
{
    private readonly IMovimientoStockService _movimientoStockService;

    public MovimientosStockController(IMovimientoStockService movimientoStockService)
    {
        _movimientoStockService = movimientoStockService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaDto<MovimientoStockDto>>> Buscar(
        [FromQuery] Guid? productoId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken ct = default)
        => Ok(await _movimientoStockService.BuscarAsync(productoId, desde, hasta, pagina, tamanoPagina, ct));
}
