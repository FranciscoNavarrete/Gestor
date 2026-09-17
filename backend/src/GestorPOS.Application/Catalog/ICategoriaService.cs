using GestorPOS.Application.Catalog.Dtos;

namespace GestorPOS.Application.Catalog;

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaDto>> ListarAsync(CancellationToken ct = default);
    Task<CategoriaDto> CrearAsync(CrearCategoriaRequest request, CancellationToken ct = default);
    Task<CategoriaDto> EditarAsync(Guid id, EditarCategoriaRequest request, CancellationToken ct = default);
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
    Task<CategoriaDto> ActivarAsync(Guid id, CancellationToken ct = default);
}
