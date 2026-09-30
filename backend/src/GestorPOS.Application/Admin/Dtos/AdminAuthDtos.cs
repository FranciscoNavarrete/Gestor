namespace GestorPOS.Application.Admin.Dtos;

public record AdminLoginRequest(string Email, string Password);

public record AdminLoginResponse(string Token, DateTime ExpiraUtc, string Nombre, string Rol);
