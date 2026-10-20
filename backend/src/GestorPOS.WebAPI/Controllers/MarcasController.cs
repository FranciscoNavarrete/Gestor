using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/marcas")]
[Authorize]
public class MarcasController : ControllerBase
{
    private readonly IMarcaService _marcaService;

    public MarcasController(IMarcaService marcaService)
    {
        _marcaService = marcaService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MarcaDto>>> Listar(CancellationToken ct)
        => Ok(await _marcaService.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<MarcaDto>> Crear(CrearMarcaRequest request, CancellationToken ct)
        => Ok(await _marcaService.CrearAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MarcaDto>> Editar(Guid id, EditarMarcaRequest request, CancellationToken ct)
        => Ok(await _marcaService.EditarAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await _marcaService.DesactivarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activar")]
    public async Task<ActionResult<MarcaDto>> Activar(Guid id, CancellationToken ct)
        => Ok(await _marcaService.ActivarAsync(id, ct));

    [HttpDelete("{id:guid}/permanente")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await _marcaService.EliminarAsync(id, ct);
        return NoContent();
    }
}
