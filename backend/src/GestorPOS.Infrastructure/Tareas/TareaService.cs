using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Tareas;
using GestorPOS.Application.Tareas.Dtos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Common;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Tareas;

public class TareaService : ITareaService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TareaService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<TareaDto>> ListarAsync(bool incluirCompletadas, CancellationToken ct = default)
    {
        var query = _db.Tareas.AsQueryable();
        if (!incluirCompletadas)
            query = query.Where(t => !t.Completada);

        return await query
            .OrderBy(t => t.FechaHora)
            .Select(t => new TareaDto(t.Id, t.Titulo, t.Notas, t.FechaHora, t.MinutosAntesAviso, t.Completada))
            .ToListAsync(ct);
    }

    public async Task<TareaDto> CrearAsync(CrearTareaRequest request, CancellationToken ct = default)
    {
        var fechaHoraUtc = ZonaHoraria.ConvertirAUtc(request.Fecha, request.Hora);
        var tarea = Tarea.Crear(
            _tenantContext.TenantId, _tenantContext.UsuarioId, request.Titulo, request.Notas,
            fechaHoraUtc, request.MinutosAntesAviso);

        _db.Tareas.Add(tarea);
        await _db.SaveChangesAsync(ct);
        return ToDto(tarea);
    }

    public async Task<TareaDto> EditarAsync(Guid id, EditarTareaRequest request, CancellationToken ct = default)
    {
        var tarea = await _db.Tareas.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException("La tarea no existe.");

        var fechaHoraUtc = ZonaHoraria.ConvertirAUtc(request.Fecha, request.Hora);
        tarea.Editar(request.Titulo, request.Notas, fechaHoraUtc, request.MinutosAntesAviso);
        await _db.SaveChangesAsync(ct);
        return ToDto(tarea);
    }

    public async Task<TareaDto> MarcarCompletadaAsync(Guid id, CancellationToken ct = default)
    {
        var tarea = await _db.Tareas.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException("La tarea no existe.");

        tarea.MarcarCompletada();
        await _db.SaveChangesAsync(ct);
        return ToDto(tarea);
    }

    public async Task<TareaDto> MarcarPendienteAsync(Guid id, CancellationToken ct = default)
    {
        var tarea = await _db.Tareas.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException("La tarea no existe.");

        tarea.MarcarPendiente();
        await _db.SaveChangesAsync(ct);
        return ToDto(tarea);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var tarea = await _db.Tareas.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException("La tarea no existe.");

        _db.Tareas.Remove(tarea);
        await _db.SaveChangesAsync(ct);
    }

    private static TareaDto ToDto(Tarea tarea) =>
        new(tarea.Id, tarea.Titulo, tarea.Notas, tarea.FechaHora, tarea.MinutosAntesAviso, tarea.Completada);
}
