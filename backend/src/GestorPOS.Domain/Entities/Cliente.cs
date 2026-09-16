using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class Cliente : TenantEntity
{
    public string Telefono { get; private set; } = string.Empty;
    public string? Nombre { get; private set; }

    private Cliente() { }

    public static Cliente Crear(Guid tenantId, string telefono, string? nombre = null)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            throw new ArgumentException("El teléfono del cliente es obligatorio.", nameof(telefono));

        return new Cliente
        {
            TenantId = tenantId,
            Telefono = telefono.Trim(),
            Nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim()
        };
    }

    public void ActualizarNombre(string? nombre) =>
        Nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();

    public void ActualizarTelefono(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            throw new ArgumentException("El teléfono del cliente es obligatorio.", nameof(telefono));
        Telefono = telefono.Trim();
    }
}
