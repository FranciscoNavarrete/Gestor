namespace GestorPOS.Domain.Common;

/// <summary>Función opcional "Medidas y presentaciones": vender por kilo, litro o metro y por presentaciones
/// (Bolsa 15 kg, Caja x12). La prende el operador por negocio. Con ella apagada no se pueden crear productos
/// medidos ni presentaciones nuevas, pero lo que ya existe sigue funcionando.</summary>
public static class MedidasFeature
{
    public const string Clave = "medidas";
}
