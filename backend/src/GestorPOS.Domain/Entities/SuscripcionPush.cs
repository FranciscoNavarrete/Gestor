using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Una suscripción a notificaciones push de un dispositivo/navegador puntual de un usuario
/// (Web Push estándar: cada endpoint es único por navegador+dispositivo, así que un usuario con el
/// celular y la compu tiene dos filas). Sin esto no hay forma de mandarle un push — el browser nunca
/// expone el endpoint de vuelta, solo lo entrega una vez al suscribirse.</summary>
public class SuscripcionPush : TenantEntity
{
    public Guid UsuarioId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string P256dh { get; private set; } = string.Empty;
    public string Auth { get; private set; } = string.Empty;

    private SuscripcionPush() { }

    public static SuscripcionPush Crear(Guid tenantId, Guid usuarioId, string endpoint, string p256dh, string auth)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("El endpoint de la suscripción es obligatorio.", nameof(endpoint));

        return new SuscripcionPush
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
        };
    }
}
