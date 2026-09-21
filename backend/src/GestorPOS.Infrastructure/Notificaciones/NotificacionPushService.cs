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

    public async Task NotificarStockBajoAsync(string productoNombre, int stockActual, CancellationToken ct = default)
    {
        var publicKey = _configuration["Vapid:PublicKey"];
        var privateKey = _configuration["Vapid:PrivateKey"];
        var subject = _configuration["Vapid:Subject"];

        // Sin claves configuradas (ambiente que todavía no las tiene) no rompe la venta/ajuste por esto,
        // simplemente no manda nada.
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(subject))
            return;

        var suscripciones = await _db.SuscripcionesPush.ToListAsync(ct);
        if (suscripciones.Count == 0) return;

        var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        var payload = JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = "Stock bajo",
                body = $"{productoNombre}: quedan {stockActual} unidades.",
                icon = "/icons/icon-192x192.png",
                data = new { onActionClick = new { @default = new { operation = "openWindow", url = "/productos" } } },
            },
        });

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
}
