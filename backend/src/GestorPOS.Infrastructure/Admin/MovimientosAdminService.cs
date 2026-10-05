using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Admin;

public class MovimientosAdminService : IMovimientosAdminService
{
    private const int MaximoDiasDeRango = 366;
    private const int MaximoResultados = 500;

    private readonly AppDbContext _db;

    public MovimientosAdminService(AppDbContext db) => _db = db;

    public async Task<MovimientosAdminDto> ListarAsync(
        DateOnly desde, DateOnly hasta, Guid? adminId, string? accion, string? texto, CancellationToken ct = default)
    {
        if (desde == default || hasta == default)
            throw new AppException("Elegí las fechas.");

        var hoy = DateOnly.FromDateTime(CalculadoraComisiones.AHoraArgentina(DateTime.UtcNow));
        if (hasta > hoy)
            throw new AppException("La fecha hasta no puede ser mayor a hoy.");
        if (desde > hasta)
            throw new AppException("La fecha desde no puede ser mayor que la fecha hasta.");
        if (hasta.DayNumber - desde.DayNumber > MaximoDiasDeRango)
            throw new AppException("El rango no puede superar un año.");

        var desdeUtc = InicioDelDiaEnUtc(desde);
        var hastaUtcExclusivo = InicioDelDiaEnUtc(hasta.AddDays(1));

        var consulta = _db.MovimientosAdmin.AsNoTracking()
            .Where(m => m.FechaUtc >= desdeUtc && m.FechaUtc < hastaUtcExclusivo);

        if (adminId is not null)
            consulta = consulta.Where(m => m.AdminId == adminId);
        if (!string.IsNullOrWhiteSpace(accion))
        {
            var prefijo = accion.Trim();
            // "negocio" o "feature" o "usuario" filtra por tipo; "negocio.alta" por acción exacta.
            consulta = prefijo.Contains('.')
                ? consulta.Where(m => m.Accion == prefijo)
                : consulta.Where(m => m.Accion.StartsWith(prefijo + "."));
        }
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var patron = $"%{texto.Trim()}%";
            consulta = consulta.Where(m =>
                EF.Functions.ILike(m.EntidadNombre, patron) || EF.Functions.ILike(m.AdminNombre, patron));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(m => m.FechaUtc)
            .Take(MaximoResultados)
            .Select(m => new MovimientoAdminDto(
                m.Id, m.FechaUtc, m.AdminId, m.AdminNombre, m.AdminRol, m.Accion, m.Entidad, m.EntidadId, m.EntidadNombre, m.Detalle))
            .ToListAsync(ct);

        return new MovimientosAdminDto(desde, hasta, total, items);
    }

    // Medianoche de un día de Argentina, expresada en UTC (Argentina es UTC-3).
    private static DateTime InicioDelDiaEnUtc(DateOnly dia) =>
        DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue) - CalculadoraComisiones.OffsetArgentina, DateTimeKind.Utc);
}
