using GestorPOS.Application.Compras;
using GestorPOS.Application.Compras.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/compras")]
[Authorize]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _compraService;

    public ComprasController(ICompraService compraService)
    {
        _compraService = compraService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompraResumenDto>>> Listar(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
        => Ok(await _compraService.ListarAsync(desde, hasta, ct));

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenComprasDto>> Resumen(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
        => Ok(await _compraService.ResumenAsync(desde, hasta, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompraDto>> Obtener(Guid id, CancellationToken ct)
        => Ok(await _compraService.ObtenerAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CompraDto>> Crear(CrearCompraRequest request, CancellationToken ct)
        => Ok(await _compraService.CrearAsync(request, ct));
}
