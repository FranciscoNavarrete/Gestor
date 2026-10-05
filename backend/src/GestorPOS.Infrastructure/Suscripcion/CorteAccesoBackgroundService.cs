using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorPOS.Infrastructure.Suscripcion;

/// <summary>Cada pocos minutos revisa las suscripciones de los negocios activos y corta el acceso de los
/// que quedaron cancelados (ver <see cref="CorteAccesoPorSuscripcionService"/>). Se puede apagar con
/// Suscripcion:CorteAutomatico=false.</summary>
public class CorteAccesoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<CorteAccesoBackgroundService> _logger;

    public CorteAccesoBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<CorteAccesoBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.Equals(_config["Suscripcion:CorteAutomatico"], "false", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Corte automático por suscripción cancelada: desactivado por configuración.");
            return;
        }

        var segundos = int.TryParse(_config["Suscripcion:IntervaloSegundos"], out var s) && s >= 30 ? s : 300;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(segundos));

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var servicio = scope.ServiceProvider.GetRequiredService<CorteAccesoPorSuscripcionService>();
                await servicio.EjecutarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el corte automático por suscripción cancelada.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
