using GestorPOS.Application.Tareas.Dtos;

namespace GestorPOS.Application.Tareas;

public interface ITareaService
{
    Task<IReadOnlyList<TareaDto>> ListarAsync(bool incluirCompletadas, CancellationToken ct = default);
    Task<TareaDto> CrearAsync(CrearTareaRequest request, CancellationToken ct = default);
    Task<TareaDto> EditarAsync(Guid id, EditarTareaRequest request, CancellationToken ct = default);
    Task<TareaDto> MarcarCompletadaAsync(Guid id, CancellationToken ct = default);
    Task<TareaDto> MarcarPendienteAsync(Guid id, CancellationToken ct = default);
    Task EliminarAsync(Guid id, CancellationToken ct = default);
}
