using GestorPOS.Application.Auth;
using GestorPOS.Application.Auth.Dtos;
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

    [HttpPost("registro-negocio")]
    public async Task<ActionResult<AuthResponse>> RegistrarNegocio(RegistrarNegocioRequest request, CancellationToken ct)
    {
        var response = await _authService.RegistrarNegocioAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }
}
