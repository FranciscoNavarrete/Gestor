using GestorPOS.Application.Configuracion;
using GestorPOS.Application.Configuracion.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/medios-pago")]
[Authorize]
public class MediosPagoController : ControllerBase
{
    private readonly IMedioPagoService _medioPagoService;

    public MediosPagoController(IMedioPagoService medioPagoService)
    {
        _medioPagoService = medioPagoService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MedioPagoDto>>> Listar(CancellationToken ct)
        => Ok(await _medioPagoService.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<MedioPagoDto>> Crear(CrearMedioPagoRequest request, CancellationToken ct)
        => Ok(await _medioPagoService.CrearAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MedioPagoDto>> Editar(Guid id, EditarMedioPagoRequest request, CancellationToken ct)
        => Ok(await _medioPagoService.EditarAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await _medioPagoService.DesactivarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activar")]
    public async Task<ActionResult<MedioPagoDto>> Activar(Guid id, CancellationToken ct)
        => Ok(await _medioPagoService.ActivarAsync(id, ct));

    [HttpDelete("{id:guid}/permanente")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await _medioPagoService.EliminarAsync(id, ct);
        return NoContent();
    }
}
