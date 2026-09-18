using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using GestorPOS.Application.Common.Dtos;
using GestorPOS.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly IProductoImportService _productoImportService;

    public ProductosController(IProductoService productoService, IProductoImportService productoImportService)
    {
        _productoService = productoService;
        _productoImportService = productoImportService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductoDto>>> Listar([FromQuery] bool bajoStock, CancellationToken ct)
        => Ok(await _productoService.ListarAsync(bajoStock, ct));

    [HttpGet("buscar")]
    public async Task<ActionResult<PaginaDto<ProductoDto>>> Buscar(
        [FromQuery] string? busqueda,
        [FromQuery] bool bajoStock,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken ct = default)
        => Ok(await _productoService.BuscarAsync(busqueda, bajoStock, pagina, tamanoPagina, ct));

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

    [HttpPost("{id:guid}/activar")]
    public async Task<ActionResult<ProductoDto>> Activar(Guid id, CancellationToken ct)
        => Ok(await _productoService.ActivarAsync(id, ct));

    [HttpPost("{id:guid}/ajustar-stock")]
    public async Task<ActionResult<ProductoDto>> AjustarStock(Guid id, AjustarStockRequest request, CancellationToken ct)
        => Ok(await _productoService.AjustarStockAsync(id, request.Cantidad, request.Motivo, ct));

    [HttpPost("actualizar-precios-masivo")]
    public async Task<IActionResult> ActualizarPreciosMasivo(ActualizarPreciosMasivoRequest request, CancellationToken ct)
    {
        var afectados = await _productoService.ActualizarPreciosMasivoAsync(request, ct);
        return Ok(new { productosActualizados = afectados });
    }

    [HttpGet("plantilla-excel")]
    public IActionResult DescargarPlantilla()
    {
        var bytes = _productoImportService.GenerarPlantilla();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-productos.xlsx");
    }

    [HttpPost("importar-excel")]
    public async Task<ActionResult<ImportarProductosResultado>> ImportarExcel(IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            throw new AppException("Subí un archivo Excel (.xlsx).");

        await using var stream = archivo.OpenReadStream();
        var resultado = await _productoImportService.ImportarAsync(stream, ct);
        return Ok(resultado);
    }
}
