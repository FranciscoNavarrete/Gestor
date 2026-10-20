using GestorPOS.Application.Catalog.Dtos;

namespace GestorPOS.Application.Catalog;

public interface IMarcaService
{
    Task<IReadOnlyList<MarcaDto>> ListarAsync(CancellationToken ct = default);
    Task<MarcaDto> CrearAsync(CrearMarcaRequest request, CancellationToken ct = default);
    Task<MarcaDto> EditarAsync(Guid id, EditarMarcaRequest request, CancellationToken ct = default);
    Task DesactivarAsync(Guid id, CancellationToken ct = default);
    Task<MarcaDto> ActivarAsync(Guid id, CancellationToken ct = default);

    /// <summary>Borrado definitivo — solo si la marca nunca se usó en ningún producto (activo o no). Si ya se
    /// usó, rechaza con AppException sugiriendo DesactivarAsync en su lugar.</summary>
    Task EliminarAsync(Guid id, CancellationToken ct = default);
}
