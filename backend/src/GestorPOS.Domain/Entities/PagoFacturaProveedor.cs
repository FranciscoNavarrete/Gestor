using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Pago (total o parcial) registrado contra una FacturaProveedor. Mismo patrón que
/// PagoCuenta para la cuenta corriente de clientes, pero del lado de lo que la empresa debe.</summary>
public class PagoFacturaProveedor : TenantEntity
{
    public Guid FacturaProveedorId { get; private set; }
    public decimal Monto { get; private set; }
    public string MedioPago { get; private set; } = string.Empty;
    public Guid UsuarioId { get; private set; }

    private PagoFacturaProveedor() { }

    public static PagoFacturaProveedor Crear(Guid tenantId, Guid facturaProveedorId, decimal monto, string medioPago, Guid usuarioId)
    {
        if (monto <= 0)
            throw new ArgumentException("El monto del pago tiene que ser mayor a cero.", nameof(monto));
        if (string.IsNullOrWhiteSpace(medioPago))
            throw new ArgumentException("Elegí el medio de pago.", nameof(medioPago));

        return new PagoFacturaProveedor
        {
            TenantId = tenantId,
            FacturaProveedorId = facturaProveedorId,
            Monto = monto,
            MedioPago = medioPago.Trim(),
            UsuarioId = usuarioId,
        };
    }
}
