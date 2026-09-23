using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Compras;
using GestorPOS.Application.Compras.Dtos;
using GestorPOS.Application.MovimientosStock;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Common;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Compras;

public class CompraService : ICompraService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IMovimientoStockService _movimientoStockService;

    public CompraService(AppDbContext db, ITenantContext tenantContext, IMovimientoStockService movimientoStockService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _movimientoStockService = movimientoStockService;
    }

    public async Task<CompraDto> CrearAsync(CrearCompraRequest request, CancellationToken ct = default)
    {
        if (request.Items.Count == 0)
            throw new AppException("La compra debe tener al menos un producto.");

        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == request.ProveedorId && p.Activo, ct)
            ?? throw new AppException("El proveedor no existe o está inactivo.");

        var productoIds = request.Items.Select(i => i.ProductoId).ToList();
        var productos = await _db.Productos
            .Where(p => productoIds.Contains(p.Id) && p.Activo)
            .ToDictionaryAsync(p => p.Id, ct);

        var items = new List<(Producto Producto, int Cantidad, decimal CostoUnitario)>();
        foreach (var itemRequest in request.Items)
        {
            if (!productos.TryGetValue(itemRequest.ProductoId, out var producto))
                throw new AppException("Uno de los productos de la compra no existe o está inactivo.");

            items.Add((producto, itemRequest.Cantidad, itemRequest.CostoUnitario));
        }

        var compra = Compra.Crear(_tenantContext.TenantId, _tenantContext.UsuarioId, proveedor, items);

        foreach (var (producto, cantidad, costoUnitario) in items)
        {
            producto.AjustarStock(cantidad);
            producto.ActualizarCosto(costoUnitario);
            _movimientoStockService.Registrar(
                producto.Id, producto.Nombre, cantidad, producto.StockActual, $"Compra a {proveedor.Nombre}");
        }

        _db.Compras.Add(compra);
        await _db.SaveChangesAsync(ct);

        return ToDto(compra);
    }

    public async Task<CompraDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var compra = await _db.Compras
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("La compra no existe.");

        return ToDto(compra);
    }

    public async Task<IReadOnlyList<CompraResumenDto>> ListarAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        var query = AplicarFiltroFecha(_db.Compras.Include(c => c.Items).AsQueryable(), desde, hasta);

        return await query
            .OrderByDescending(c => c.FechaCreacion)
            .Select(c => new CompraResumenDto(c.Id, c.FechaCreacion, c.ProveedorNombre, c.Items.Count, c.Total))
            .ToListAsync(ct);
    }

    public async Task<ResumenComprasDto> ResumenAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        var query = AplicarFiltroFecha(_db.Compras.AsQueryable(), desde, hasta);

        var compras = await query.Select(c => new { c.Total, c.ProveedorId }).ToListAsync(ct);

        return new ResumenComprasDto(
            compras.Count,
            compras.Sum(c => c.Total),
            compras.Select(c => c.ProveedorId).Distinct().Count());
    }

    private static IQueryable<Compra> AplicarFiltroFecha(IQueryable<Compra> query, DateOnly? desde, DateOnly? hasta)
    {
        if (desde is not null)
        {
            var desdeUtc = ZonaHoraria.ConvertirAUtc(desde.Value, TimeOnly.MinValue);
            query = query.Where(c => c.FechaCreacion >= desdeUtc);
        }

        if (hasta is not null)
        {
            var hastaUtc = ZonaHoraria.ConvertirAUtc(hasta.Value.AddDays(1), TimeOnly.MinValue);
            query = query.Where(c => c.FechaCreacion < hastaUtc);
        }

        return query;
    }

    private static CompraDto ToDto(Compra compra)
    {
        var items = compra.Items
            .Select(i => new CompraItemDto(i.ProductoId, i.ProductoNombre, i.Cantidad, i.CostoUnitario, i.Subtotal))
            .ToList();

        return new CompraDto(compra.Id, compra.FechaCreacion, compra.ProveedorId, compra.ProveedorNombre, compra.Total, items);
    }
}
