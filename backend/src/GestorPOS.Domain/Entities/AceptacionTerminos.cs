namespace GestorPOS.Domain.Entities;

public enum OrigenAceptacionTerminos
{
    /// <summary>El dueño del negocio aceptó los términos al ingresar (es la que habilita el uso del sistema).</summary>
    Cliente,
    /// <summary>Quien dio el alta confirmó que le explicó las condiciones al cliente.</summary>
    Vendedor,
}

/// <summary>Constancia de que se aceptaron (o se explicaron) los términos y condiciones de una versión.
/// Se guarda quién, cuándo y desde qué IP; nunca se modifica: una versión nueva genera una constancia nueva.</summary>
public class AceptacionTerminos
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public OrigenAceptacionTerminos Origen { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string UsuarioNombre { get; private set; } = string.Empty;
    public string? Ip { get; private set; }
    public DateTime FechaUtc { get; private set; } = DateTime.UtcNow;

    private AceptacionTerminos() { }

    public static AceptacionTerminos Registrar(
        Guid tenantId, string version, OrigenAceptacionTerminos origen, Guid usuarioId, string usuarioNombre, string? ip)
        => new()
        {
            TenantId = tenantId,
            Version = version,
            Origen = origen,
            UsuarioId = usuarioId,
            UsuarioNombre = usuarioNombre,
            Ip = string.IsNullOrWhiteSpace(ip) ? null : ip.Trim(),
        };
}
