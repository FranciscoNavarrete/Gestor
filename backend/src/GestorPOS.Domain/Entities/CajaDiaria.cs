using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class CajaDiaria : TenantEntity
{
    public Guid UsuarioId { get; private set; }
    public decimal MontoApertura { get; private set; }
    public decimal? MontoCierreEsperado { get; private set; }
    public decimal? MontoCierreReal { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public bool Abierta { get; private set; } = true;

    public decimal? Diferencia => MontoCierreReal is null || MontoCierreEsperado is null
        ? null
        : MontoCierreReal - MontoCierreEsperado;

    private CajaDiaria() { }

    public static CajaDiaria Abrir(Guid tenantId, Guid usuarioId, decimal montoApertura)
    {
        if (montoApertura < 0)
            throw new ArgumentException("El monto de apertura no puede ser negativo.", nameof(montoApertura));

        return new CajaDiaria
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            MontoApertura = montoApertura
        };
    }

    public void Cerrar(decimal ventasEfectivoDelPeriodo, decimal netoMovimientosManuales, decimal montoCierreReal)
    {
        if (!Abierta)
            throw new InvalidOperationException("Esta caja ya está cerrada.");
        if (montoCierreReal < 0)
            throw new ArgumentException("El monto de cierre no puede ser negativo.", nameof(montoCierreReal));

        MontoCierreEsperado = MontoApertura + ventasEfectivoDelPeriodo + netoMovimientosManuales;
        MontoCierreReal = montoCierreReal;
        FechaCierre = DateTime.UtcNow;
        Abierta = false;
    }
}
