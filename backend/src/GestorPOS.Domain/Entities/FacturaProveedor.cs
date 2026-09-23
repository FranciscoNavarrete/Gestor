using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Factura recibida de un proveedor — cuenta a pagar. Independiente de Compra: no mueve
/// stock ni costo, solo lleva el registro de qué le debés a cada proveedor.</summary>
public class FacturaProveedor : TenantEntity
{
    public Guid ProveedorId { get; private set; }
    public string ProveedorNombre { get; private set; } = string.Empty;
    public string NumeroFactura { get; private set; } = string.Empty;
    public decimal Monto { get; private set; }
    public DateOnly FechaEmision { get; private set; }
    public DateOnly? FechaVencimiento { get; private set; }
    public Guid UsuarioId { get; private set; }

    private FacturaProveedor() { }

    public static FacturaProveedor Crear(
        Guid tenantId, Proveedor proveedor, string numeroFactura, decimal monto,
        DateOnly fechaEmision, DateOnly? fechaVencimiento, Guid usuarioId)
    {
        if (string.IsNullOrWhiteSpace(numeroFactura))
            throw new ArgumentException("El número de factura es obligatorio.", nameof(numeroFactura));
        if (monto <= 0)
            throw new ArgumentException("El monto de la factura tiene que ser mayor a cero.", nameof(monto));

        return new FacturaProveedor
        {
            TenantId = tenantId,
            ProveedorId = proveedor.Id,
            ProveedorNombre = proveedor.Nombre,
            NumeroFactura = numeroFactura.Trim(),
            Monto = monto,
            FechaEmision = fechaEmision,
            FechaVencimiento = fechaVencimiento,
            UsuarioId = usuarioId,
        };
    }
}
