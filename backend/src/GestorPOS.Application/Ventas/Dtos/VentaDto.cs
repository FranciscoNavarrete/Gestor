namespace GestorPOS.Application.Ventas.Dtos;

public record ItemVentaRequest(Guid ProductoId, int Cantidad);

public record CrearVentaRequest(IReadOnlyList<ItemVentaRequest> Items, string MedioPago, string? TelefonoCliente);

public record VentaItemDto(Guid ProductoId, string ProductoNombre, int Cantidad, decimal PrecioUnitario, decimal Subtotal);

public record VentaDto(
    Guid Id,
    DateTime Fecha,
    decimal Total,
    string MedioPago,
    string? TelefonoCliente,
    IReadOnlyList<VentaItemDto> Items,
    string TicketTexto,
    string? WhatsAppLink);

public record VentaResumenDto(Guid Id, DateTime Fecha, decimal Total, string MedioPago);
