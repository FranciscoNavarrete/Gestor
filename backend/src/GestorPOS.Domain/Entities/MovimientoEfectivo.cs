namespace GestorPOS.Domain.Entities;

/// <summary>Cómo se saldó parte del efectivo que un vendedor tiene a su cargo: Entrega (le dio el dinero al
/// operador) o Compensacion (se descontó de su liquidación). Lo que cobró en efectivo sale de los
/// <see cref="PagoManual"/> en efectivo que registró el propio vendedor.</summary>
public enum TipoMovimientoEfectivo { Entrega, Compensacion }

public class MovimientoEfectivo
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid VendedorId { get; private set; }
    public TipoMovimientoEfectivo Tipo { get; private set; }
    public decimal Monto { get; private set; }
    public DateOnly Fecha { get; private set; }
    public string? Nota { get; private set; }

    /// <summary>Solo en las compensaciones: la liquidación que descontó este efectivo.</summary>
    public Guid? LiquidacionId { get; private set; }

    public Guid RegistradoPorId { get; private set; }
    public string RegistradoPorNombre { get; private set; } = string.Empty;
    public DateTime FechaRegistroUtc { get; private set; } = DateTime.UtcNow;

    private MovimientoEfectivo() { }

    public static MovimientoEfectivo Entrega(
        Guid vendedorId, decimal monto, DateOnly fecha, string? nota, Guid registradoPorId, string registradoPorNombre)
    {
        if (monto <= 0) throw new ArgumentException("El monto tiene que ser mayor a cero.", nameof(monto));
        return new MovimientoEfectivo
        {
            VendedorId = vendedorId,
            Tipo = TipoMovimientoEfectivo.Entrega,
            Monto = monto,
            Fecha = fecha,
            Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(),
            RegistradoPorId = registradoPorId,
            RegistradoPorNombre = registradoPorNombre,
        };
    }

    public static MovimientoEfectivo Compensacion(
        Guid vendedorId, decimal monto, DateOnly fecha, Guid liquidacionId, Guid registradoPorId, string registradoPorNombre) =>
        new()
        {
            VendedorId = vendedorId,
            Tipo = TipoMovimientoEfectivo.Compensacion,
            Monto = monto,
            Fecha = fecha,
            LiquidacionId = liquidacionId,
            RegistradoPorId = registradoPorId,
            RegistradoPorNombre = registradoPorNombre,
        };
}
