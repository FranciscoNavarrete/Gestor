namespace GestorPOS.Domain.Enums;

/// <summary>Unidad en la que se lleva el stock y se vende un producto. Unidad es la de siempre (enteros);
/// Kilo, Litro y Metro admiten hasta 3 decimales (gramos, mililitros y milímetros exactos).</summary>
public enum UnidadMedida
{
    Unidad = 0,
    Kilo = 1,
    Litro = 2,
    Metro = 3
}

public static class UnidadMedidaExtensions
{
    public static string Simbolo(this UnidadMedida unidad) => unidad switch
    {
        UnidadMedida.Kilo => "kg",
        UnidadMedida.Litro => "L",
        UnidadMedida.Metro => "m",
        _ => "u"
    };

    public static bool AdmiteDecimales(this UnidadMedida unidad) => unidad != UnidadMedida.Unidad;
}
