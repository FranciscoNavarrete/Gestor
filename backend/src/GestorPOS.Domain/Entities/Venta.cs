using GestorPOS.Domain.Common;
using GestorPOS.Domain.Enums;

namespace GestorPOS.Domain.Entities;

public class Venta : TenantEntity
{
    public Guid UsuarioId { get; private set; }
    public MedioPago MedioPago { get; private set; }
    public string? TelefonoCliente { get; private set; }
    public decimal Total { get; private set; }

    private readonly List<VentaItem> _items = new();
    public IReadOnlyCollection<VentaItem> Items => _items.AsReadOnly();

    private Venta() { }

    public static Venta Crear(
        Guid tenantId, Guid usuarioId, MedioPago medioPago, string? telefonoCliente,
        IEnumerable<(Producto Producto, int Cantidad)> items)
    {
        var venta = new Venta
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            MedioPago = medioPago,
            TelefonoCliente = string.IsNullOrWhiteSpace(telefonoCliente) ? null : telefonoCliente.Trim()
        };

        var itemsVenta = items.Select(i => VentaItem.Crear(i.Producto, i.Cantidad)).ToList();
        if (itemsVenta.Count == 0)
            throw new ArgumentException("La venta debe tener al menos un producto.", nameof(items));

        foreach (var item in itemsVenta)
            item.AsignarVenta(venta.Id);

        venta._items.AddRange(itemsVenta);
        venta.Total = itemsVenta.Sum(i => i.Subtotal);

        return venta;
    }
}
