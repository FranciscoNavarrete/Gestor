namespace GestorPOS.Application.Admin;

/// <summary>Una venta confirmada de un vendedor, con la fecha de alta del negocio (UTC).</summary>
public record VentaConfirmada(Guid Id, Guid VendedorId, DateTime FechaAltaUtc);

/// <summary>Las primeras <paramref name="CantidadPrimeras"/> ventas de cada mes valen <paramref name="Primeras"/>;
/// de ahí en adelante, <paramref name="Siguientes"/>. Quien llega a <paramref name="BonoVentas"/> ventas en el
/// mes gana además un bono de <paramref name="BonoMonto"/> (una vez por mes).</summary>
public record TarifaComision(
    int CantidadPrimeras, decimal Primeras, decimal Siguientes, int BonoVentas = 10, decimal BonoMonto = 100_000m)
{
    public static readonly TarifaComision Predeterminada = new(3, 35_000m, 40_000m);
}

/// <summary>Lo que corresponde a una venta: su comisión, si cayó en la tarifa de las "primeras" y el bono
/// (distinto de cero solo en la venta que completa la cantidad que desbloquea el bono).</summary>
public record AsignacionComision(decimal Comision, bool EsPrimera, decimal Bono);

/// <summary>Reglas del manual del vendedor: la comisión depende del orden de la venta dentro del mes
/// calendario (hora de Argentina) y se reinicia cada mes, por vendedor. El orden es por fecha de alta.</summary>
public static class CalculadoraComisiones
{
    /// <summary>Argentina no usa horario de verano, así que alcanza con un desfase fijo.</summary>
    public static readonly TimeSpan OffsetArgentina = TimeSpan.FromHours(-3);

    public static DateTime AHoraArgentina(DateTime utc) => utc + OffsetArgentina;

    /// <summary>Comisión de cada venta, si cayó en la tarifa de las "primeras" y el bono.</summary>
    public static Dictionary<Guid, AsignacionComision> Asignar(
        IEnumerable<VentaConfirmada> ventas, TarifaComision tarifa)
    {
        var resultado = new Dictionary<Guid, AsignacionComision>();

        var grupos = ventas.GroupBy(v =>
        {
            var local = AHoraArgentina(v.FechaAltaUtc);
            return (v.VendedorId, local.Year, local.Month);
        });

        foreach (var grupo in grupos)
        {
            var orden = 0;
            foreach (var venta in grupo.OrderBy(v => v.FechaAltaUtc).ThenBy(v => v.Id))
            {
                var esPrimera = orden < tarifa.CantidadPrimeras;
                orden++;
                var bono = tarifa.BonoVentas > 0 && orden == tarifa.BonoVentas ? tarifa.BonoMonto : 0m;
                resultado[venta.Id] = new AsignacionComision(esPrimera ? tarifa.Primeras : tarifa.Siguientes, esPrimera, bono);
            }
        }

        return resultado;
    }
}
