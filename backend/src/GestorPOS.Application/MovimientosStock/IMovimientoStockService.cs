using GestorPOS.Application.Common.Dtos;
using GestorPOS.Application.MovimientosStock.Dtos;

namespace GestorPOS.Application.MovimientosStock;

public interface IMovimientoStockService
{
    Task<PaginaDto<MovimientoStockDto>> BuscarAsync(
        Guid? productoId, DateOnly? desde, DateOnly? hasta, int pagina, int tamanoPagina, CancellationToken ct = default);

    /// <summary>Registra un movimiento de stock (venta o ajuste manual). No llama SaveChanges por su
    /// cuenta — mismo patrón que ClienteService.ObtenerOCrearPorTelefonoAsync — queda a cargo del
    /// caller persistirlo junto con el resto de los cambios (ajuste de stock o venta) en una sola
    /// transacción.</summary>
    void Registrar(Guid productoId, string productoNombre, int cantidad, int stockResultante, string motivo);
}
