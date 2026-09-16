namespace GestorPOS.Application.Configuracion.Dtos;

public record NegocioDto(string Nombre, string? Telefono, bool TieneLogo);

public record ActualizarNegocioRequest(string Nombre, string? Telefono);
