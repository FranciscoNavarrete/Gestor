using GestorPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsuariosController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista los usuarios del negocio del token actual (el query filter global acota por tenant).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var usuarios = await _db.Usuarios
            .Select(u => new { u.Id, u.Nombre, u.Email, Rol = u.Rol.ToString(), u.Activo })
            .ToListAsync(ct);

        return Ok(usuarios);
    }
}
