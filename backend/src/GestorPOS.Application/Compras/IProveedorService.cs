using GestorPOS.Application.Compras.Dtos;

namespace GestorPOS.Application.Compras;

public interface IProveedorService
{
    Task<IReadOnlyList<ProveedorDto>> ListarAsync(CancellationToken ct = default);
    Task<ProveedorDto> CrearAsync(CrearProveedorRequest request, CancellationToken ct = default);
    Task<ProveedorDto> EditarAsync(Guid id, EditarProveedorRequest request, CancellationToken ct = default);
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
    Task<ProveedorDto> ActivarAsync(Guid id, CancellationToken ct = default);
}
