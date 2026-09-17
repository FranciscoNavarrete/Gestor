using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Listar(CancellationToken ct)
        => Ok(await _categoriaService.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> Crear(CrearCategoriaRequest request, CancellationToken ct)
        => Ok(await _categoriaService.CrearAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoriaDto>> Editar(Guid id, EditarCategoriaRequest request, CancellationToken ct)
        => Ok(await _categoriaService.EditarAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await _categoriaService.DesactivarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activar")]
    public async Task<ActionResult<CategoriaDto>> Activar(Guid id, CancellationToken ct)
        => Ok(await _categoriaService.ActivarAsync(id, ct));
}
