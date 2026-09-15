namespace GestorPOS.Application.Common.Exceptions;

/// <summary>Excepción de negocio con mensaje seguro para mostrar al usuario final.</summary>
public class AppException(string message) : Exception(message);
