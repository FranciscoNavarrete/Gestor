using GestorPOS.Application.Ventas.Dtos;

namespace GestorPOS.Application.Ventas;

public interface IVentaService
{
    Task<VentaDto> CrearAsync(CrearVentaRequest request, CancellationToken ct = default);
    Task<VentaDto> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<VentaResumenDto>> ListarAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);
}
