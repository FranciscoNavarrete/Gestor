using GestorPOS.Application.FacturasProveedor.Dtos;

namespace GestorPOS.Application.FacturasProveedor;

public interface IFacturaProveedorService
{
    Task<FacturaProveedorDto> CrearAsync(CrearFacturaProveedorRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<FacturaProveedorDto>> ListarAsync(
        Guid? proveedorId, DateOnly? desde, DateOnly? hasta, bool incluirPagadas, CancellationToken ct = default);

    Task<ResumenFacturasProveedorDto> ResumenAsync(CancellationToken ct = default);

    Task<FacturaProveedorDto> RegistrarPagoAsync(Guid id, RegistrarPagoFacturaRequest request, CancellationToken ct = default);
}
