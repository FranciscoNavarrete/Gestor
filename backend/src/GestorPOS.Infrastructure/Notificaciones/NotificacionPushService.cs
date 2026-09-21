using System.Net;
using System.Text.Json;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Notificaciones;
using GestorPOS.Application.Notificaciones.Dtos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebPush;

namespace GestorPOS.Infrastructure.Notificaciones;

public class NotificacionPushService : INotificacionPushService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;

    public NotificacionPushService(AppDbContext db, ITenantContext tenantContext, IConfiguration configuration)
    {
        _db = db;
        _tenantContext = tenantContext;
        _configuration = configuration;
    }

    public string ObtenerClavePublica() => _configuration["Vapid:PublicKey"] ?? string.Empty;

    public async Task SuscribirseAsync(SuscribirsePushRequest request, CancellationToken ct = default)
    {
        var existente = await _db.SuscripcionesPush.FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);
        if (existente is not null) return;

        var suscripcion = SuscripcionPush.Crear(
            _tenantContext.TenantId, _tenantContext.UsuarioId, request.Endpoint, request.P256dh, request.Auth);
        _db.SuscripcionesPush.Add(suscripcion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DesuscribirseAsync(string endpoint, CancellationToken ct = default)
    {
        var suscripcion = await _db.SuscripcionesPush.FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);
        if (suscripcion is null) return;

        _db.SuscripcionesPush.Remove(suscripcion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SuscripcionPushResumenDto>> ListarSuscripcionesAsync(CancellationToken ct = default)
    {
        return await _db.SuscripcionesPush
            .OrderByDescending(s => s.FechaCreacion)
            .Select(s => new SuscripcionPushResumenDto(s.Endpoint.Substring(0, Math.Min(60, s.Endpoint.Length)), s.FechaCreacion))
            .ToListAsync(ct);
    }

    public async Task NotificarStockBajoAsync(string productoNombre, int stockActual, CancellationToken ct = default)
    {
        var vapidDetails = ObtenerVapidDetails();
        if (vapidDetails is null) return;

        var suscripciones = await _db.SuscripcionesPush.ToListAsync(ct);
        if (suscripciones.Count == 0) return;

        var payload = ArmarPayload("Stock bajo", $"{productoNombre}: quedan {stockActual} unidades.");
        var cliente = new WebPushClient();

        foreach (var suscripcion in suscripciones)
        {
            var pushSubscription = new PushSubscription(suscripcion.Endpoint, suscripcion.P256dh, suscripcion.Auth);
            try
            {
                await cliente.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // El navegador/dispositivo ya no existe (desinstaló la app, limpió datos, etc.) — se
                // borra sola, no tiene sentido seguir intentándole ni pedirle al usuario que la saque.
                _db.SuscripcionesPush.Remove(suscripcion);
            }
            catch (Exception)
            {
                // Ninguna falla al notificar (red, servicio caído, claves de suscripción corruptas,
                // etc.) debe frenar la venta o el ajuste de stock que la disparó.
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ResultadoPruebaPushDto>> EnviarPruebaAsync(CancellationToken ct = default)
    {
        var vapidDetails = ObtenerVapidDetails();
        if (vapidDetails is null)
            return [new ResultadoPruebaPushDto("(ninguno)", false, "El servidor no tiene las claves VAPID configuradas.")];

        var suscripciones = await _db.SuscripcionesPush.ToListAsync(ct);
        if (suscripciones.Count == 0)
            return [new ResultadoPruebaPushDto("(ninguno)", false, "No hay ninguna suscripción guardada para este negocio.")];

        var payload = ArmarPayload("Prueba de notificación", "Si ves esto, las notificaciones push están funcionando.");
        var cliente = new WebPushClient();
        var resultados = new List<ResultadoPruebaPushDto>();

        foreach (var suscripcion in suscripciones)
        {
            var resumen = suscripcion.Endpoint.Substring(0, Math.Min(60, suscripcion.Endpoint.Length));
            var pushSubscription = new PushSubscription(suscripcion.Endpoint, suscripcion.P256dh, suscripcion.Auth);
            try
            {
                await cliente.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                resultados.Add(new ResultadoPruebaPushDto(resumen, true, null));
            }
            catch (Exception ex)
            {
                resultados.Add(new ResultadoPruebaPushDto(resumen, false, ex.ToString()));
            }
        }

        return resultados;
    }

    private VapidDetails? ObtenerVapidDetails()
    {
        var publicKey = _configuration["Vapid:PublicKey"];
        var privateKey = _configuration["Vapid:PrivateKey"];
        var subject = _configuration["Vapid:Subject"];

        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(subject))
            return null;

        return new VapidDetails(subject, publicKey, privateKey);
    }

    private static string ArmarPayload(string titulo, string cuerpo) => JsonSerializer.Serialize(new
    {
        notification = new
        {
            title = titulo,
            body = cuerpo,
            icon = "/icons/icon-192x192.png",
            data = new { onActionClick = new { @default = new { operation = "openWindow", url = "/productos" } } },
        },
    });
}
