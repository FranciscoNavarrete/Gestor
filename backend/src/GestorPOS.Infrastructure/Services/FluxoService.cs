using System.Net.Http.Json;
using System.Text.Json;
using GestorPOS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestorPOS.Infrastructure.Services;

public class FluxoService : IFluxoService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<FluxoService> _logger;

    public FluxoService(HttpClient http, IConfiguration config, ILogger<FluxoService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<FluxoSuscripcionResultado?> IniciarSuscripcionAsync(
        string nombre, string apellido, string email, CancellationToken ct = default)
    {
        var baseUrl = _config["Fluxo:BaseUrl"];
        var apiKey = _config["Fluxo:VendedorApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Fluxo:BaseUrl o Fluxo:VendedorApiKey no configurados — se omite el alta de suscripción.");
            return null;
        }

        var planId = _config.GetValue<int?>("Fluxo:PlanIdSuscripcion");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/vendedor/suscripciones/iniciar");
            request.Headers.Add("X-Vendedor-Api-Key", apiKey);
            request.Content = JsonContent.Create(new { nombre, apellido, email, mpPlanId = planId });

            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Fluxo devolvió {StatusCode} al iniciar la suscripción de {Email}.", response.StatusCode, email);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<FluxoRespuesta>(JsonOptions, ct);
            if (body is null || !body.Exitoso || body.Contenido is null)
            {
                _logger.LogWarning("Fluxo no pudo crear la suscripción de {Email}: {Mensaje}", email, body?.Mensaje);
                return null;
            }

            var c = body.Contenido;
            return new FluxoSuscripcionResultado(c.ClienteId, c.UsuarioId, c.Email, c.PasswordTemporal, c.InitPoint, c.MpSuscripcionId);
        }
        catch (Exception ex)
        {
            // No revertimos el alta del negocio en GestorPOS por esto — queda sin suscripción
            // para vincular después a mano (Fluxo caído, timeout, etc.).
            _logger.LogWarning(ex, "Error llamando a Fluxo para iniciar la suscripción de {Email}.", email);
            return null;
        }
    }

    private class FluxoRespuesta
    {
        public bool Exitoso { get; set; }
        public string? Mensaje { get; set; }
        public FluxoContenido? Contenido { get; set; }
    }

    private class FluxoContenido
    {
        public int ClienteId { get; set; }
        public int UsuarioId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordTemporal { get; set; } = string.Empty;
        public string? InitPoint { get; set; }
        public int? MpSuscripcionId { get; set; }
    }
}
