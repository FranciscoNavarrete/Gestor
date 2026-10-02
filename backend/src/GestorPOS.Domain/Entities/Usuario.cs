using System.Text.RegularExpressions;
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

    // Se usa al desactivar el negocio dueño de este usuario: el email original queda libre para
    // que se pueda dar de alta un negocio nuevo con ese mismo email (el índice único de Email no
    // distingue si el tenant dueño sigue activo).
    public void LiberarEmail() => Email = $"baja+{Guid.NewGuid():N}+{Email}";

    private static readonly Regex EmailLiberado = new(@"^baja\+[0-9a-f]{32}\+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>El email que tenía antes de <see cref="LiberarEmail"/>, o null si no está liberado.</summary>
    public string? EmailOriginalLiberado()
    {
        var email = Email;
        var liberado = false;
        for (var match = EmailLiberado.Match(email); match.Success; match = EmailLiberado.Match(email))
        {
            email = match.Groups[1].Value;
            liberado = true;
        }
        return liberado ? email : null;
    }

    public void RestaurarEmail(string emailOriginal) => Email = emailOriginal;

    public void Activar() => Activo = true;
}
