using GestorPOS.Application.Caja.Dtos;

namespace GestorPOS.Application.Caja;

public interface ICajaService
{
    Task<CajaDto?> ObtenerActualAsync(CancellationToken ct = default);
    Task<CajaDto> AbrirAsync(AbrirCajaRequest request, CancellationToken ct = default);
    Task<CajaDto> CerrarAsync(CerrarCajaRequest request, CancellationToken ct = default);
    Task<CajaDto> AgregarMovimientoAsync(CrearMovimientoCajaRequest request, CancellationToken ct = default);
}
