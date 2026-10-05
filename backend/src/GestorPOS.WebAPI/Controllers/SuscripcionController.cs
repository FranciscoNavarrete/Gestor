using GestorPOS.Application.Suscripcion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

/// <summary>La suscripción del propio negocio, solo para su dueño (usuario Admin del negocio).</summary>
[ApiController]
[Route("api/suscripcion")]
[Authorize(Roles = "Admin")]
public class SuscripcionController : ControllerBase
{
    private readonly IMiSuscripcionService _suscripcion;

    public SuscripcionController(IMiSuscripcionService suscripcion) => _suscripcion = suscripcion;

    [HttpGet]
    public async Task<ActionResult<MiSuscripcionDto>> Obtener(CancellationToken ct)
        => Ok(await _suscripcion.ObtenerAsync(ct));

    [HttpPut("tarjeta")]
    public async Task<IActionResult> CambiarTarjeta(CambiarTarjetaRequest request, CancellationToken ct)
    {
        await _suscripcion.CambiarTarjetaAsync(request, ct);
        return NoContent();
    }
}
