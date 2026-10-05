using System.Globalization;
using GestorPOS.Application.Admin;
using Microsoft.Extensions.Configuration;

namespace GestorPOS.Infrastructure.Admin;

/// <summary>Lee la tarifa de comisiones y el bono de la configuración del servidor (Comisiones:*).</summary>
public static class TarifaComisionConfiguracion
{
    public static TarifaComision Leer(IConfiguration config)
    {
        var predeterminada = TarifaComision.Predeterminada;
        return new TarifaComision(
            int.TryParse(config["Comisiones:CantidadPrimeras"], out var cantidad) ? cantidad : predeterminada.CantidadPrimeras,
            decimal.TryParse(config["Comisiones:TarifaPrimeras"], NumberStyles.Number, CultureInfo.InvariantCulture, out var primeras) ? primeras : predeterminada.Primeras,
            decimal.TryParse(config["Comisiones:TarifaSiguientes"], NumberStyles.Number, CultureInfo.InvariantCulture, out var siguientes) ? siguientes : predeterminada.Siguientes,
            int.TryParse(config["Comisiones:BonoVentas"], out var bonoVentas) ? bonoVentas : predeterminada.BonoVentas,
            decimal.TryParse(config["Comisiones:BonoMonto"], NumberStyles.Number, CultureInfo.InvariantCulture, out var bonoMonto) ? bonoMonto : predeterminada.BonoMonto);
    }
}
