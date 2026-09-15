using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class Categoria : TenantEntity
{
    public string Nombre { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;

    private Categoria() { }

    public static Categoria Crear(Guid tenantId, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la categoría es obligatorio.", nameof(nombre));

        return new Categoria
        {
            TenantId = tenantId,
            Nombre = nombre.Trim()
        };
    }

    public void Renombrar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la categoría es obligatorio.", nameof(nombre));
        Nombre = nombre.Trim();
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
