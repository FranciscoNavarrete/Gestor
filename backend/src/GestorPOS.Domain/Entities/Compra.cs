using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class Compra : TenantEntity
{
    public Guid ProveedorId { get; private set; }
    public string ProveedorNombre { get; private set; } = string.Empty;
    public Guid UsuarioId { get; private set; }
    public decimal Total { get; private set; }

    private readonly List<CompraItem> _items = new();
    public IReadOnlyCollection<CompraItem> Items => _items.AsReadOnly();

    private Compra() { }

    public static Compra Crear(
        Guid tenantId, Guid usuarioId, Proveedor proveedor,
        IEnumerable<(Producto Producto, int Cantidad, decimal CostoUnitario)> items)
    {
        var compra = new Compra
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            ProveedorId = proveedor.Id,
            ProveedorNombre = proveedor.Nombre
        };

        var itemsCompra = items.Select(i => CompraItem.Crear(i.Producto, i.Cantidad, i.CostoUnitario)).ToList();
        if (itemsCompra.Count == 0)
            throw new ArgumentException("La compra debe tener al menos un producto.", nameof(items));

        foreach (var item in itemsCompra)
            item.AsignarCompra(compra.Id);

        compra._items.AddRange(itemsCompra);
        compra.Total = itemsCompra.Sum(i => i.Subtotal);

        return compra;
    }
}
