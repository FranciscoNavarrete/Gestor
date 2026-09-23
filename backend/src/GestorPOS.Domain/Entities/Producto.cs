using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class Producto : TenantEntity
{
    public string Sku { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public Guid? CategoriaId { get; private set; }
    public decimal Precio { get; private set; }
    public decimal Costo { get; private set; }
    public int StockActual { get; private set; }
    public int StockMinimo { get; private set; }
    public bool Activo { get; private set; } = true;

    public bool EnStockMinimo => StockActual <= StockMinimo;

    private Producto() { }

    public static Producto Crear(
        Guid tenantId, string sku, string nombre, Guid? categoriaId,
        decimal precio, decimal costo, int stockActual, int stockMinimo)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("El SKU es obligatorio.", nameof(sku));
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (precio < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(precio));
        if (costo < 0)
            throw new ArgumentException("El costo no puede ser negativo.", nameof(costo));
        if (stockActual < 0)
            throw new ArgumentException("El stock actual no puede ser negativo.", nameof(stockActual));
        if (stockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(stockMinimo));

        return new Producto
        {
            TenantId = tenantId,
            Sku = sku.Trim(),
            Nombre = nombre.Trim(),
            CategoriaId = categoriaId,
            Precio = precio,
            Costo = costo,
            StockActual = stockActual,
            StockMinimo = stockMinimo
        };
    }

    public void Editar(string nombre, Guid? categoriaId, decimal precio, decimal costo, int stockMinimo)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (precio < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(precio));
        if (costo < 0)
            throw new ArgumentException("El costo no puede ser negativo.", nameof(costo));
        if (stockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(stockMinimo));

        Nombre = nombre.Trim();
        CategoriaId = categoriaId;
        Precio = precio;
        Costo = costo;
        StockMinimo = stockMinimo;
    }

    public void ActualizarPrecio(decimal nuevoPrecio)
    {
        if (nuevoPrecio < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(nuevoPrecio));
        Precio = nuevoPrecio;
    }

    /// <summary>Actualiza el costo vigente del producto — se llama al registrar una compra, con el
    /// costo unitario de esa compra (método "último costo", no promedio ponderado).</summary>
    public void ActualizarCosto(decimal nuevoCosto)
    {
        if (nuevoCosto < 0)
            throw new ArgumentException("El costo no puede ser negativo.", nameof(nuevoCosto));
        Costo = nuevoCosto;
    }

    public void AjustarStock(int cantidad)
    {
        var nuevoStock = StockActual + cantidad;
        if (nuevoStock < 0)
            throw new InvalidOperationException($"No hay stock suficiente de '{Nombre}' (disponible: {StockActual}).");
        StockActual = nuevoStock;
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
