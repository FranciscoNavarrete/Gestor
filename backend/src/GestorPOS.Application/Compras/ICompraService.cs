using GestorPOS.Application.Compras.Dtos;

namespace GestorPOS.Application.Compras;

public interface ICompraService
{
    Task<CompraDto> CrearAsync(CrearCompraRequest request, CancellationToken ct = default);
    Task<CompraDto> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CompraResumenDto>> ListarAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);
    Task<ResumenComprasDto> ResumenAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);
}
