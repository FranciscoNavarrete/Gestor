using GestorPOS.Application.Configuracion.Dtos;

namespace GestorPOS.Application.Configuracion;

public interface IMedioPagoService
{
    Task<IReadOnlyList<MedioPagoDto>> ListarAsync(CancellationToken ct = default);
    Task<MedioPagoDto> CrearAsync(CrearMedioPagoRequest request, CancellationToken ct = default);
    Task<MedioPagoDto> EditarAsync(Guid id, EditarMedioPagoRequest request, CancellationToken ct = default);
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
    Task<MedioPagoDto> ActivarAsync(Guid id, CancellationToken ct = default);

    /// <summary>Borrado definitivo — solo si el medio de pago nunca se usó en ninguna venta y no es
    /// el protegido ("Efectivo"). Si ya se usó, rechaza con AppException sugiriendo desactivarlo.</summary>
    Task EliminarAsync(Guid id, CancellationToken ct = default);

    Task<bool> EsValidoYActivoAsync(string nombre, CancellationToken ct = default);
}
