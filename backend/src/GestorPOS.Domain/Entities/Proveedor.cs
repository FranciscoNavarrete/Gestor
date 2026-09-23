using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class Proveedor : TenantEntity
{
    public string Nombre { get; private set; } = string.Empty;
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public bool Activo { get; private set; } = true;

    private Proveedor() { }

    public static Proveedor Crear(Guid tenantId, string nombre, string? telefono, string? email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del proveedor es obligatorio.", nameof(nombre));

        return new Proveedor
        {
            TenantId = tenantId,
            Nombre = nombre.Trim(),
            Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim()
        };
    }

    public void Editar(string nombre, string? telefono, string? email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del proveedor es obligatorio.", nameof(nombre));

        Nombre = nombre.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
