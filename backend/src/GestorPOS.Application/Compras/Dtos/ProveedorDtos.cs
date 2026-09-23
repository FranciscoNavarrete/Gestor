namespace GestorPOS.Application.Compras.Dtos;

public record ProveedorDto(Guid Id, string Nombre, string? Cuit, string? Telefono, string? Email, bool Activo);

public record CrearProveedorRequest(string Nombre, string? Cuit, string? Telefono, string? Email);

public record EditarProveedorRequest(string Nombre, string? Cuit, string? Telefono, string? Email);
