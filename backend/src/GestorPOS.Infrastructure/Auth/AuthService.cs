using GestorPOS.Application.Auth;
using GestorPOS.Application.Auth.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public AuthService(AppDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado && u.Activo, ct);

        if (usuario is null || !_passwordHasher.Verify(request.Password, usuario.PasswordHash))
            throw new AppException("Email o contraseña incorrectos.");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == usuario.TenantId, ct);
        if (tenant is null || !tenant.Activo)
            throw new AppException("El negocio asociado a esta cuenta no está disponible.");

        var features = await _db.TenantFeatures.IgnoreQueryFilters()
            .Where(f => f.TenantId == tenant.Id && f.Habilitado)
            .Select(f => f.Clave)
            .ToListAsync(ct);

        var (token, expira) = _jwtService.GenerarToken(usuario, features);
        return new AuthResponse(token, expira, tenant.Id, tenant.Nombre, usuario.Nombre, usuario.Rol.ToString(), features);
    }
}
