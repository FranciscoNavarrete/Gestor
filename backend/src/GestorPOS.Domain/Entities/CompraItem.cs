using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Línea de una compra. ProductoNombre y CostoUnitario quedan "congelados" al momento de
/// comprar, mismo patrón que VentaItem — el historial no cambia si después se edita el producto.</summary>
public class CompraItem : BaseEntity
{
    public Guid CompraId { get; private set; }
    public Guid ProductoId { get; private set; }
    public string ProductoNombre { get; private set; } = string.Empty;
    public int Cantidad { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public decimal Subtotal { get; private set; }

    private CompraItem() { }

    internal static CompraItem Crear(Producto producto, int cantidad, decimal costoUnitario)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));
        if (costoUnitario < 0)
            throw new ArgumentException("El costo no puede ser negativo.", nameof(costoUnitario));

        return new CompraItem
        {
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            Subtotal = costoUnitario * cantidad
        };
    }

    internal void AsignarCompra(Guid compraId) => CompraId = compraId;
}
