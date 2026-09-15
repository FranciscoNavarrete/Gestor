using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Línea de una venta. Guarda nombre y precio "congelados" al momento de vender,
/// para que la venta no cambie si después se edita o se borra el producto.</summary>
public class VentaItem : BaseEntity
{
    public Guid VentaId { get; private set; }
    public Guid ProductoId { get; private set; }
    public string ProductoNombre { get; private set; } = string.Empty;
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Subtotal { get; private set; }

    private VentaItem() { }

    internal static VentaItem Crear(Producto producto, int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));

        return new VentaItem
        {
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            Cantidad = cantidad,
            PrecioUnitario = producto.Precio,
            Subtotal = producto.Precio * cantidad
        };
    }

    internal void AsignarVenta(Guid ventaId) => VentaId = ventaId;
}
