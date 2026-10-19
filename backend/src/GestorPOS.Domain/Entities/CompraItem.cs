using GestorPOS.Domain.Common;
using GestorPOS.Domain.Enums;

namespace GestorPOS.Domain.Entities;

/// <summary>Línea de una compra. ProductoNombre y CostoUnitario quedan "congelados" al momento de
/// comprar, mismo patrón que VentaItem — el historial no cambia si después se edita el producto.
/// Si se compró por presentación ("2 bolsas de 15 kg"), Cantidad cuenta presentaciones, CostoUnitario es lo
/// que costó cada una y FactorBase dice cuántas unidades del producto vale cada una.</summary>
public class CompraItem : BaseEntity
{
    public Guid CompraId { get; private set; }
    public Guid ProductoId { get; private set; }
    public string ProductoNombre { get; private set; } = string.Empty;
    public decimal Cantidad { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public decimal Subtotal { get; private set; }
    public string? PresentacionNombre { get; private set; }
    public decimal FactorBase { get; private set; } = 1m;
    public UnidadMedida Unidad { get; private set; } = UnidadMedida.Unidad;

    /// <summary>Lo que suma al stock del producto, en su unidad.</summary>
    public decimal CantidadBase => Cantidad * FactorBase;
    /// <summary>Costo por unidad del producto (por kilo, por litro...), el que pasa a ser el costo vigente.</summary>
    public decimal CostoPorUnidadBase => ReglasCantidad.RedondearImporte(CostoUnitario / FactorBase);

    private CompraItem() { }

    internal static CompraItem Crear(Producto producto, decimal cantidad, decimal costoUnitario,
        ProductoPresentacion? presentacion = null)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));
        if (costoUnitario < 0)
            throw new ArgumentException("El costo no puede ser negativo.", nameof(costoUnitario));

        decimal factor = 1m;
        string? nombrePresentacion = null;

        if (presentacion is null)
        {
            ReglasCantidad.Validar(producto.Unidad, cantidad);
        }
        else
        {
            if (presentacion.ProductoId != producto.Id)
                throw new ArgumentException("La presentación no pertenece al producto.", nameof(presentacion));
            if (cantidad != decimal.Truncate(cantidad))
                throw new ArgumentException("Las presentaciones se compran de a una: la cantidad tiene que ser un número entero.", nameof(cantidad));
            factor = presentacion.CantidadBase;
            nombrePresentacion = presentacion.Nombre;
        }

        return new CompraItem
        {
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            Subtotal = ReglasCantidad.RedondearImporte(costoUnitario * cantidad),
            PresentacionNombre = nombrePresentacion,
            FactorBase = factor,
            Unidad = producto.Unidad
        };
    }

    internal void AsignarCompra(Guid compraId) => CompraId = compraId;
}
