using System.Net;
using System.Text.Json;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebPush;

namespace GestorPOS.Infrastructure.Tareas;

/// <summary>Barre, cada un minuto y para TODOS los tenants (usa IgnoreQueryFilters — no hay request
/// ni ITenantContext acá, es un proceso de fondo), las tareas con recordatorio pendiente cuya hora de
/// aviso ya llegó y todavía no se les mandó el push. Mismo mecanismo de Web Push que
/// NotificacionPushService, pero ese está pensado para dispararse DENTRO de una request de un tenant
/// puntual (venta, ajuste de stock) — acá no hay tenant actual, por eso no se reusa tal cual.</summary>
public class TareaRecordatorioBackgroundService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TareaRecordatorioBackgroundService> _logger;

    public TareaRecordatorioBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TareaRecordatorioBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                await ProcesarRecordatoriosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Un error acá (red, WebPush caído, lo que sea) nunca puede tirar abajo el proceso.
                _logger.LogError(ex, "Error procesando recordatorios de tareas.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcesarRecordatoriosAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var vapidDetails = ObtenerVapidDetails(configuration);
        if (vapidDetails is null) return;

        var ahora = DateTime.UtcNow;
        var candidatas = await db.Tareas.IgnoreQueryFilters()
            .Where(t => !t.Completada && !t.NotificacionEnviada && t.MinutosAntesAviso != null)
            .ToListAsync(ct);

        var pendientes = candidatas
            .Where(t => ahora >= t.FechaHora.AddMinutes(-t.MinutosAntesAviso!.Value))
            .ToList();
        if (pendientes.Count == 0) return;

        var cliente = new WebPushClient();

        foreach (var grupo in pendientes.GroupBy(t => t.TenantId))
        {
            var suscripciones = await db.SuscripcionesPush.IgnoreQueryFilters()
                .Where(s => s.TenantId == grupo.Key)
                .ToListAsync(ct);

            foreach (var tarea in grupo)
            {
                var payload = ArmarPayload(tarea);

                foreach (var suscripcion in suscripciones)
                {
                    var pushSubscription = new PushSubscription(suscripcion.Endpoint, suscripcion.P256dh, suscripcion.Auth);
                    try
                    {
                        await cliente.SendNotificationAsync(pushSubscription, payload, vapidDetails, ct);
                    }
                    catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                    {
                        db.SuscripcionesPush.Remove(suscripcion);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo mandar el recordatorio de la tarea {TareaId}.", tarea.Id);
                    }
                }

                tarea.MarcarNotificacionEnviada();
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static VapidDetails? ObtenerVapidDetails(IConfiguration configuration)
    {
        var publicKey = configuration["Vapid:PublicKey"];
        var privateKey = configuration["Vapid:PrivateKey"];
        var subject = configuration["Vapid:Subject"];

        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(subject))
            return null;

        return new VapidDetails(subject, publicKey, privateKey);
    }

    private static string ArmarPayload(Tarea tarea)
    {
        var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(tarea.FechaHora, Common.ZonaHoraria.Argentina);
        return JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = "Recordatorio",
                body = $"{tarea.Titulo} · {fechaLocal:HH:mm}",
                icon = "/icons/icon-192x192.png",
                data = new { onActionClick = new { @default = new { operation = "openWindow", url = "/tareas" } } },
            },
        });
    }
}
