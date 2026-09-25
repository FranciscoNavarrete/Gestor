using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class MovimientoCaja : TenantEntity
{
    public Guid CajaDiariaId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public decimal Monto { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public Guid UsuarioId { get; private set; }

    private MovimientoCaja() { }

    public static MovimientoCaja Crear(Guid tenantId, Guid cajaDiariaId, string tipo, decimal monto, string motivo, Guid usuarioId)
    {
        if (tipo != "Ingreso" && tipo != "Egreso")
            throw new ArgumentException("El tipo tiene que ser Ingreso o Egreso.", nameof(tipo));
        if (monto <= 0)
            throw new ArgumentException("El monto tiene que ser mayor a cero.", nameof(monto));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Ingresá un motivo para el movimiento.", nameof(motivo));

        return new MovimientoCaja
        {
            TenantId = tenantId,
            CajaDiariaId = cajaDiariaId,
            Tipo = tipo,
            Monto = monto,
            Motivo = motivo.Trim(),
            UsuarioId = usuarioId,
        };
    }
}
