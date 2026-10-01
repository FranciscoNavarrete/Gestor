namespace GestorPOS.Application.Common.Interfaces;

public record FluxoSuscripcionResultado(
    int ClienteId, int UsuarioId, string Email, string PasswordTemporal, string? InitPoint, int? SuscripcionId);

public record FluxoPlan(
    int MpPlanId, string Nombre, decimal Monto, string Moneda, string TipoFrecuencia, int Frecuencia, int DiasGratis);

/// <summary>Integración con Fluxo (el motor de cobros/suscripciones que factura a los negocios
/// que usan GestorPOS). Si Fluxo no responde o falla, el negocio se crea igual en GestorPOS —
/// esto nunca debe frenar el alta, solo queda sin suscripción para resolver después a mano.</summary>
public interface IFluxoService
{
    Task<FluxoSuscripcionResultado?> IniciarSuscripcionAsync(
        string nombre, string apellido, string email, int? mpPlanId, CancellationToken ct = default);

    Task<IReadOnlyList<FluxoPlan>> ListarPlanesAsync(CancellationToken ct = default);

    /// <summary>Estado (pending/authorized/...) de cada suscripción pedida, por id. Si Fluxo
    /// falla o no responde, devuelve un diccionario vacío -- el llamador no debe romperse por
    /// esto, solo mostrar el negocio sin estado de suscripción.</summary>
    Task<IReadOnlyDictionary<int, string>> ObtenerEstadosSuscripcionesAsync(
        IEnumerable<int> suscripcionIds, CancellationToken ct = default);
}
