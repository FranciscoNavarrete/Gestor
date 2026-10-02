namespace GestorPOS.Application.Admin;

/// <summary>Una venta confirmada de un vendedor, con la fecha de alta del negocio (UTC).</summary>
public record VentaConfirmada(Guid Id, Guid VendedorId, DateTime FechaAltaUtc);

/// <summary>Las primeras <paramref name="CantidadPrimeras"/> ventas de cada mes valen <paramref name="Primeras"/>;
/// de ahí en adelante, <paramref name="Siguientes"/>.</summary>
public record TarifaComision(int CantidadPrimeras, decimal Primeras, decimal Siguientes)
{
    public static readonly TarifaComision Predeterminada = new(3, 35_000m, 40_000m);
}

/// <summary>Reglas del manual del vendedor: la comisión depende del orden de la venta dentro del mes
/// calendario (hora de Argentina) y se reinicia cada mes, por vendedor. El orden es por fecha de alta.</summary>
public static class CalculadoraComisiones
{
    /// <summary>Argentina no usa horario de verano, así que alcanza con un desfase fijo.</summary>
    public static readonly TimeSpan OffsetArgentina = TimeSpan.FromHours(-3);

    public static DateTime AHoraArgentina(DateTime utc) => utc + OffsetArgentina;

    /// <summary>Comisión de cada venta y si cayó en la tarifa de las "primeras".</summary>
    public static Dictionary<Guid, (decimal Comision, bool EsPrimera)> Asignar(
        IEnumerable<VentaConfirmada> ventas, TarifaComision tarifa)
    {
        var resultado = new Dictionary<Guid, (decimal, bool)>();

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
                resultado[venta.Id] = (esPrimera ? tarifa.Primeras : tarifa.Siguientes, esPrimera);
                orden++;
            }
        }

        return resultado;
    }
}
