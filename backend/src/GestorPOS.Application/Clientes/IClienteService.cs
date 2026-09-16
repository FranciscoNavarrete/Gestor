using GestorPOS.Application.Clientes.Dtos;
using GestorPOS.Application.Common.Dtos;

namespace GestorPOS.Application.Clientes;

public interface IClienteService
{
    /// <summary>Paginado + búsqueda por nombre/teléfono — para la pantalla de gestión de Clientes.</summary>
    Task<PaginaDto<ClienteDto>> BuscarAsync(
        string? busqueda, int pagina, int tamanoPagina, CancellationToken ct = default);

    Task<ClienteDetalleDto> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ClienteVentaDto>> ListarVentasAsync(Guid id, CancellationToken ct = default);
    Task<ClienteDto> EditarAsync(Guid id, EditarClienteRequest request, CancellationToken ct = default);

    /// <summary>Busca un cliente por teléfono en el tenant actual, o lo crea si no existe todavía.
    /// Usado por VentaService al cobrar una venta con teléfono cargado — no llama SaveChanges por su
    /// cuenta, queda a cargo del caller persistirlo junto con el resto de los cambios de la venta.</summary>
    Task<Guid> ObtenerOCrearPorTelefonoAsync(string telefono, CancellationToken ct = default);
}
