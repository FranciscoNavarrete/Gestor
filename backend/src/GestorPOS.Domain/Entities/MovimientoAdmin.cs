namespace GestorPOS.Domain.Entities;

/// <summary>Una acción hecha desde el panel de administración (alta de un negocio, activar o desactivar un
/// negocio, una función o un usuario). Los nombres se guardan tal como estaban en el momento, así el
/// historial se entiende aunque después se renombre algo.</summary>
public class MovimientoAdmin
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime FechaUtc { get; private set; } = DateTime.UtcNow;
    public Guid AdminId { get; private set; }
    public string AdminNombre { get; private set; } = string.Empty;
    public string AdminRol { get; private set; } = string.Empty;

    /// <summary>negocio.alta, negocio.activado, negocio.desactivado, feature.activada, feature.desactivada,
    /// usuario.creado, usuario.activado o usuario.desactivado.</summary>
    public string Accion { get; private set; } = string.Empty;

    /// <summary>negocio o usuario: sobre qué se hizo la acción.</summary>
    public string Entidad { get; private set; } = string.Empty;
    public Guid? EntidadId { get; private set; }
    public string EntidadNombre { get; private set; } = string.Empty;
    public string? Detalle { get; private set; }

    private MovimientoAdmin() { }

    public static MovimientoAdmin Crear(
        Guid adminId, string adminNombre, string adminRol, string accion, string entidad,
        Guid? entidadId, string entidadNombre, string? detalle) =>
        new()
        {
            AdminId = adminId,
            AdminNombre = adminNombre,
            AdminRol = adminRol,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            EntidadNombre = entidadNombre,
            Detalle = detalle,
        };
}
