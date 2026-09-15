using GestorPOS.Application.Catalog.Dtos;

namespace GestorPOS.Application.Catalog;

public interface IProductoService
{
    Task<IReadOnlyList<ProductoDto>> ListarAsync(bool soloBajoStock, CancellationToken ct = default);
    Task<ProductoDto> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<ProductoDto> CrearAsync(CrearProductoRequest request, CancellationToken ct = default);
    Task<ProductoDto> EditarAsync(Guid id, EditarProductoRequest request, CancellationToken ct = default);
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
    Task<int> ActualizarPreciosMasivoAsync(ActualizarPreciosMasivoRequest request, CancellationToken ct = default);

    /// <summary>Suma o resta stock manualmente (reposición, merma, corrección de inventario).
    /// Las ventas descuentan stock por su cuenta; esto es para todo lo demás.</summary>
    Task<ProductoDto> AjustarStockAsync(Guid id, int cantidad, CancellationToken ct = default);
}
