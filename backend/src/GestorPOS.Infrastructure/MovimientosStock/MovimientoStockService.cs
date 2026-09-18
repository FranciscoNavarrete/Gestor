using GestorPOS.Application.Common.Dtos;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.MovimientosStock;
using GestorPOS.Application.MovimientosStock.Dtos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Common;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.MovimientosStock;

public class MovimientoStockService : IMovimientoStockService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MovimientoStockService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PaginaDto<MovimientoStockDto>> BuscarAsync(
        Guid? productoId, DateOnly? desde, DateOnly? hasta, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);

        var query = _db.MovimientosStock.AsQueryable();

        if (productoId is not null)
            query = query.Where(m => m.ProductoId == productoId);

        if (desde is not null)
        {
            var desdeUtc = ZonaHoraria.ConvertirAUtc(desde.Value, TimeOnly.MinValue);
            query = query.Where(m => m.FechaCreacion >= desdeUtc);
        }

        if (hasta is not null)
        {
            var hastaUtc = ZonaHoraria.ConvertirAUtc(hasta.Value.AddDays(1), TimeOnly.MinValue);
            query = query.Where(m => m.FechaCreacion < hastaUtc);
        }

        var totalItems = await query.CountAsync(ct);
        var totalPaginas = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)tamanoPagina);

        var items = await query
            .OrderByDescending(m => m.FechaCreacion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(m => new MovimientoStockDto(
                m.Id, m.ProductoId, m.ProductoNombre, m.Cantidad, m.StockResultante,
                m.Motivo, m.UsuarioNombre, m.FechaCreacion))
            .ToListAsync(ct);

        return new PaginaDto<MovimientoStockDto>(items, pagina, tamanoPagina, totalItems, totalPaginas);
    }

    public void Registrar(Guid productoId, string productoNombre, int cantidad, int stockResultante, string motivo)
    {
        var movimiento = MovimientoStock.Crear(
            _tenantContext.TenantId, productoId, productoNombre, cantidad, stockResultante,
            motivo, _tenantContext.UsuarioId, _tenantContext.UsuarioNombre);
        _db.MovimientosStock.Add(movimiento);
    }
}
