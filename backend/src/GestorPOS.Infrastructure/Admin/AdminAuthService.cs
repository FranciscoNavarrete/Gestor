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
    private readonly ICurrentAdminContext _currentAdmin;

    public AdminAuthService(AppDbContext db, IPasswordHasher hasher, IJwtService jwt, ICurrentAdminContext currentAdmin)
    {
        _db     = db;
        _hasher = hasher;
        _jwt    = jwt;
        _currentAdmin = currentAdmin;
    }

    public async Task CambiarPasswordAsync(GestorPOS.Application.Auth.Dtos.CambiarPasswordRequest request, CancellationToken ct = default)
    {
        var usuario = await _db.AdminUsuarios.FirstOrDefaultAsync(u => u.Id == _currentAdmin.AdminId && u.Activo, ct)
            ?? throw new AppException("No se encontró tu usuario.");

        if (!_hasher.Verify(request.PasswordActual ?? string.Empty, usuario.PasswordHash))
            throw new AppException("La contraseña actual no es correcta.");

        GestorPOS.Application.Common.ReglasPassword.ValidarNueva(request.PasswordActual, request.PasswordNueva);
        usuario.CambiarPassword(_hasher.Hash(request.PasswordNueva));
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _db.AdminUsuarios
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant() && u.Activo, ct);

        if (usuario is null || !_hasher.Verify(request.Password, usuario.PasswordHash))
            throw new AppException("Email o contraseña incorrectos.");

        var (token, expira) = _jwt.GenerarTokenAdmin(usuario);
        return new AdminLoginResponse(token, expira, usuario.Nombre, usuario.Rol.ToString(), usuario.DebeCambiarPassword);
    }
}
