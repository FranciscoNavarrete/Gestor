namespace GestorPOS.Application.Notificaciones.Dtos;

public record ClaveVapidDto(string ClavePublica);

public record SuscribirsePushRequest(string Endpoint, string P256dh, string Auth);

public record DesuscribirsePushRequest(string Endpoint);

public record SuscripcionPushResumenDto(string EndpointResumido, DateTime FechaCreacion);

public record ResultadoPruebaPushDto(string EndpointResumido, bool Exito, string? Error);
