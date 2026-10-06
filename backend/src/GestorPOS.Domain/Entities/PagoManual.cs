namespace GestorPOS.Domain.Entities;

public enum MetodoPagoManual { Efectivo, Transferencia, Tarjeta, Otro }

public enum EstadoPagoManual { Pendiente, Confirmado }

/// <summary>Un pago recibido por fuera de Mercado Pago, hoy el primer pago de un negocio (alta + primer mes).
/// En efectivo lo registra quien lo recibió (el vendedor) y queda confirmado al instante; una transferencia
/// (o tarjeta u otro, que llegan a la cuenta del operador) nace Pendiente y la confirma el operador.</summary>
public class PagoManual
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public decimal Monto { get; private set; }
    public MetodoPagoManual Metodo { get; private set; }
    public EstadoPagoManual Estado { get; private set; }
    public DateOnly? FechaRecepcion { get; private set; }
    public string? Nota { get; private set; }

    public Guid RegistradoPorId { get; private set; }
    public string RegistradoPorNombre { get; private set; } = string.Empty;
    public string RegistradoPorRol { get; private set; } = string.Empty;
    public DateTime FechaRegistroUtc { get; private set; } = DateTime.UtcNow;

    public Guid? ConfirmadoPorId { get; private set; }
    public string? ConfirmadoPorNombre { get; private set; }
    public DateTime? FechaConfirmacionUtc { get; private set; }

    private PagoManual() { }

    public static PagoManual Registrar(
        Guid tenantId, decimal monto, MetodoPagoManual metodo, DateOnly? fechaRecepcion, string? nota,
        Guid registradoPorId, string registradoPorNombre, string registradoPorRol)
    {
        if (monto <= 0) throw new ArgumentException("El monto tiene que ser mayor a cero.", nameof(monto));

        var pago = new PagoManual
        {
            TenantId = tenantId,
            Monto = monto,
            Metodo = metodo,
            Estado = metodo == MetodoPagoManual.Efectivo ? EstadoPagoManual.Confirmado : EstadoPagoManual.Pendiente,
            FechaRecepcion = metodo == MetodoPagoManual.Efectivo ? fechaRecepcion : null,
            Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(),
            RegistradoPorId = registradoPorId,
            RegistradoPorNombre = registradoPorNombre,
            RegistradoPorRol = registradoPorRol,
        };
        if (pago.Estado == EstadoPagoManual.Confirmado)
        {
            pago.ConfirmadoPorId = registradoPorId;
            pago.ConfirmadoPorNombre = registradoPorNombre;
            pago.FechaConfirmacionUtc = pago.FechaRegistroUtc;
        }
        return pago;
    }

    public void Confirmar(
        decimal monto, MetodoPagoManual metodo, DateOnly fechaRecepcion, string? nota, Guid adminId, string adminNombre)
    {
        if (Estado == EstadoPagoManual.Confirmado)
            throw new InvalidOperationException("El pago ya está confirmado.");
        if (monto <= 0) throw new ArgumentException("El monto tiene que ser mayor a cero.", nameof(monto));

        Monto = monto;
        Metodo = metodo;
        FechaRecepcion = fechaRecepcion;
        if (!string.IsNullOrWhiteSpace(nota)) Nota = nota.Trim();
        Estado = EstadoPagoManual.Confirmado;
        ConfirmadoPorId = adminId;
        ConfirmadoPorNombre = adminNombre;
        FechaConfirmacionUtc = DateTime.UtcNow;
    }
}
