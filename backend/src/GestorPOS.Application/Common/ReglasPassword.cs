using GestorPOS.Application.Common.Exceptions;

namespace GestorPOS.Application.Common;

public static class ReglasPassword
{
    public const int LargoMinimo = 8;

    /// <summary>Valida una contraseña nueva elegida por el usuario (no aplica a las que carga el operador al dar de alta).</summary>
    public static void ValidarNueva(string? actual, string? nueva)
    {
        if (string.IsNullOrWhiteSpace(nueva) || nueva.Length < LargoMinimo)
            throw new AppException($"La contraseña nueva tiene que tener al menos {LargoMinimo} caracteres.");
        if (nueva == actual)
            throw new AppException("La contraseña nueva tiene que ser distinta de la actual.");
    }
}
