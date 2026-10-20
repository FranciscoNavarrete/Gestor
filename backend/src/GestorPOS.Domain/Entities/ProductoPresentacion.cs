using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Forma alternativa de vender o comprar un producto: "Bolsa 15 kg", "Caja x12". Tiene su propio precio
/// de venta. No tiene stock propio: vender o comprar N presentaciones mueve el stock del producto en
/// N × CantidadBase de su unidad (kilos, litros, metros o unidades). El suelto no es una presentación: es el
/// precio y la unidad del propio producto.</summary>
public class ProductoPresentacion : TenantEntity
{
    public Guid ProductoId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    /// <summary>Cuántas unidades del producto (kilos, litros, metros o unidades) trae una presentación.</summary>
    public decimal CantidadBase { get; private set; }
    public decimal Precio { get; private set; }
    public bool Activo { get; private set; } = true;

    private ProductoPresentacion() { }

    public static ProductoPresentacion Crear(Producto producto, string nombre, decimal cantidadBase, decimal precio)
    {
        Validar(producto, nombre, cantidadBase, precio);
        return new ProductoPresentacion
        {
            TenantId = producto.TenantId,
            ProductoId = producto.Id,
            Nombre = nombre.Trim(),
            CantidadBase = cantidadBase,
            Precio = precio
        };
    }

    public void Editar(Producto producto, string nombre, decimal cantidadBase, decimal precio)
    {
        Validar(producto, nombre, cantidadBase, precio);
        Nombre = nombre.Trim();
        CantidadBase = cantidadBase;
        Precio = precio;
    }

    public void ActualizarPrecio(decimal nuevoPrecio)
    {
        if (nuevoPrecio < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(nuevoPrecio));
        Precio = nuevoPrecio;
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;

    private static void Validar(Producto producto, string nombre, decimal cantidadBase, decimal precio)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la presentación es obligatorio.", nameof(nombre));
        if (cantidadBase <= 0)
            throw new ArgumentException("La cantidad de la presentación tiene que ser mayor a cero.", nameof(cantidadBase));
        ReglasCantidad.Validar(producto.Unidad, cantidadBase, "La cantidad de la presentación");
        if (precio < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(precio));
    }
}
