namespace GestorPOS.Application.Compras.Dtos;

/// <summary>Con PresentacionId, Cantidad cuenta presentaciones y CostoUnitario es lo que costó cada una.
/// Sin presentación, Cantidad y CostoUnitario son por unidad del producto (lo de siempre).</summary>
public record ItemCompraRequest(Guid ProductoId, decimal Cantidad, decimal CostoUnitario, Guid? PresentacionId = null);

public record CrearCompraRequest(Guid ProveedorId, IReadOnlyList<ItemCompraRequest> Items);

public record CompraItemDto(
    Guid ProductoId, string ProductoNombre, decimal Cantidad, decimal CostoUnitario, decimal Subtotal,
    string? PresentacionNombre = null, string Unidad = "Unidad");

public record CompraDto(
    Guid Id,
    DateTime Fecha,
    Guid ProveedorId,
    string ProveedorNombre,
    decimal Total,
    IReadOnlyList<CompraItemDto> Items);

public record CompraResumenDto(Guid Id, DateTime Fecha, string ProveedorNombre, int CantidadItems, decimal Total);

public record ResumenComprasDto(int CantidadCompras, decimal TotalComprado, int CantidadProveedores);
