using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Admin;

public class AdminAuthService : IAdminAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public AdminAuthService(AppDbContext db, IPasswordHasher hasher, IJwtService jwt)
    {
        _db     = db;
        _hasher = hasher;
        _jwt    = jwt;
    }

    public async Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _db.AdminUsuarios
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant() && u.Activo, ct);

        if (usuario is null || !_hasher.Verify(request.Password, usuario.PasswordHash))
            throw new AppException("Email o contraseña incorrectos.");

        var (token, expira) = _jwt.GenerarTokenAdmin(usuario);
        return new AdminLoginResponse(token, expira, usuario.Nombre, usuario.Rol.ToString());
    }
}
