using GestorPOS.Application.Tareas;
using GestorPOS.Application.Tareas.Dtos;
using GestorPOS.Domain.Common;
using GestorPOS.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/tareas")]
[Authorize]
[RequireFeature(TareasFeature.Clave)]
public class TareasController : ControllerBase
{
    private readonly ITareaService _tareaService;

    public TareasController(ITareaService tareaService)
    {
        _tareaService = tareaService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TareaDto>>> Listar([FromQuery] bool incluirCompletadas, CancellationToken ct)
        => Ok(await _tareaService.ListarAsync(incluirCompletadas, ct));

    [HttpPost]
    public async Task<ActionResult<TareaDto>> Crear(CrearTareaRequest request, CancellationToken ct)
        => Ok(await _tareaService.CrearAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TareaDto>> Editar(Guid id, EditarTareaRequest request, CancellationToken ct)
        => Ok(await _tareaService.EditarAsync(id, request, ct));

    [HttpPost("{id:guid}/completar")]
    public async Task<ActionResult<TareaDto>> Completar(Guid id, CancellationToken ct)
        => Ok(await _tareaService.MarcarCompletadaAsync(id, ct));

    [HttpPost("{id:guid}/pendiente")]
    public async Task<ActionResult<TareaDto>> Pendiente(Guid id, CancellationToken ct)
        => Ok(await _tareaService.MarcarPendienteAsync(id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await _tareaService.EliminarAsync(id, ct);
        return NoContent();
    }
}
