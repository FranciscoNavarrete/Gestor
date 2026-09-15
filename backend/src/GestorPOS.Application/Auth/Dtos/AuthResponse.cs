namespace GestorPOS.Application.Auth.Dtos;

public record AuthResponse(
    string Token,
    DateTime ExpiraUtc,
    Guid TenantId,
    string NombreNegocio,
    string NombreUsuario,
    string Rol);
