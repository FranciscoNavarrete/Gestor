using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Marca de un producto (Royal Canin, Coca-Cola...). Catálogo propio de cada negocio, igual que Categoria:
/// opcional en el producto, se puede desactivar y solo se borra del todo si nunca se usó.</summary>
public class Marca : TenantEntity
{
    public string Nombre { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;

    private Marca() { }

    public static Marca Crear(Guid tenantId, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la marca es obligatorio.");

        return new Marca
        {
            TenantId = tenantId,
            Nombre = nombre.Trim()
        };
    }

    public void Renombrar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la marca es obligatorio.");
        Nombre = nombre.Trim();
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
