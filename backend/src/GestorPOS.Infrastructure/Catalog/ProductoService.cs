using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using GestorPOS.Application.Common.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.MovimientosStock;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Catalog;

public class ProductoService : IProductoService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IMovimientoStockService _movimientoStockService;

    public ProductoService(AppDbContext db, ITenantContext tenantContext, IMovimientoStockService movimientoStockService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _movimientoStockService = movimientoStockService;
    }

    public async Task<IReadOnlyList<ProductoDto>> ListarAsync(bool soloBajoStock, CancellationToken ct = default)
    {
        var query = _db.Productos.Where(p => p.Activo);
        if (soloBajoStock)
            query = query.Where(p => p.StockActual <= p.StockMinimo);

        return await query
            .OrderBy(p => p.Nombre)
            .Select(p => ToDto(p, p.CategoriaId != null
                ? _db.Categorias.Where(c => c.Id == p.CategoriaId).Select(c => c.Nombre).FirstOrDefault()
                : null))
            .ToListAsync(ct);
    }

    public async Task<PaginaDto<ProductoDto>> BuscarAsync(
        string? busqueda, bool soloBajoStock, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);

        var query = _db.Productos.Where(p => p.Activo);
        if (soloBajoStock)
            query = query.Where(p => p.StockActual <= p.StockMinimo);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(termino) ||
                p.Sku.ToLower().Contains(termino) ||
                (p.CategoriaId != null && _db.Categorias.Any(c => c.Id == p.CategoriaId && c.Nombre.ToLower().Contains(termino))));
        }

        var totalItems = await query.CountAsync(ct);
        var totalPaginas = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)tamanoPagina);

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(p => ToDto(p, p.CategoriaId != null
                ? _db.Categorias.Where(c => c.Id == p.CategoriaId).Select(c => c.Nombre).FirstOrDefault()
                : null))
            .ToListAsync(ct);

        return new PaginaDto<ProductoDto>(items, pagina, tamanoPagina, totalItems, totalPaginas);
    }

    public async Task<ProductoDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El producto no existe.");

        var categoriaNombre = producto.CategoriaId is null
            ? null
            : await _db.Categorias.Where(c => c.Id == producto.CategoriaId).Select(c => c.Nombre).FirstOrDefaultAsync(ct);

        return ToDto(producto, categoriaNombre);
    }

    public async Task<ProductoDto> CrearAsync(CrearProductoRequest request, CancellationToken ct = default)
    {
        var skuEnUso = await _db.Productos.AnyAsync(p => p.Sku == request.Sku, ct);
        if (skuEnUso)
            throw new AppException($"Ya existe un producto con el SKU '{request.Sku}'.");

        if (request.CategoriaId is not null)
        {
            var categoriaExiste = await _db.Categorias.AnyAsync(c => c.Id == request.CategoriaId, ct);
            if (!categoriaExiste)
                throw new AppException("La categoría indicada no existe.");
        }

        var producto = Producto.Crear(
            _tenantContext.TenantId, request.Sku, request.Nombre, request.CategoriaId,
            request.Precio, request.Costo, request.StockActual, request.StockMinimo);

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(producto.Id, ct);
    }

    public async Task<ProductoDto> EditarAsync(Guid id, EditarProductoRequest request, CancellationToken ct = default)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El producto no existe.");

        if (request.CategoriaId is not null)
        {
            var categoriaExiste = await _db.Categorias.AnyAsync(c => c.Id == request.CategoriaId, ct);
            if (!categoriaExiste)
                throw new AppException("La categoría indicada no existe.");
        }

        producto.Editar(request.Nombre, request.CategoriaId, request.Precio, request.Costo, request.StockMinimo);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(producto.Id, ct);
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El producto no existe.");

        producto.Desactivar();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProductoDto> ActivarAsync(Guid id, CancellationToken ct = default)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El producto no existe.");

        producto.Activar();
        await _db.SaveChangesAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task<ProductoDto> AjustarStockAsync(Guid id, int cantidad, string motivo, CancellationToken ct = default)
    {
        if (cantidad == 0)
            throw new AppException("La cantidad a ajustar no puede ser cero.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new AppException("Elegí un motivo para el ajuste.");

        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El producto no existe.");

        try
        {
            producto.AjustarStock(cantidad);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException(ex.Message);
        }

        _movimientoStockService.Registrar(producto.Id, producto.Nombre, cantidad, producto.StockActual, motivo);
        await _db.SaveChangesAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task<int> ActualizarPreciosMasivoAsync(ActualizarPreciosMasivoRequest request, CancellationToken ct = default)
    {
        var query = _db.Productos.Where(p => p.Activo);
        if (request.CategoriaId is not null)
            query = query.Where(p => p.CategoriaId == request.CategoriaId);

        var productos = await query.ToListAsync(ct);
        var factor = 1 + (request.Porcentaje / 100m);

        foreach (var producto in productos)
            producto.ActualizarPrecio(Math.Round(producto.Precio * factor, 2));

        await _db.SaveChangesAsync(ct);
        return productos.Count;
    }

    private static ProductoDto ToDto(Producto p, string? categoriaNombre) => new(
        p.Id, p.Sku, p.Nombre, p.CategoriaId, categoriaNombre,
        p.Precio, p.Costo, p.StockActual, p.StockMinimo, p.EnStockMinimo, p.Activo);
}
