using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/admin/auth")]
[AllowAnonymous]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _adminAuth;

    public AdminAuthController(IAdminAuthService adminAuth) => _adminAuth = adminAuth;

    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        [FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        var response = await _adminAuth.LoginAsync(request, ct);
        return Ok(response);
    }
}
