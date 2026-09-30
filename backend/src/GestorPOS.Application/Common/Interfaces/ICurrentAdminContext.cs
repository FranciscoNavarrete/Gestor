namespace GestorPOS.Application.Common.Interfaces;

/// <summary>Resuelve el admin (operador de la plataforma) actual a partir del JWT admin de la
/// request en curso — separado de ITenantContext, que es para el JWT de un negocio/tenant.</summary>
public interface ICurrentAdminContext
{
    Guid AdminId { get; }
    string Rol { get; }
    bool EsOperador { get; }
}
