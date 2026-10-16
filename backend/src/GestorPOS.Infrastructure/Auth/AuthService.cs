using GestorPOS.Application.Auth;
using GestorPOS.Application.Auth.Dtos;
using GestorPOS.Application.Common;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Terminos;
using GestorPOS.Domain.Entities;
using GestorPOS.Domain.Enums;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ITenantContext _tenantContext;

    public AuthService(AppDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService, ITenantContext tenantContext)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _tenantContext = tenantContext;
    }

    public async Task CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == _tenantContext.UsuarioId && u.Activo, ct)
            ?? throw new AppException("No se encontró tu usuario.");

        if (!_passwordHasher.Verify(request.PasswordActual ?? string.Empty, usuario.PasswordHash))
            throw new AppException("La contraseña actual no es correcta.");

        ReglasPassword.ValidarNueva(request.PasswordActual, request.PasswordNueva);
        usuario.CambiarPassword(_passwordHasher.Hash(request.PasswordNueva));
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado && u.Activo, ct);

        if (usuario is null || !_passwordHasher.Verify(request.Password, usuario.PasswordHash))
            throw new AppException("Email o contraseña incorrectos.");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == usuario.TenantId, ct);
        if (tenant is { Activo: false, PendienteActivacion: true })
            throw new AppException("Tu cuenta está esperando la confirmación del pago. Apenas la recibamos vas a poder entrar. Si ya pagaste, avisale a tu vendedor.");
        if (tenant is null || !tenant.Activo)
            throw new AppException("El negocio asociado a esta cuenta no está disponible.");

        var features = await _db.TenantFeatures.IgnoreQueryFilters()
            .Where(f => f.TenantId == tenant.Id && f.Habilitado)
            .Select(f => f.Clave)
            .ToListAsync(ct);

        // Solo el dueño (Admin) acepta los términos; hasta que lo haga, el front le muestra esa pantalla antes de dejarlo usar el sistema.
        var terminosPendientes = usuario.Rol == RolUsuario.Admin
            && !await _db.AceptacionesTerminos.AnyAsync(a =>
                a.TenantId == tenant.Id && a.Origen == OrigenAceptacionTerminos.Cliente && a.Version == TerminosVigentes.Version, ct);

        var (token, expira) = _jwtService.GenerarToken(usuario, features);
        return new AuthResponse(token, expira, tenant.Id, tenant.Nombre, usuario.Nombre, usuario.Rol.ToString(), features, terminosPendientes, usuario.DebeCambiarPassword);
    }
}
