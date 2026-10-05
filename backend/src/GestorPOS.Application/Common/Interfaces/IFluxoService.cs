namespace GestorPOS.Application.Common.Interfaces;

/// <summary>InitPoint viene solo cuando el cobro es por link de pago; con tarjeta queda null y la
/// suscripción ya sale autorizada.</summary>
public record FluxoSuscripcionResultado(
    int ClienteId, int UsuarioId, string Email, string PasswordTemporal, string? InitPoint, int? SuscripcionId,
    string? Estado);

/// <summary>Link de pago de una suscripción de Fluxo; InitPoint viene solo mientras siga pendiente.</summary>
public record FluxoLinkPago(string Estado, string? InitPoint);

/// <summary>Estado de una suscripción, si alguna vez se confirmó (aunque después se haya cancelado) y si
/// Mercado Pago ya aprobó el primer cobro. AjusteMontoPendiente: el primer cobro se aprobó pero falta bajar
/// la suscripción al monto mensual.</summary>
public record FluxoEstadoSuscripcion(
    string Estado, bool Confirmada, bool PrimerCobroAprobado = false, bool AjusteMontoPendiente = false,
    bool CobroRechazado = false, string? MotivoRechazo = null, DateTime? ProximoReintento = null,
    decimal MontoMensual = 0, decimal MontoProximoCobro = 0, DateTime? ProximoCobro = null,
    DateTime? FechaInicio = null, DateTime? FechaCancelacion = null);

/// <summary>Un cobro de la suscripción. Estado: aprobado, rechazado, pendiente o cancelado; el motivo viene en español.</summary>
public record FluxoCobro(
    DateTime? Fecha, decimal Monto, string Estado, string? Motivo, int Intento, DateTime? ProximoReintento, bool EsPrimerCobro);

/// <summary>Historial de cobros y próximo cobro (si la suscripción sigue autorizada).</summary>
public record FluxoCobros(
    string Estado, decimal MontoMensual, DateTime? ProximoCobro, decimal? ProximoMonto, IReadOnlyList<FluxoCobro> Cobros);

public record FluxoPlan(
    int MpPlanId, string Nombre, decimal Monto, string Moneda, string TipoFrecuencia, int Frecuencia, int DiasGratis,
    decimal? MontoPrimerCobro = null);

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

    /// <summary>Tira <see cref="GestorPOS.Application.Common.Exceptions.AppException"/> si Fluxo no
    /// responde o no encuentra la suscripción, con un mensaje apto para mostrar.</summary>
    Task<FluxoLinkPago> ObtenerLinkPagoAsync(int suscripcionId, CancellationToken ct = default);

    /// <summary>Tira <see cref="GestorPOS.Application.Common.Exceptions.AppException"/> si Fluxo no
    /// responde o no puede consultar los cobros en Mercado Pago.</summary>
    Task<FluxoCobros> ObtenerCobrosAsync(int suscripcionId, CancellationToken ct = default);

    Task<IReadOnlyList<FluxoPlan>> ListarPlanesAsync(CancellationToken ct = default);

    /// <summary>Estado (pending/authorized/...) de cada suscripción pedida, por id. Si Fluxo
    /// falla o no responde, devuelve un diccionario vacío -- el llamador no debe romperse por
    /// esto, solo mostrar el negocio sin estado de suscripción.</summary>
    /// <summary>Como <see cref="ObtenerEstadosSuscripcionesAsync"/> pero con si la suscripción se
    /// confirmó alguna vez. Devuelve null si Fluxo no responde (a diferencia de un resultado vacío),
    /// para que quien arma un reporte pueda avisar en vez de mostrar números falsos.</summary>
    Task<IReadOnlyDictionary<int, FluxoEstadoSuscripcion>?> ObtenerConfirmacionesAsync(
        IEnumerable<int> suscripcionIds, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, string>> ObtenerEstadosSuscripcionesAsync(
        IEnumerable<int> suscripcionIds, CancellationToken ct = default);
}
