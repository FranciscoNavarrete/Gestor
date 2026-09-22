using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class PagoCuenta : TenantEntity
{
    public Guid ClienteId { get; private set; }
    public decimal Monto { get; private set; }
    public string MedioPago { get; private set; } = string.Empty;
    public Guid UsuarioId { get; private set; }

    private PagoCuenta() { }

    public static PagoCuenta Crear(Guid tenantId, Guid clienteId, decimal monto, string medioPago, Guid usuarioId)
    {
        if (monto <= 0)
            throw new ArgumentException("El monto del pago tiene que ser mayor a cero.", nameof(monto));
        if (string.IsNullOrWhiteSpace(medioPago))
            throw new ArgumentException("Elegí el medio de pago.", nameof(medioPago));

        return new PagoCuenta
        {
            TenantId = tenantId,
            ClienteId = clienteId,
            Monto = monto,
            MedioPago = medioPago.Trim(),
            UsuarioId = usuarioId,
        };
    }
}
