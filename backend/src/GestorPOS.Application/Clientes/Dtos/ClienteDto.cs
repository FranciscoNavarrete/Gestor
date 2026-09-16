namespace GestorPOS.Application.Clientes.Dtos;

public record ClienteDto(Guid Id, string Telefono, string? Nombre, DateTime FechaAlta, int CantidadCompras);

public record ClienteDetalleDto(
    Guid Id,
    string Telefono,
    string? Nombre,
    DateTime FechaAlta,
    int CantidadCompras,
    decimal TotalGastado,
    DateTime? UltimaCompra);

public record ClienteVentaDto(Guid Id, DateTime Fecha, decimal Total, string MedioPago);

public record EditarClienteRequest(string? Nombre);
