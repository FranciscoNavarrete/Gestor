namespace GestorPOS.Domain.Entities;

public enum AdminRol { Operador, Vendedor }

public class AdminUsuario
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public AdminRol Rol { get; private set; }
    public bool Activo { get; private set; } = true;
    public DateTime CreadoUtc { get; private set; } = DateTime.UtcNow;

    /// <summary>El operador le restableció la contraseña con una temporal: tiene que elegir una nueva antes de seguir.</summary>
    public bool DebeCambiarPassword { get; private set; }

    private AdminUsuario() { }

    public static AdminUsuario Crear(string email, string passwordHash, string nombre, AdminRol rol) =>
        new() { Email = email.ToLowerInvariant(), PasswordHash = passwordHash, Nombre = nombre, Rol = rol };

    public void CambiarPassword(string nuevoHash)
    {
        PasswordHash = nuevoHash;
        DebeCambiarPassword = false;
    }

    public void RestablecerConTemporal(string hashTemporal)
    {
        PasswordHash = hashTemporal;
        DebeCambiarPassword = true;
    }

    public void Desactivar() => Activo = false;
    public void Activar()    => Activo = true;
}
