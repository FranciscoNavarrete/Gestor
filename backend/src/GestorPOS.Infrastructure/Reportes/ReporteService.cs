using GestorPOS.Application.Reportes;
using GestorPOS.Application.Reportes.Dtos;
using GestorPOS.Domain.Common;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Common;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Reportes;

public class ReporteService : IReporteService
{
    private readonly AppDbContext _db;

    public ReporteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RankingProductoDto>> RankingProductosAsync(
        DateOnly? desde, DateOnly? hasta, int top, CancellationToken ct = default)
    {
        // El GroupBy con múltiples Sum() no traduce a SQL cuando viene de un SelectMany sobre
        // una navegación filtrada por tenant; se trae la lista plana (volumen bajo en un POS) y se agrupa en memoria.
        var items = await VentasEnRango(desde, hasta)
            .SelectMany(v => v.Items)
            .Select(i => new { i.ProductoId, i.ProductoNombre, i.Cantidad, i.Subtotal })
            .ToListAsync(ct);

        return items
            .GroupBy(i => new { i.ProductoId, i.ProductoNombre })
            .Select(g => new RankingProductoDto(
                g.Key.ProductoId, g.Key.ProductoNombre, g.Sum(i => i.Cantidad), g.Sum(i => i.Subtotal)))
            .OrderByDescending(r => r.CantidadVendida)
            .Take(top)
            .ToList();
    }

    public async Task<IReadOnlyList<RankingClienteDto>> RankingClientesAsync(
        DateOnly? desde, DateOnly? hasta, int top, CancellationToken ct = default)
    {
        var ventas = await VentasEnRango(desde, hasta)
            .Where(v => v.ClienteId != null)
            .Select(v => new { ClienteId = v.ClienteId!.Value, v.Total })
            .ToListAsync(ct);

        if (ventas.Count == 0) return [];

        var clienteIds = ventas.Select(v => v.ClienteId).Distinct().ToList();
        var clientes = await _db.Clientes
            .Where(c => clienteIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        return ventas
            .GroupBy(v => v.ClienteId)
            .Select(g =>
            {
                clientes.TryGetValue(g.Key, out var cliente);
                return new RankingClienteDto(
                    g.Key, cliente?.Nombre, cliente?.Telefono ?? "", g.Count(), g.Sum(v => v.Total));
            })
            .OrderByDescending(r => r.TotalGastado)
            .Take(top)
            .ToList();
    }

    public async Task<GananciasDto> GananciasAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        var ventas = VentasEnRango(desde, hasta);
        var cantidadVentas = await ventas.CountAsync(ct);

        var items = ventas.SelectMany(v => v.Items);
        var totalVentas = await items.SumAsync(i => (decimal?)i.Subtotal, ct) ?? 0m;
        var totalCosto = await items.SumAsync(i => (decimal?)(i.CostoUnitario * i.Cantidad), ct) ?? 0m;

        return new GananciasDto(cantidadVentas, totalVentas, totalCosto, totalVentas - totalCosto);
    }

    public async Task<DashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var hoy = ZonaHoraria.HoyEnArgentina();
        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);

        var gananciasHoy = await GananciasAsync(hoy, hoy, ct);
        var ventasMes = await GananciasAsync(inicioMes, hoy, ct);
        var rankingHoy = await RankingProductosAsync(hoy, hoy, top: 1, ct);

        var productosBajoStock = await _db.Productos
            .CountAsync(p => p.Activo && p.StockActual <= p.StockMinimo, ct);

        return new DashboardDto(
            VentasHoy: gananciasHoy.TotalVentas,
            CantidadVentasHoy: gananciasHoy.CantidadVentas,
            GananciaHoy: gananciasHoy.GananciaNeta,
            VentasMes: ventasMes.TotalVentas,
            ProductosBajoStockMinimo: productosBajoStock,
            ProductoMasVendidoHoy: rankingHoy.FirstOrDefault());
    }

    public async Task<DeudasDto> DeudasAsync(CancellationToken ct = default)
    {
        var deudas = await _db.Clientes
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                c.Telefono,
                CantidadVentasACuenta = _db.Ventas.Count(v => v.ClienteId == c.Id && v.MedioPago == CuentaCorriente.MedioPago),
                Saldo = (_db.Ventas.Where(v => v.ClienteId == c.Id && v.MedioPago == CuentaCorriente.MedioPago).Sum(v => (decimal?)v.Total) ?? 0m)
                    - (_db.PagosCuenta.Where(p => p.ClienteId == c.Id).Sum(p => (decimal?)p.Monto) ?? 0m),
            })
            .Where(x => x.Saldo > 0)
            .OrderByDescending(x => x.Saldo)
            .ToListAsync(ct);

        var clientes = deudas
            .Select(d => new DeudaClienteDto(d.Id, d.Nombre, d.Telefono, d.CantidadVentasACuenta, d.Saldo))
            .ToList();

        return new DeudasDto(clientes.Sum(c => c.Saldo), clientes.Count, clientes);
    }

    private IQueryable<Venta> VentasEnRango(DateOnly? desde, DateOnly? hasta)
    {
        var query = _db.Ventas.AsQueryable();

        if (desde is not null)
        {
            var desdeUtc = ZonaHoraria.ConvertirAUtc(desde.Value, TimeOnly.MinValue);
            query = query.Where(v => v.FechaCreacion >= desdeUtc);
        }

        if (hasta is not null)
        {
            var hastaUtc = ZonaHoraria.ConvertirAUtc(hasta.Value.AddDays(1), TimeOnly.MinValue);
            query = query.Where(v => v.FechaCreacion < hastaUtc);
        }

        return query;
    }
}
