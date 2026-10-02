using System.Net.Http.Json;
using System.Text.Json;
using GestorPOS.Application.Common.Exceptions;
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

    public async Task<FluxoSuscripcionResultado> IniciarSuscripcionAsync(
        string nombre, string apellido, string email, int? mpPlanId, string? cardTokenId = null,
        CancellationToken ct = default)
    {
        var baseUrl = _config["Fluxo:BaseUrl"];
        var apiKey = _config["Fluxo:VendedorApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Fluxo:BaseUrl o Fluxo:VendedorApiKey no configurados.");
            throw new AppException("La integración con Fluxo no está configurada.");
        }

        var conTarjeta = !string.IsNullOrWhiteSpace(cardTokenId);

        FluxoRespuesta? body;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/vendedor/suscripciones/iniciar");
            request.Headers.Add("X-Vendedor-Api-Key", apiKey);
            request.Content = JsonContent.Create(new { nombre, apellido, email, mpPlanId, cardTokenId });

            var response = await _http.SendAsync(request, ct);
            // Fluxo devuelve el mismo cuerpo {exitoso, mensaje, contenido} tanto en 200 como en
            // 400, así que vale la pena leerlo igual aunque el status no haya sido éxito.
            body = await response.Content.ReadFromJsonAsync<FluxoRespuesta>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a Fluxo para iniciar la suscripción de {Email}.", email);
            throw new AppException("No se pudo conectar con Fluxo para crear la suscripción. Probá de nuevo en un momento.");
        }

        // Por link hace falta el InitPoint; con tarjeta no hay link, alcanza con que la suscripción
        // haya quedado creada (MpSuscripcionId).
        var resultadoValido = body is { Exitoso: true, Contenido: not null }
            && (conTarjeta
                ? body.Contenido.MpSuscripcionId.HasValue
                : !string.IsNullOrWhiteSpace(body.Contenido.InitPoint));
        if (!resultadoValido)
        {
            _logger.LogWarning(
                "Fluxo no pudo crear la suscripción de {Email} (con tarjeta: {ConTarjeta}): {Mensaje}",
                email, conTarjeta, body?.Mensaje);
            throw new AppException(body?.Mensaje ?? "Fluxo no pudo crear la suscripción.");
        }

        var c = body!.Contenido!;
        return new FluxoSuscripcionResultado(
            c.ClienteId, c.UsuarioId, c.Email, c.PasswordTemporal, c.InitPoint, c.MpSuscripcionId, c.EstadoSuscripcion);
    }

    public async Task<FluxoLinkPago> ObtenerLinkPagoAsync(int suscripcionId, CancellationToken ct = default)
    {
        var baseUrl = _config["Fluxo:BaseUrl"];
        var apiKey = _config["Fluxo:VendedorApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Fluxo:BaseUrl o Fluxo:VendedorApiKey no configurados.");
            throw new AppException("La integración con Fluxo no está configurada.");
        }

        FluxoRespuestaLink? body;
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/api/vendedor/suscripciones/{suscripcionId}/link");
            request.Headers.Add("X-Vendedor-Api-Key", apiKey);

            var response = await _http.SendAsync(request, ct);
            body = await response.Content.ReadFromJsonAsync<FluxoRespuestaLink>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a Fluxo para traer el link de la suscripción {Id}.", suscripcionId);
            throw new AppException("No se pudo conectar con Fluxo para traer el link. Probá de nuevo en un momento.");
        }

        if (body is not { Exitoso: true, Contenido: not null })
        {
            _logger.LogWarning("Fluxo no devolvió el link de la suscripción {Id}: {Mensaje}", suscripcionId, body?.Mensaje);
            throw new AppException(body?.Mensaje ?? "Fluxo no pudo traer el link de pago.");
        }

        return new FluxoLinkPago(body.Contenido.Estado, body.Contenido.InitPoint);
    }

    public async Task<IReadOnlyList<FluxoPlan>> ListarPlanesAsync(CancellationToken ct = default)
    {
        var baseUrl = _config["Fluxo:BaseUrl"];
        var apiKey = _config["Fluxo:VendedorApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Fluxo:BaseUrl o Fluxo:VendedorApiKey no configurados — no se pueden listar los planes.");
            return [];
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/api/vendedor/planes");
            request.Headers.Add("X-Vendedor-Api-Key", apiKey);

            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Fluxo devolvió {StatusCode} al listar los planes.", response.StatusCode);
                return [];
            }

            var body = await response.Content.ReadFromJsonAsync<FluxoRespuestaPlanes>(JsonOptions, ct);
            if (body is null || !body.Exitoso || body.Contenido is null)
            {
                _logger.LogWarning("Fluxo no pudo listar los planes: {Mensaje}", body?.Mensaje);
                return [];
            }

            return body.Contenido
                .Select(p => new FluxoPlan(p.MpPlanId, p.Nombre, p.Monto, p.Moneda, p.TipoFrecuencia, p.Frecuencia, p.DiasGratis))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a Fluxo para listar los planes.");
            return [];
        }
    }

    public async Task<IReadOnlyDictionary<int, string>> ObtenerEstadosSuscripcionesAsync(
        IEnumerable<int> suscripcionIds, CancellationToken ct = default)
    {
        var confirmaciones = await ObtenerConfirmacionesAsync(suscripcionIds, ct);
        return confirmaciones is null
            ? new Dictionary<int, string>()
            : confirmaciones.ToDictionary(e => e.Key, e => e.Value.Estado);
    }

    public async Task<IReadOnlyDictionary<int, FluxoEstadoSuscripcion>?> ObtenerConfirmacionesAsync(
        IEnumerable<int> suscripcionIds, CancellationToken ct = default)
    {
        var ids = suscripcionIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<int, FluxoEstadoSuscripcion>();

        var baseUrl = _config["Fluxo:BaseUrl"];
        var apiKey = _config["Fluxo:VendedorApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            return null;

        try
        {
            var idsQuery = string.Join(',', ids);
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/api/vendedor/suscripciones/estados?ids={idsQuery}");
            request.Headers.Add("X-Vendedor-Api-Key", apiKey);

            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Fluxo devolvió {StatusCode} al pedir estados de suscripciones.", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<FluxoRespuestaEstados>(JsonOptions, ct);
            if (body is null || !body.Exitoso || body.Contenido is null)
                return null;

            return body.Contenido.ToDictionary(
                e => e.MpSuscripcionId, e => new FluxoEstadoSuscripcion(e.Estado, e.Confirmada));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a Fluxo para pedir estados de suscripciones.");
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
        public string? EstadoSuscripcion { get; set; }
    }

    private class FluxoRespuestaLink
    {
        public bool Exitoso { get; set; }
        public string? Mensaje { get; set; }
        public FluxoLinkContenido? Contenido { get; set; }
    }

    private class FluxoLinkContenido
    {
        public string Estado { get; set; } = string.Empty;
        public string? InitPoint { get; set; }
    }

    private class FluxoRespuestaPlanes
    {
        public bool Exitoso { get; set; }
        public string? Mensaje { get; set; }
        public List<FluxoPlanContenido>? Contenido { get; set; }
    }

    private class FluxoPlanContenido
    {
        public int MpPlanId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public string TipoFrecuencia { get; set; } = string.Empty;
        public int Frecuencia { get; set; }
        public int DiasGratis { get; set; }
    }

    private class FluxoRespuestaEstados
    {
        public bool Exitoso { get; set; }
        public string? Mensaje { get; set; }
        public List<FluxoEstadoContenido>? Contenido { get; set; }
    }

    private class FluxoEstadoContenido
    {
        public int MpSuscripcionId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool Confirmada { get; set; }
    }
}
