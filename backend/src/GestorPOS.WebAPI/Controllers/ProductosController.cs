using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductoDto>>> Listar([FromQuery] bool bajoStock, CancellationToken ct)
        => Ok(await _productoService.ListarAsync(bajoStock, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductoDto>> Obtener(Guid id, CancellationToken ct)
        => Ok(await _productoService.ObtenerAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ProductoDto>> Crear(CrearProductoRequest request, CancellationToken ct)
        => Ok(await _productoService.CrearAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductoDto>> Editar(Guid id, EditarProductoRequest request, CancellationToken ct)
        => Ok(await _productoService.EditarAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await _productoService.DesactivarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("actualizar-precios-masivo")]
    public async Task<IActionResult> ActualizarPreciosMasivo(ActualizarPreciosMasivoRequest request, CancellationToken ct)
    {
        var afectados = await _productoService.ActualizarPreciosMasivoAsync(request, ct);
        return Ok(new { productosActualizados = afectados });
    }
}
