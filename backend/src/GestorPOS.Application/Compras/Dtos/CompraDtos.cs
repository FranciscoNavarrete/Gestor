namespace GestorPOS.Application.Compras.Dtos;

public record ItemCompraRequest(Guid ProductoId, int Cantidad, decimal CostoUnitario);

public record CrearCompraRequest(Guid ProveedorId, IReadOnlyList<ItemCompraRequest> Items);

public record CompraItemDto(Guid ProductoId, string ProductoNombre, int Cantidad, decimal CostoUnitario, decimal Subtotal);

public record CompraDto(
    Guid Id,
    DateTime Fecha,
    Guid ProveedorId,
    string ProveedorNombre,
    decimal Total,
    IReadOnlyList<CompraItemDto> Items);

public record CompraResumenDto(Guid Id, DateTime Fecha, string ProveedorNombre, int CantidadItems, decimal Total);

public record ResumenComprasDto(int CantidadCompras, decimal TotalComprado, int CantidadProveedores);
