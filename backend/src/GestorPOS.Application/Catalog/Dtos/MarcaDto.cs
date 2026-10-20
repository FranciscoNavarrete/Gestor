namespace GestorPOS.Application.Catalog.Dtos;

public record MarcaDto(Guid Id, string Nombre, bool Activo);

public record CrearMarcaRequest(string Nombre);

public record EditarMarcaRequest(string Nombre);
