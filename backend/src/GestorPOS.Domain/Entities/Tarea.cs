using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Tarea con fecha/hora y recordatorio opcional por push. NotificacionEnviada evita que el
/// background service que barre recordatorios pendientes mande el mismo aviso más de una vez — se
/// resetea si se edita la fecha/hora o el aviso, para que se recalcule.</summary>
public class Tarea : TenantEntity
{
    public string Titulo { get; private set; } = string.Empty;
    public string? Notas { get; private set; }
    public DateTime FechaHora { get; private set; }
    public int? MinutosAntesAviso { get; private set; }
    public bool Completada { get; private set; }
    public bool NotificacionEnviada { get; private set; }
    public Guid UsuarioId { get; private set; }

    private Tarea() { }

    public static Tarea Crear(
        Guid tenantId, Guid usuarioId, string titulo, string? notas, DateTime fechaHoraUtc, int? minutosAntesAviso)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título de la tarea es obligatorio.", nameof(titulo));
        if (minutosAntesAviso is < 0)
            throw new ArgumentException("Los minutos de aviso no pueden ser negativos.", nameof(minutosAntesAviso));

        return new Tarea
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            Titulo = titulo.Trim(),
            Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim(),
            FechaHora = fechaHoraUtc,
            MinutosAntesAviso = minutosAntesAviso,
        };
    }

    public void Editar(string titulo, string? notas, DateTime fechaHoraUtc, int? minutosAntesAviso)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título de la tarea es obligatorio.", nameof(titulo));
        if (minutosAntesAviso is < 0)
            throw new ArgumentException("Los minutos de aviso no pueden ser negativos.", nameof(minutosAntesAviso));

        Titulo = titulo.Trim();
        Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
        FechaHora = fechaHoraUtc;
        MinutosAntesAviso = minutosAntesAviso;
        NotificacionEnviada = false;
    }

    public void MarcarCompletada() => Completada = true;
    public void MarcarPendiente() => Completada = false;
    public void MarcarNotificacionEnviada() => NotificacionEnviada = true;
}
