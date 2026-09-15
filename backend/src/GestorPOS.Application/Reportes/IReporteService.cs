using GestorPOS.Application.Reportes.Dtos;

namespace GestorPOS.Application.Reportes;

public interface IReporteService
{
    Task<IReadOnlyList<RankingProductoDto>> RankingProductosAsync(DateOnly? desde, DateOnly? hasta, int top, CancellationToken ct = default);
    Task<GananciasDto> GananciasAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);
    Task<DashboardDto> DashboardAsync(CancellationToken ct = default);
}
