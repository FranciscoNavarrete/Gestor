namespace GestorPOS.Application.Admin.Dtos;

public record TenantResumenDto(Guid Id, string Nombre, string Slug, bool Activo, DateTime FechaCreacion);

public record TenantFeatureDto(Guid Id, string Clave, bool Habilitado);
