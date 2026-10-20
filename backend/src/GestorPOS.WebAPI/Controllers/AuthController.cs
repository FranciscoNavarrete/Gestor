using GestorPOS.Application.Auth;
using GestorPOS.Application.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("cambiar-password")]
    [Authorize]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest request, CancellationToken ct)
    {
        await _authService.CambiarPasswordAsync(request, ct);
        return NoContent();
    }

    /// <summary>Las funciones que el negocio tiene hoy: la app las consulta para enterarse de un cambio del
    /// operador sin que el dueño tenga que volver a ingresar.</summary>
    [HttpGet("funciones")]
    [Authorize]
    public IActionResult Funciones()
        => Ok(new { features = User.FindAll("feature").Select(c => c.Value).ToList() });

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }
}
