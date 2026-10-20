using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using GestorPOS.Application.Common.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.MovimientosStock;
using GestorPOS.Application.Notificaciones;
using GestorPOS.Domain.Common;
using GestorPOS.Domain.Entities;
using GestorPOS.Domain.Enums;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Catalog;

public class ProductoService : IProductoService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IMovimientoStockService _movimientoStockService;
    private readonly INotificacionPushService _notificacionPushService;

    public ProductoService(
        AppDbContext db, ITenantContext tenantContext, IMovimientoStockService movimientoStockService,
        INotificacionPushService notificacionPushService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _notificacionPushService = notificacionPushService;
        _movimientoStockService = movimientoStockService;
    }

    public async Task<IReadOnlyList<ProductoDto>> ListarAsync(bool soloBajoStock, CancellationToken ct = default)
    {
        var query = _db.Productos.Where(p => p.Activo);
        if (soloBajoStock)
            query = query.Where(p => p.StockActual <= p.StockMinimo);

        var productos = await query
            .OrderBy(p => p.Nombre)
            .Select(p => ToDto(p,
                p.CategoriaId != null
                    ? _db.Categorias.Where(c => c.Id == p.CategoriaId).Select(c => c.Nombre).FirstOrDefault()
                    : null,
                p.MarcaId != null
                    ? _db.Marcas.Where(m => m.Id == p.MarcaId).Select(m => m.Nombre).FirstOrDefault()
                    : null))
            .ToListAsync(ct);

        return await ConPresentacionesAsync(productos, ct);
    }

    public async Task<PaginaDto<ProductoDto>> BuscarAsync(
        string? busqueda, bool soloBajoStock, bool soloInactivos, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);

        // Mismo criterio que "Solo stock bajo": el filtro aísla, no suma — activos por defecto,
        // o solo los inactivos cuando está prendido, nunca los dos grupos mezclados.
        var query = _db.Productos.Where(p => p.Activo == !soloInactivos);
        if (soloBajoStock)
            query = query.Where(p => p.StockActual <= p.StockMinimo);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(termino) ||
                p.Sku.ToLower().Contains(termino) ||
                (p.CategoriaId != null && _db.Categorias.Any(c => c.Id == p.CategoriaId && c.Nombre.ToLower().Contains(termino))) ||
                (p.MarcaId != null && _db.Marcas.Any(m => m.Id == p.MarcaId && m.Nombre.ToLower().Contains(termino))));
        }

        var totalItems = await query.CountAsync(ct);
        var totalPaginas = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)tamanoPagina);

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(p => ToDto(p,
                p.CategoriaId != null
                    ? _db.Categorias.Where(c => c.Id == p.CategoriaId).Select(c => c.Nombre).FirstOrDefault()
                    : null,
                p.MarcaId != null
                    ? _db.Marcas.Where(m => m.Id == p.MarcaId).Select(m => m.Nombre).FirstOrDefault()
                    : null))
            .ToListAsync(ct);

        return new PaginaDto<ProductoDto>(
            await ConPresentacionesAsync(items, ct), pagina, tamanoPagina, totalItems, totalPaginas);
    }

    public async Task<ProductoDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El producto no existe.");

        var categoriaNombre = producto.CategoriaId is null
            ? null
            : await _db.Categorias.Where(c => c.Id == producto.CategoriaId).Select(c => c.Nombre).FirstOrDefaultAsync(ct);

        var marcaNombre = producto.MarcaId is null
            ? null
            : await _db.Marcas.Where(m => m.Id == producto.MarcaId).Select(m => m.Nombre).FirstOrDefaultAsync(ct);

        var dto = ToDto(producto, categoriaNombre, marcaNombre);
        return (await ConPresentacionesAsync([dto], ct))[0];
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

        await ValidarMarcaAsync(request.MarcaId, ct);

        var unidad = ParsearUnidad(request.Unidad);
        var presentaciones = request.Presentaciones ?? [];
        if ((unidad != UnidadMedida.Unidad || presentaciones.Count > 0) && !_tenantContext.TieneFeature(MedidasFeature.Clave))
            throw new AppException(MensajeFuncionNoActivada);

        var producto = Producto.Crear(
            _tenantContext.TenantId, request.Sku, request.Nombre, request.CategoriaId,
            request.Precio, request.Costo, request.StockActual, request.StockMinimo, unidad, request.MarcaId);

        var nombresUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pedida in presentaciones)
        {
            var presentacion = ProductoPresentacion.Crear(producto, pedida.Nombre, pedida.CantidadBase, pedida.Precio);
            if (!nombresUsados.Add(presentacion.Nombre))
                throw new AppException($"Ya hay una presentación llamada '{presentacion.Nombre}'.");
            _db.ProductoPresentaciones.Add(presentacion);
        }

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

        await ValidarMarcaAsync(request.MarcaId, ct);

        producto.Editar(request.Nombre, request.CategoriaId, request.Precio, request.Costo, request.StockMinimo, request.MarcaId);
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

    public async Task<ProductoDto> AjustarStockAsync(Guid id, decimal cantidad, string motivo, CancellationToken ct = default)
    {
        if (cantidad == 0)
            throw new AppException("La cantidad a ajustar no puede ser cero.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new AppException("Elegí un motivo para el ajuste.");

        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El producto no existe.");

        var estabaBajoAntes = producto.EnStockMinimo;

        try
        {
            producto.AjustarStock(cantidad);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw new AppException(ex.Message);
        }

        _movimientoStockService.Registrar(producto.Id, producto.Nombre, cantidad, producto.StockActual, motivo);
        await _db.SaveChangesAsync(ct);

        // Avisa solo en el momento en que CRUZA el mínimo, no en cada ajuste posterior mientras sigue
        // bajo — si no, spamea un push por cada venta chica mientras el stock no se repone.
        if (!estabaBajoAntes && producto.EnStockMinimo)
            await _notificacionPushService.NotificarStockBajoAsync(producto.Nombre, producto.StockActual, producto.Unidad.Simbolo(), ct);

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

    public async Task<PresentacionDto> CrearPresentacionAsync(
        Guid productoId, CrearPresentacionRequest request, CancellationToken ct = default)
    {
        if (!_tenantContext.TieneFeature(MedidasFeature.Clave))
            throw new AppException(MensajeFuncionNoActivada);

        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == productoId, ct)
            ?? throw new AppException("El producto no existe.");

        var presentacion = ProductoPresentacion.Crear(producto, request.Nombre, request.CantidadBase, request.Precio);
        await ValidarNombreLibreAsync(productoId, presentacion.Nombre, null, ct);

        _db.ProductoPresentaciones.Add(presentacion);
        await _db.SaveChangesAsync(ct);
        return ToDto(presentacion);
    }

    public async Task<PresentacionDto> EditarPresentacionAsync(
        Guid productoId, Guid presentacionId, EditarPresentacionRequest request, CancellationToken ct = default)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == productoId, ct)
            ?? throw new AppException("El producto no existe.");
        var presentacion = await ObtenerPresentacionAsync(productoId, presentacionId, ct);

        presentacion.Editar(producto, request.Nombre, request.CantidadBase, request.Precio);
        await ValidarNombreLibreAsync(productoId, presentacion.Nombre, presentacionId, ct);

        await _db.SaveChangesAsync(ct);
        return ToDto(presentacion);
    }

    public async Task DesactivarPresentacionAsync(Guid productoId, Guid presentacionId, CancellationToken ct = default)
    {
        var presentacion = await ObtenerPresentacionAsync(productoId, presentacionId, ct);
        presentacion.Desactivar();
        await _db.SaveChangesAsync(ct);
    }

    public async Task ActivarPresentacionAsync(Guid productoId, Guid presentacionId, CancellationToken ct = default)
    {
        var presentacion = await ObtenerPresentacionAsync(productoId, presentacionId, ct);
        await ValidarNombreLibreAsync(productoId, presentacion.Nombre, presentacionId, ct);
        presentacion.Activar();
        await _db.SaveChangesAsync(ct);
    }

    private const string MensajeFuncionNoActivada =
        "La función Medidas y presentaciones no está activada para tu negocio.";

    private static UnidadMedida ParsearUnidad(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return UnidadMedida.Unidad;
        if (Enum.TryParse<UnidadMedida>(texto.Trim(), ignoreCase: true, out var unidad) && Enum.IsDefined(unidad))
            return unidad;
        throw new AppException("La unidad indicada no existe. Usá Unidad, Kilo, Litro o Metro.");
    }

    private async Task<ProductoPresentacion> ObtenerPresentacionAsync(Guid productoId, Guid presentacionId, CancellationToken ct) =>
        await _db.ProductoPresentaciones.FirstOrDefaultAsync(p => p.Id == presentacionId && p.ProductoId == productoId, ct)
            ?? throw new AppException("La presentación no existe.");

    private async Task ValidarNombreLibreAsync(Guid productoId, string nombre, Guid? excluirId, CancellationToken ct)
    {
        var minuscula = nombre.ToLower();
        var repetido = await _db.ProductoPresentaciones.AnyAsync(
            p => p.ProductoId == productoId && p.Activo && p.Id != excluirId && p.Nombre.ToLower() == minuscula, ct);
        if (repetido)
            throw new AppException($"Ya hay una presentación llamada '{nombre}'.");
    }

    /// <summary>Completa cada producto con sus presentaciones activas, en una sola consulta.</summary>
    private async Task<IReadOnlyList<ProductoDto>> ConPresentacionesAsync(IReadOnlyList<ProductoDto> productos, CancellationToken ct)
    {
        if (productos.Count == 0)
            return productos;

        var ids = productos.Select(p => p.Id).ToList();
        var presentaciones = await _db.ProductoPresentaciones
            .Where(p => ids.Contains(p.ProductoId) && p.Activo)
            .OrderBy(p => p.CantidadBase)
            .ToListAsync(ct);
        if (presentaciones.Count == 0)
            return productos;

        var porProducto = presentaciones.GroupBy(p => p.ProductoId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PresentacionDto>)g.Select(ToDto).ToList());

        return productos
            .Select(p => porProducto.TryGetValue(p.Id, out var lista) ? p with { Presentaciones = lista } : p)
            .ToList();
    }

    private static PresentacionDto ToDto(ProductoPresentacion p) =>
        new(p.Id, p.ProductoId, p.Nombre, p.CantidadBase, p.Precio, p.Activo);

    private async Task ValidarMarcaAsync(Guid? marcaId, CancellationToken ct)
    {
        if (marcaId is null) return;
        if (!await _db.Marcas.AnyAsync(m => m.Id == marcaId, ct))
            throw new AppException("La marca indicada no existe.");
    }

    private static ProductoDto ToDto(Producto p, string? categoriaNombre, string? marcaNombre) => new(
        p.Id, p.Sku, p.Nombre, p.CategoriaId, categoriaNombre, p.MarcaId, marcaNombre,
        p.Precio, p.Costo, p.StockActual, p.StockMinimo, p.EnStockMinimo, p.Activo,
        p.Unidad.ToString(), Array.Empty<PresentacionDto>());
}
