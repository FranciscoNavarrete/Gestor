namespace GestorPOS.Application.Common.Interfaces;

/// <summary>InitPoint viene solo cuando el cobro es por link de pago; con tarjeta queda null y la
/// suscripción ya sale autorizada.</summary>
public record FluxoSuscripcionResultado(
    int ClienteId, int UsuarioId, string Email, string PasswordTemporal, string? InitPoint, int? SuscripcionId,
    string? Estado);

public record FluxoPlan(
    int MpPlanId, string Nombre, decimal Monto, string Moneda, string TipoFrecuencia, int Frecuencia, int DiasGratis);

/// <summary>Integración con Fluxo (el motor de cobros/suscripciones que factura a los negocios
/// que usan GestorPOS).</summary>
public interface IFluxoService
{
    /// <summary>Tira <see cref="GestorPOS.Application.Common.Exceptions.AppException"/> si Fluxo
    /// no devuelve un link de pago válido (caído, email sin cuenta real en Mercado Pago, etc.) —
    /// un negocio sin forma de cobrarle no tiene que llegar a crearse en GestorPOS. Con
    /// <paramref name="cardTokenId"/> (token de tarjeta generado en el navegador) no hay link: la
    /// suscripción se crea autorizada, y se tira AppException si Mercado Pago rechaza la tarjeta.</summary>
    Task<FluxoSuscripcionResultado> IniciarSuscripcionAsync(
        string nombre, string apellido, string email, int? mpPlanId, string? cardTokenId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<FluxoPlan>> ListarPlanesAsync(CancellationToken ct = default);

    /// <summary>Estado (pending/authorized/...) de cada suscripción pedida, por id. Si Fluxo
    /// falla o no responde, devuelve un diccionario vacío -- el llamador no debe romperse por
    /// esto, solo mostrar el negocio sin estado de suscripción.</summary>
    Task<IReadOnlyDictionary<int, string>> ObtenerEstadosSuscripcionesAsync(
        IEnumerable<int> suscripcionIds, CancellationToken ct = default);
}
