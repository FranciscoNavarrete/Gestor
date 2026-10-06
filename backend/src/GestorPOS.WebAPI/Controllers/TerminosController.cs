using GestorPOS.Application.Terminos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

/// <summary>Términos y condiciones: el texto es público (también se lee desde el alta); aceptarlos es cosa del
/// dueño del negocio (usuario Admin), y el sistema le pide hacerlo antes de usarlo.</summary>
[ApiController]
[Route("api/terminos")]
public class TerminosController : ControllerBase
{
    private readonly ITerminosService _terminos;

    public TerminosController(ITerminosService terminos) => _terminos = terminos;

    [HttpGet]
    [AllowAnonymous]
    public ActionResult<TerminosDto> Vigentes() => Ok(TerminosVigentes.ComoDto());

    [HttpGet("estado")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EstadoTerminosDto>> Estado(CancellationToken ct)
        => Ok(await _terminos.EstadoAsync(ct));

    [HttpPost("aceptar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Aceptar(CancellationToken ct)
    {
        await _terminos.AceptarAsync(HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return NoContent();
    }
}
