namespace GestorPOS.Application.Ventas.Dtos;

/// <summary>PresentacionId vacío = suelto / por unidad (lo de siempre). Con presentación, Cantidad cuenta presentaciones.</summary>
public record ItemVentaRequest(Guid ProductoId, decimal Cantidad, Guid? PresentacionId = null);

public record CrearVentaRequest(
    IReadOnlyList<ItemVentaRequest> Items, string MedioPago, string? TelefonoCliente, string? NombreCliente);

public record VentaItemDto(
    Guid ProductoId, string ProductoNombre, decimal Cantidad, decimal PrecioUnitario, decimal Subtotal,
    string? PresentacionNombre = null, string Unidad = "Unidad");

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
