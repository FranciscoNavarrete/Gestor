namespace GestorPOS.Application.FacturasProveedor.Dtos;

public record CrearFacturaProveedorRequest(
    Guid ProveedorId, string NumeroFactura, decimal Monto, DateOnly FechaEmision, DateOnly? FechaVencimiento);

public record RegistrarPagoFacturaRequest(decimal Monto, string MedioPago);

public record FacturaProveedorDto(
    Guid Id,
    Guid ProveedorId,
    string ProveedorNombre,
    string NumeroFactura,
    decimal Monto,
    decimal Saldo,
    string Estado,
    DateOnly FechaEmision,
    DateOnly? FechaVencimiento,
    DateTime FechaCreacion);

public record ResumenFacturasProveedorDto(decimal TotalAPagar, int CantidadPendientes);
