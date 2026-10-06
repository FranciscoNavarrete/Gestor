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
        bool primerPagoManual = false, bool sinAlta = false, CancellationToken ct = default)
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
            request.Content = JsonContent.Create(new { nombre, apellido, email, mpPlanId, cardTokenId, primerPagoManual, sinAlta });

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

    public async Task<FluxoCobros> ObtenerCobrosAsync(int suscripcionId, CancellationToken ct = default)
    {
        var baseUrl = _config["Fluxo:BaseUrl"];
        var apiKey = _config["Fluxo:VendedorApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            throw new AppException("La integración con Fluxo no está configurada.");

        FluxoRespuestaCobros? body;
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/api/vendedor/suscripciones/{suscripcionId}/cobros");
            request.Headers.Add("X-Vendedor-Api-Key", apiKey);

            var response = await _http.SendAsync(request, ct);
            body = await response.Content.ReadFromJsonAsync<FluxoRespuestaCobros>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a Fluxo para traer los cobros de la suscripción {Id}.", suscripcionId);
            throw new AppException("No se pudo conectar con Fluxo para traer los cobros. Probá de nuevo en un momento.");
        }

        if (body is not { Exitoso: true, Contenido: not null })
        {
            _logger.LogWarning("Fluxo no devolvió los cobros de la suscripción {Id}: {Mensaje}", suscripcionId, body?.Mensaje);
            throw new AppException(body?.Mensaje ?? "Fluxo no pudo traer los cobros.");
        }

        var c = body.Contenido;
        return new FluxoCobros(
            c.Estado, c.MontoMensual, c.ProximoCobro, c.ProximoMonto,
            (c.Cobros ?? []).Select(x => new FluxoCobro(x.Fecha, x.Monto, x.Estado, x.Motivo, x.Intento, x.ProximoReintento, x.EsPrimerCobro)).ToList(),
            c.TarjetaEditable, c.MpPlanId, c.PlanNombre, c.MontoNormal, c.Promo?.ComoPromo());
    }

    public async Task CambiarPlanAsync(int suscripcionId, int mpPlanId, CancellationToken ct = default)
    {
        var baseUrl = _config["Fluxo:BaseUrl"];
        var apiKey = _config["Fluxo:VendedorApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            throw new AppException("La integración con Fluxo no está configurada.");

        FluxoRespuestaSimple? body;
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put, $"{baseUrl.TrimEnd('/')}/api/vendedor/suscripciones/{suscripcionId}/plan")
            {
                Content = JsonContent.Create(new { mpPlanId }),
            };
            request.Headers.Add("X-Vendedor-Api-Key", apiKey);

            var response = await _http.SendAsync(request, ct);
            body = await response.Content.ReadFromJsonAsync<FluxoRespuestaSimple>(JsonOptions, ct);
            if (response.IsSuccessStatusCode && body?.Exitoso == true) return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a Fluxo para cambiar el plan de la suscripción {Id}.", suscripcionId);
            throw new AppException("No se pudo conectar para cambiar el plan. Probá de nuevo en un momento.");
        }

        _logger.LogWarning("Fluxo no pudo cambiar el plan de la suscripción {Id}: {Mensaje}", suscripcionId, body?.Mensaje);
        throw new AppException(body?.Mensaje ?? "No se pudo cambiar el plan.");
    }

    public async Task CambiarTarjetaAsync(int suscripcionId, string cardTokenId, CancellationToken ct = default)
    {
        var baseUrl = _config["Fluxo:BaseUrl"];
        var apiKey = _config["Fluxo:VendedorApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            throw new AppException("La integración con Fluxo no está configurada.");

        FluxoRespuestaSimple? body;
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put, $"{baseUrl.TrimEnd('/')}/api/vendedor/suscripciones/{suscripcionId}/tarjeta")
            {
                Content = JsonContent.Create(new { cardTokenId }),
            };
            request.Headers.Add("X-Vendedor-Api-Key", apiKey);

            var response = await _http.SendAsync(request, ct);
            body = await response.Content.ReadFromJsonAsync<FluxoRespuestaSimple>(JsonOptions, ct);
            if (response.IsSuccessStatusCode && body?.Exitoso == true) return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a Fluxo para cambiar la tarjeta de la suscripción {Id}.", suscripcionId);
            throw new AppException("No se pudo conectar para cambiar la tarjeta. Probá de nuevo en un momento.");
        }

        _logger.LogWarning("Fluxo no pudo cambiar la tarjeta de la suscripción {Id}: {Mensaje}", suscripcionId, body?.Mensaje);
        throw new AppException(body?.Mensaje ?? "No se pudo cambiar la tarjeta. Revisá los datos y probá de nuevo.");
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
                .Select(p => new FluxoPlan(p.MpPlanId, p.Nombre, p.Monto, p.Moneda, p.TipoFrecuencia, p.Frecuencia, p.DiasGratis, p.MontoPrimerCobro, p.MontoPromo, p.MesesPromo))
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
            // En tandas, para no armar una URL gigante cuando hay muchos negocios.
            var resultado = new Dictionary<int, FluxoEstadoSuscripcion>();
            foreach (var tanda in ids.Chunk(150))
            {
                var idsQuery = string.Join(',', tanda);
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

                foreach (var e in body.Contenido)
                {
                    resultado[e.MpSuscripcionId] = new FluxoEstadoSuscripcion(
                        e.Estado, e.Confirmada, e.PrimerCobroAprobado, e.AjusteMontoPendiente, e.CobroRechazado, e.MotivoRechazo,
                        e.ProximoReintento, e.MontoMensual, e.MontoProximoCobro, e.ProximoCobro, e.FechaInicio, e.FechaCancelacion,
                        e.MpPlanId, e.PlanNombre, e.Promo?.ComoPromo());
                }
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a Fluxo para pedir estados de suscripciones.");
            return null;
        }
    }

    // Respuestas cuyo contenido no se lee (solo importa si salió bien y el mensaje).
    private class FluxoRespuestaSimple
    {
        public bool Exitoso { get; set; }
        public string? Mensaje { get; set; }
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
        public decimal? MontoPrimerCobro { get; set; }
        public decimal? MontoPromo { get; set; }
        public int? MesesPromo { get; set; }
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
        public bool PrimerCobroAprobado { get; set; }
        public bool AjusteMontoPendiente { get; set; }
        public bool CobroRechazado { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTime? ProximoReintento { get; set; }
        public decimal MontoMensual { get; set; }
        public decimal MontoProximoCobro { get; set; }
        public DateTime? ProximoCobro { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public int MpPlanId { get; set; }
        public string? PlanNombre { get; set; }
        public FluxoPromoContenido? Promo { get; set; }
    }

    private class FluxoPromoContenido
    {
        public decimal MontoPromo { get; set; }
        public int MesesPromo { get; set; }
        public int MesActual { get; set; }
        public decimal MontoNormal { get; set; }
        public DateTime? UltimoCobroPromo { get; set; }
        public DateTime? NormalDesde { get; set; }

        public FluxoPromo ComoPromo() => new(MontoPromo, MesesPromo, MesActual, MontoNormal, UltimoCobroPromo, NormalDesde);
    }

    private class FluxoRespuestaCobros
    {
        public bool Exitoso { get; set; }
        public string? Mensaje { get; set; }
        public FluxoCobrosContenido? Contenido { get; set; }
    }

    private class FluxoCobrosContenido
    {
        public string Estado { get; set; } = string.Empty;
        public decimal MontoMensual { get; set; }
        public DateTime? ProximoCobro { get; set; }
        public decimal? ProximoMonto { get; set; }
        public bool TarjetaEditable { get; set; }
        public int MpPlanId { get; set; }
        public string? PlanNombre { get; set; }
        public decimal MontoNormal { get; set; }
        public FluxoPromoContenido? Promo { get; set; }
        public List<FluxoCobroContenido>? Cobros { get; set; }
    }

    private class FluxoCobroContenido
    {
        public DateTime? Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public int Intento { get; set; }
        public DateTime? ProximoReintento { get; set; }
        public bool EsPrimerCobro { get; set; }
    }
}
