using GestorPOS.Application.Catalog.Dtos;
using GestorPOS.Application.Common.Dtos;

namespace GestorPOS.Application.Catalog;

public interface IProductoService
{
    /// <summary>Trae todos los productos activos sin paginar — para el buscador de Venta,
    /// que necesita todo el catálogo cargado en memoria para filtrar al instante mientras se cobra.</summary>
    Task<IReadOnlyList<ProductoDto>> ListarAsync(bool soloBajoStock, CancellationToken ct = default);

    /// <summary>Paginado + búsqueda por nombre/SKU/categoría — para la pantalla de gestión de
    /// Productos, pensado para catálogos grandes (ej. migrados por Excel).</summary>
    Task<PaginaDto<ProductoDto>> BuscarAsync(
        string? busqueda, bool soloBajoStock, int pagina, int tamanoPagina, CancellationToken ct = default);
    Task<ProductoDto> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<ProductoDto> CrearAsync(CrearProductoRequest request, CancellationToken ct = default);
    Task<ProductoDto> EditarAsync(Guid id, EditarProductoRequest request, CancellationToken ct = default);
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
    Task<ProductoDto> ActivarAsync(Guid id, CancellationToken ct = default);
    Task<int> ActualizarPreciosMasivoAsync(ActualizarPreciosMasivoRequest request, CancellationToken ct = default);

    /// <summary>Suma o resta stock manualmente (reposición, merma, corrección de inventario).
    /// Las ventas descuentan stock por su cuenta; esto es para todo lo demás.</summary>
    Task<ProductoDto> AjustarStockAsync(Guid id, int cantidad, string motivo, CancellationToken ct = default);
}
