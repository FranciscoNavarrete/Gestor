namespace GestorPOS.Application.Catalog.Dtos;

public record CategoriaDto(Guid Id, string Nombre, bool Activo);

public record CrearCategoriaRequest(string Nombre);

public record EditarCategoriaRequest(string Nombre);
