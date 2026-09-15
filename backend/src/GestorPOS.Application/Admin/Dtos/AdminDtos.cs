namespace GestorPOS.Application.Admin.Dtos;

public record TenantResumenDto(Guid Id, string Nombre, string Slug, bool Activo, DateTime FechaCreacion);

public record TenantFeatureDto(Guid Id, string Clave, bool Habilitado);

/// <summary>Alta de un negocio hecha por el operador de GestorPOS. El email/password que se cargan
/// acá son las credenciales que se le entregan al cliente — no hay auto-registro público.</summary>
public record CrearNegocioRequest(string NombreNegocio, string NombreAdmin, string Email, string Password);
