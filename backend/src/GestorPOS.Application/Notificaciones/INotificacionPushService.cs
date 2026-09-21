using GestorPOS.Application.Notificaciones.Dtos;

namespace GestorPOS.Application.Notificaciones;

public interface INotificacionPushService
{
    string ObtenerClavePublica();

    Task SuscribirseAsync(SuscribirsePushRequest request, CancellationToken ct = default);
    Task DesuscribirseAsync(string endpoint, CancellationToken ct = default);
    Task<IReadOnlyList<SuscripcionPushResumenDto>> ListarSuscripcionesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ResultadoPruebaPushDto>> EnviarPruebaAsync(CancellationToken ct = default);

    /// <summary>Manda un push a todos los dispositivos suscriptos del tenant actual avisando que un
    /// producto cruzó su stock mínimo. Si el envío a algún endpoint falla porque el navegador ya no
    /// existe (410/404), se borra esa suscripción sola — no hace falta que el usuario la saque a mano.</summary>
    Task NotificarStockBajoAsync(string productoNombre, int stockActual, CancellationToken ct = default);
}
