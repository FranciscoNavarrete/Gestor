using GestorPOS.Application.Notificaciones;
using GestorPOS.Application.Notificaciones.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionPushService _notificacionPushService;

    public NotificacionesController(INotificacionPushService notificacionPushService)
    {
        _notificacionPushService = notificacionPushService;
    }

    [HttpGet("clave-publica")]
    public ActionResult<ClaveVapidDto> ObtenerClavePublica()
        => Ok(new ClaveVapidDto(_notificacionPushService.ObtenerClavePublica()));

    [HttpPost("suscribirse")]
    public async Task<IActionResult> Suscribirse(SuscribirsePushRequest request, CancellationToken ct)
    {
        await _notificacionPushService.SuscribirseAsync(request, ct);
        return NoContent();
    }

    [HttpPost("desuscribirse")]
    public async Task<IActionResult> Desuscribirse(DesuscribirsePushRequest request, CancellationToken ct)
    {
        await _notificacionPushService.DesuscribirseAsync(request.Endpoint, ct);
        return NoContent();
    }
}
