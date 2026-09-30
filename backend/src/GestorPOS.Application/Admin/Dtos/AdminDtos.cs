namespace GestorPOS.Application.Admin.Dtos;

public record TenantResumenDto(
    Guid Id, string Nombre, string Slug, bool Activo, DateTime FechaCreacion,
    Guid? VendedorId, string? VendedorNombre,
    int? FluxoClienteId = null, int? FluxoSuscripcionId = null, string? FluxoInitPoint = null);

public record TenantFeatureDto(Guid Id, string Clave, bool Habilitado);

/// <summary>Alta de un negocio hecha por el operador de GestorPOS. El email/password que se cargan
/// acá son las credenciales que se le entregan al cliente — no hay auto-registro público.
/// VendedorId es opcional: si quien crea el negocio es un Vendedor, el service lo autoasigna e
/// ignora lo que venga acá; si es Operador, puede elegir a qué vendedor atribuirlo (o ninguno).</summary>
public record CrearNegocioRequest(string NombreNegocio, string NombreAdmin, string Email, string Password, Guid? VendedorId = null);

public record AdminUsuarioDto(Guid Id, string Email, string Nombre, string Rol, bool Activo, DateTime CreadoUtc);

public record CrearAdminUsuarioRequest(string Email, string Password, string Nombre, string Rol);
