namespace GestorPOS.Application.Auth.Dtos;

public record RegistrarNegocioRequest(
    string NombreNegocio,
    string NombreAdmin,
    string Email,
    string Password);
