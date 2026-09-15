namespace GestorPOS.Application.Common.Interfaces;

/// <summary>Resuelve el tenant actual a partir del JWT de la request en curso.</summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    bool TieneFeature(string clave);
}
