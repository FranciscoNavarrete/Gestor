namespace GestorPOS.Application.Configuracion.Dtos;

public record MedioPagoDto(Guid Id, string Nombre, bool Activo, bool EsProtegido);

public record CrearMedioPagoRequest(string Nombre);

public record EditarMedioPagoRequest(string Nombre);
