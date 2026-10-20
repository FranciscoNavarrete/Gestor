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

        var productoIds = request.Items.Select(i => i.ProductoId).Distinct().ToList();

        // Misma idea que en las ventas: el stock se lee y se suma con los productos bloqueados.
        await using var transaccion = await _db.Database.BeginTransactionAsync(ct);
        await _db.BloquearProductosAsync(_tenantContext.TenantId, productoIds, ct);

        var productos = await _db.Productos
            .Where(p => productoIds.Contains(p.Id) && p.Activo)
            .ToDictionaryAsync(p => p.Id, ct);

        var presentacionIds = request.Items
            .Where(i => i.PresentacionId is not null).Select(i => i.PresentacionId!.Value).Distinct().ToList();
        var presentaciones = presentacionIds.Count == 0
            ? new Dictionary<Guid, ProductoPresentacion>()
            : await _db.ProductoPresentaciones
                .Where(p => presentacionIds.Contains(p.Id) && p.Activo)
                .ToDictionaryAsync(p => p.Id, ct);

        var items = new List<(Producto Producto, decimal Cantidad, decimal CostoUnitario, ProductoPresentacion? Presentacion)>();
        foreach (var itemRequest in request.Items)
        {
            if (!productos.TryGetValue(itemRequest.ProductoId, out var producto))
                throw new AppException("Uno de los productos de la compra no existe o está inactivo.");

            ProductoPresentacion? presentacion = null;
            if (itemRequest.PresentacionId is { } presentacionId
                && (!presentaciones.TryGetValue(presentacionId, out presentacion) || presentacion.ProductoId != producto.Id))
                throw new AppException("Una de las presentaciones de la compra no existe, está desactivada o no es de ese producto.");

            items.Add((producto, itemRequest.Cantidad, itemRequest.CostoUnitario, presentacion));
        }

        var compra = Compra.Crear(_tenantContext.TenantId, _tenantContext.UsuarioId, proveedor, items);

        // El stock suma en la unidad del producto y el costo vigente pasa a ser el de esa unidad (por kilo, por
        // litro...), aunque se haya comprado por presentación: "2 bolsas de 15 kg a $42.000" suma 30 kg y deja
        // el costo en $2.800 por kg.
        foreach (var item in compra.Items)
        {
            var producto = productos[item.ProductoId];
            producto.AjustarStock(item.CantidadBase);
            producto.ActualizarCosto(item.CostoPorUnidadBase);
            _movimientoStockService.Registrar(
                producto.Id, producto.Nombre, item.CantidadBase, producto.StockActual, $"Compra a {proveedor.Nombre}");
        }

        _db.Compras.Add(compra);
        await _db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

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
            .Select(i => new CompraItemDto(
                i.ProductoId, i.ProductoNombre, i.Cantidad, i.CostoUnitario, i.Subtotal,
                i.PresentacionNombre, i.Unidad.ToString()))
            .ToList();

        return new CompraDto(compra.Id, compra.FechaCreacion, compra.ProveedorId, compra.ProveedorNombre, compra.Total, items);
    }
}
