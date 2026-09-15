using GestorPOS.Domain.Common;
using GestorPOS.Domain.Enums;

namespace GestorPOS.Domain.Entities;

public class Usuario : TenantEntity
{
    public string Nombre { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; } = true;

    private Usuario() { }

    public static Usuario Crear(Guid tenantId, string nombre, string email, string passwordHash, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email es obligatorio.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña es obligatorio.", nameof(passwordHash));

        return new Usuario
        {
            TenantId = tenantId,
            Nombre = nombre.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Rol = rol
        };
    }

    public void Desactivar() => Activo = false;
}
