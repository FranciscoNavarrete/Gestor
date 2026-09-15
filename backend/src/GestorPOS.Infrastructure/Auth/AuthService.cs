using GestorPOS.Application.Auth;
using GestorPOS.Application.Auth.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
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

    public AuthService(AppDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> RegistrarNegocioAsync(RegistrarNegocioRequest request, CancellationToken ct = default)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var emailEnUso = await _db.Usuarios.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == emailNormalizado, ct);
        if (emailEnUso)
            throw new AppException("Ya existe una cuenta registrada con ese email.");

        var slug = GenerarSlug(request.NombreNegocio);
        var slugEnUso = await _db.Tenants.AnyAsync(t => t.Slug == slug, ct);
        if (slugEnUso)
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

        var tenant = Domain.Entities.Tenant.Crear(request.NombreNegocio, slug);
        _db.Tenants.Add(tenant);

        var passwordHash = _passwordHasher.Hash(request.Password);
        var admin = Usuario.Crear(tenant.Id, request.NombreAdmin, emailNormalizado, passwordHash, RolUsuario.Admin);
        _db.Usuarios.Add(admin);

        await _db.SaveChangesAsync(ct);

        var (token, expira) = _jwtService.GenerarToken(admin, featuresHabilitadas: []);
        return new AuthResponse(token, expira, tenant.Id, tenant.Nombre, admin.Nombre, admin.Rol.ToString());
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
        return new AuthResponse(token, expira, tenant.Id, tenant.Nombre, usuario.Nombre, usuario.Rol.ToString());
    }

    private static string GenerarSlug(string nombre)
    {
        var normalizado = nombre.Trim().ToLowerInvariant();
        var caracteres = normalizado.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(caracteres);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
