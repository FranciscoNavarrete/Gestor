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

    /// <summary>Busca un cliente por teléfono en el tenant actual, o lo crea si no existe todavía —
    /// en ese caso, con el nombre dado (si vino alguno; se ignora si el cliente ya existía, para no
    /// pisar un nombre ya cargado con un typo del cajero al cobrar). Usado por VentaService al cobrar
    /// una venta con teléfono cargado — no llama SaveChanges por su cuenta, queda a cargo del caller
    /// persistirlo junto con el resto de los cambios de la venta.</summary>
    Task<Guid> ObtenerOCrearPorTelefonoAsync(string telefono, string? nombre, CancellationToken ct = default);

    Task<IReadOnlyList<MovimientoCuentaDto>> ListarMovimientosCuentaAsync(Guid id, CancellationToken ct = default);

    /// <summary>Registra un pago total o parcial contra la deuda a cuenta del cliente. Rechaza si el
    /// monto supera lo que efectivamente debe.</summary>
    Task<ClienteDetalleDto> RegistrarPagoCuentaAsync(
        Guid id, RegistrarPagoCuentaRequest request, CancellationToken ct = default);
}
