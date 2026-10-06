namespace GestorPOS.Domain.Entities;

public enum EstadoLiquidacion { Pendiente, Pagada }

/// <summary>Cierre de las comisiones de un vendedor para un mes: las ventas que incluye quedan congeladas
/// con su importe. Se paga después (Pendiente → Pagada). Un mes puede tener más de una liquidación de un
/// mismo vendedor (complementarias, por ventas que se cobraron tarde), pero una venta se liquida una sola vez.</summary>
public class Liquidacion
{
    private readonly List<LiquidacionItem> _items = [];

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid VendedorId { get; private set; }
    public string VendedorNombre { get; private set; } = string.Empty;
    public int Anio { get; private set; }
    public int Mes { get; private set; }
    public EstadoLiquidacion Estado { get; private set; } = EstadoLiquidacion.Pendiente;
    public DateTime FechaCierreUtc { get; private set; } = DateTime.UtcNow;
    public Guid CerradaPorId { get; private set; }
    public string CerradaPorNombre { get; private set; } = string.Empty;
    public DateOnly? FechaPago { get; private set; }
    public string? PagadaPorNombre { get; private set; }
    public string? Nota { get; private set; }
    public decimal TotalComision { get; private set; }
    public decimal TotalBono { get; private set; }

    /// <summary>Efectivo que el vendedor tenía a su cargo y se descontó de esta liquidación.</summary>
    public decimal EfectivoCompensado { get; private set; }

    public IReadOnlyList<LiquidacionItem> Items => _items;
    public decimal Total => TotalComision + TotalBono;

    /// <summary>Lo que se le paga al vendedor; si es negativo, es lo que tiene que entregar.</summary>
    public decimal Neto => Total - EfectivoCompensado;

    private Liquidacion() { }

    public static Liquidacion Crear(
        Guid vendedorId, string vendedorNombre, int anio, int mes, Guid cerradaPorId, string cerradaPorNombre,
        IEnumerable<LiquidacionItem> items, decimal efectivoCompensado = 0)
    {
        var liquidacion = new Liquidacion
        {
            VendedorId = vendedorId,
            VendedorNombre = vendedorNombre,
            Anio = anio,
            Mes = mes,
            CerradaPorId = cerradaPorId,
            CerradaPorNombre = cerradaPorNombre,
        };
        liquidacion.EfectivoCompensado = efectivoCompensado;
        liquidacion._items.AddRange(items);
        liquidacion.TotalComision = liquidacion._items.Sum(i => i.Comision);
        liquidacion.TotalBono = liquidacion._items.Sum(i => i.Bono);
        return liquidacion;
    }

    public void MarcarPagada(DateOnly fechaPago, string? nota, string pagadaPorNombre)
    {
        if (Estado == EstadoLiquidacion.Pagada)
            throw new InvalidOperationException("La liquidación ya está pagada.");
        Estado = EstadoLiquidacion.Pagada;
        FechaPago = fechaPago;
        Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim();
        PagadaPorNombre = pagadaPorNombre;
    }
}

/// <summary>Una venta liquidada: el negocio, su orden dentro del mes del vendedor y lo que se pagó por ella.</summary>
public class LiquidacionItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LiquidacionId { get; private set; }
    public Guid TenantId { get; private set; }
    public string TenantNombre { get; private set; } = string.Empty;
    public DateTime FechaAltaUtc { get; private set; }
    public int Orden { get; private set; }
    public decimal Comision { get; private set; }
    public decimal Bono { get; private set; }

    private LiquidacionItem() { }

    public static LiquidacionItem Crear(Guid tenantId, string tenantNombre, DateTime fechaAltaUtc, int orden, decimal comision, decimal bono) =>
        new()
        {
            TenantId = tenantId,
            TenantNombre = tenantNombre,
            FechaAltaUtc = fechaAltaUtc,
            Orden = orden,
            Comision = comision,
            Bono = bono,
        };
}
