using GestorPOS.Domain.Enums;

namespace GestorPOS.Domain.Common;

/// <summary>Reglas de cantidades y redondeos compartidas por producto, venta y compra.</summary>
public static class ReglasCantidad
{
    public const int DecimalesMedida = 3;

    /// <summary>Un producto por Unidad solo admite enteros; uno medido, hasta 3 decimales.</summary>
    public static void Validar(UnidadMedida unidad, decimal cantidad, string sujeto = "La cantidad")
    {
        if (unidad == UnidadMedida.Unidad)
        {
            if (cantidad != decimal.Truncate(cantidad))
                throw new ArgumentException($"{sujeto} de un producto por unidad tiene que ser un número entero.");
        }
        else if (cantidad != Math.Round(cantidad, DecimalesMedida))
        {
            throw new ArgumentException($"{sujeto} admite hasta {DecimalesMedida} decimales.");
        }
    }

    /// <summary>Cantidad para mostrar en mensajes y tickets: sin ceros de más (3 → "3", 0.350 → "0.35").</summary>
    public static string Texto(decimal cantidad) =>
        cantidad.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Redondeo de importes en pesos (2 decimales, mitades hacia arriba).</summary>
    public static decimal RedondearImporte(decimal importe) =>
        Math.Round(importe, 2, MidpointRounding.AwayFromZero);
}
