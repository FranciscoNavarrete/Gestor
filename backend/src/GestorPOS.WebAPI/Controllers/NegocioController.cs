using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Configuracion;
using GestorPOS.Application.Configuracion.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/negocio")]
[Authorize]
public class NegocioController : ControllerBase
{
    private readonly INegocioService _negocioService;

    public NegocioController(INegocioService negocioService)
    {
        _negocioService = negocioService;
    }

    [HttpGet]
    public async Task<ActionResult<NegocioDto>> Obtener(CancellationToken ct)
        => Ok(await _negocioService.ObtenerAsync(ct));

    [HttpPut]
    public async Task<ActionResult<NegocioDto>> Actualizar(ActualizarNegocioRequest request, CancellationToken ct)
        => Ok(await _negocioService.ActualizarAsync(request, ct));

    [HttpPost("logo")]
    public async Task<ActionResult<NegocioDto>> ActualizarLogo(IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            throw new AppException("Subí una imagen para el logo.");

        using var stream = new MemoryStream();
        await archivo.CopyToAsync(stream, ct);
        return Ok(await _negocioService.ActualizarLogoAsync(stream.ToArray(), archivo.ContentType, ct));
    }

    [HttpDelete("logo")]
    public async Task<ActionResult<NegocioDto>> QuitarLogo(CancellationToken ct)
        => Ok(await _negocioService.QuitarLogoAsync(ct));

    [HttpGet("logo")]
    public async Task<IActionResult> ObtenerLogo(CancellationToken ct)
    {
        var logo = await _negocioService.ObtenerLogoAsync(ct);
        if (logo is null) return NotFound();
        return File(logo.Value.Datos, logo.Value.ContentType);
    }
}
