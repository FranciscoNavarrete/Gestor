using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Flag de una funcionalidad opcional habilitada para un tenant puntual (pedidos custom de clientes).</summary>
public class TenantFeature : TenantEntity
{
    public string Clave { get; private set; } = string.Empty;
    public bool Habilitado { get; private set; }

    private TenantFeature() { }

    public static TenantFeature Crear(Guid tenantId, string clave, bool habilitado = true)
    {
        if (string.IsNullOrWhiteSpace(clave))
            throw new ArgumentException("La clave del feature es obligatoria.", nameof(clave));

        return new TenantFeature
        {
            TenantId = tenantId,
            Clave = clave.Trim(),
            Habilitado = habilitado
        };
    }

    public void Habilitar() => Habilitado = true;
    public void Deshabilitar() => Habilitado = false;
}
