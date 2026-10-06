using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Suscripcion;

/// <summary>Estado de la suscripción del negocio. TarjetaEditable: se creó con tarjeta (no con link de pago),
/// así que el dueño puede cambiarla. EmailPagador es el email con el que se tokeniza la tarjeta nueva.</summary>
public record MiSuscripcionDto(
    string Estado, decimal MontoMensual, DateTime? ProximoCobro, decimal? ProximoMonto,
    bool TarjetaEditable, bool CobroRechazado, string? MotivoRechazo, DateTime? ProximoReintento,
    string EmailPagador, IReadOnlyList<CobroNegocioDto> Cobros, bool PrimerPagoManual = false);

public record CambiarTarjetaRequest(string CardToken);

public interface IMiSuscripcionService
{
    /// <summary>Tira AppException si el negocio no tiene una suscripción o Fluxo no responde.</summary>
    Task<MiSuscripcionDto> ObtenerAsync(CancellationToken ct = default);

    /// <summary>Cambia la tarjeta con la que se cobra la suscripción del negocio.</summary>
    Task CambiarTarjetaAsync(CambiarTarjetaRequest request, CancellationToken ct = default);
}
