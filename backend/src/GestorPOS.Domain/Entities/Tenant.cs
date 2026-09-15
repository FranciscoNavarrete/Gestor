using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Nombre { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;

    private readonly List<TenantFeature> _features = new();
    public IReadOnlyCollection<TenantFeature> Features => _features.AsReadOnly();

    private Tenant() { }

    public static Tenant Crear(string nombre, string slug)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del negocio es obligatorio.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("El slug del negocio es obligatorio.", nameof(slug));

        return new Tenant
        {
            Nombre = nombre.Trim(),
            Slug = slug.Trim().ToLowerInvariant()
        };
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
