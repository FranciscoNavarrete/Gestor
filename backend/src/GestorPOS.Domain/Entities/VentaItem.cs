using GestorPOS.Domain.Common;
using GestorPOS.Domain.Enums;

namespace GestorPOS.Domain.Entities;

/// <summary>Línea de una venta. Guarda nombre, precio y costo "congelados" al momento de vender,
/// para que la venta (y el reporte de ganancias) no cambien si después se edita el producto.
/// Si se vendió una presentación, Cantidad cuenta presentaciones, PrecioUnitario y CostoUnitario son por
/// presentación y FactorBase dice cuántas unidades del producto vale cada una. En una línea común
/// FactorBase es 1 y Unidad es la del producto.</summary>
public class VentaItem : BaseEntity
{
    public Guid VentaId { get; private set; }
    public Guid ProductoId { get; private set; }
    public string ProductoNombre { get; private set; } = string.Empty;
    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public decimal Subtotal { get; private set; }
    /// <summary>Nombre de la presentación vendida ("Bolsa 15 kg"); null cuando se vendió suelto o por unidad.</summary>
    public string? PresentacionNombre { get; private set; }
    /// <summary>Unidades del producto que vale cada unidad de Cantidad (1 en una línea común).</summary>
    public decimal FactorBase { get; private set; } = 1m;
    /// <summary>Unidad del producto al momento de vender, para mostrar la cantidad con su unidad.</summary>
    public UnidadMedida Unidad { get; private set; } = UnidadMedida.Unidad;

    /// <summary>Lo que se descuenta del stock del producto, en su unidad.</summary>
    public decimal CantidadBase => Cantidad * FactorBase;
    public decimal GananciaNeta => Subtotal - ReglasCantidad.RedondearImporte(CostoUnitario * Cantidad);

    private VentaItem() { }

    internal static VentaItem Crear(Producto producto, decimal cantidad, ProductoPresentacion? presentacion = null)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));

        decimal precio, costo, factor;
        string? nombrePresentacion = null;

        if (presentacion is null)
        {
            ReglasCantidad.Validar(producto.Unidad, cantidad);
            precio = producto.Precio;
            costo = producto.Costo;
            factor = 1m;
        }
        else
        {
            if (presentacion.ProductoId != producto.Id)
                throw new ArgumentException("La presentación no pertenece al producto.", nameof(presentacion));
            if (cantidad != decimal.Truncate(cantidad))
                throw new ArgumentException("Las presentaciones se venden de a una: la cantidad tiene que ser un número entero.", nameof(cantidad));
            precio = presentacion.Precio;
            factor = presentacion.CantidadBase;
            costo = ReglasCantidad.RedondearImporte(producto.Costo * factor);
            nombrePresentacion = presentacion.Nombre;
        }

        return new VentaItem
        {
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            Cantidad = cantidad,
            PrecioUnitario = precio,
            CostoUnitario = costo,
            Subtotal = ReglasCantidad.RedondearImporte(precio * cantidad),
            PresentacionNombre = nombrePresentacion,
            FactorBase = factor,
            Unidad = producto.Unidad
        };
    }

    internal void AsignarVenta(Guid ventaId) => VentaId = ventaId;
}
