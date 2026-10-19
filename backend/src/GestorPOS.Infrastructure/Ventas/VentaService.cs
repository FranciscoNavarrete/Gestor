using System.Globalization;
using System.Text;
using GestorPOS.Application.Caja;
using GestorPOS.Application.Clientes;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Configuracion;
using GestorPOS.Application.MovimientosStock;
using GestorPOS.Application.Notificaciones;
using GestorPOS.Application.Ventas;
using GestorPOS.Application.Ventas.Dtos;
using GestorPOS.Domain.Common;
using GestorPOS.Domain.Entities;
using GestorPOS.Domain.Enums;
using GestorPOS.Infrastructure.Common;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Ventas;

public class VentaService : IVentaService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICajaService _cajaService;
    private readonly IMedioPagoService _medioPagoService;
    private readonly IClienteService _clienteService;
    private readonly IMovimientoStockService _movimientoStockService;
    private readonly INotificacionPushService _notificacionPushService;

    public VentaService(
        AppDbContext db, ITenantContext tenantContext, ICajaService cajaService,
        IMedioPagoService medioPagoService, IClienteService clienteService,
        IMovimientoStockService movimientoStockService, INotificacionPushService notificacionPushService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cajaService = cajaService;
        _medioPagoService = medioPagoService;
        _clienteService = clienteService;
        _movimientoStockService = movimientoStockService;
        _notificacionPushService = notificacionPushService;
    }

    public async Task<VentaDto> CrearAsync(CrearVentaRequest request, CancellationToken ct = default)
    {
        if (await _cajaService.ObtenerActualAsync(ct) is null)
            throw new AppException("No hay una caja abierta. Abrí la caja antes de registrar una venta.");

        if (request.Items.Count == 0)
            throw new AppException("La venta debe tener al menos un producto.");

        if (request.MedioPago == CuentaCorriente.MedioPago)
        {
            if (!_tenantContext.TieneFeature(CuentaCorriente.Feature))
                throw new AppException($"Medio de pago inválido: '{request.MedioPago}'.");
            if (string.IsNullOrWhiteSpace(request.TelefonoCliente))
                throw new AppException("Para vender a cuenta necesitás cargar el teléfono del cliente.");
        }
        else if (!await _medioPagoService.EsValidoYActivoAsync(request.MedioPago, ct))
        {
            throw new AppException($"Medio de pago inválido: '{request.MedioPago}'.");
        }

        var productoIds = request.Items.Select(i => i.ProductoId).Distinct().ToList();
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

        var items = new List<(Producto Producto, decimal Cantidad, ProductoPresentacion? Presentacion)>();
        foreach (var itemRequest in request.Items)
        {
            if (!productos.TryGetValue(itemRequest.ProductoId, out var producto))
                throw new AppException("Uno de los productos de la venta no existe o está inactivo.");

            ProductoPresentacion? presentacion = null;
            if (itemRequest.PresentacionId is { } presentacionId
                && (!presentaciones.TryGetValue(presentacionId, out presentacion) || presentacion.ProductoId != producto.Id))
                throw new AppException("Una de las presentaciones de la venta no existe, está desactivada o no es de ese producto.");

            items.Add((producto, itemRequest.Cantidad, presentacion));
        }

        var venta = Venta.Crear(_tenantContext.TenantId, _tenantContext.UsuarioId, request.MedioPago, request.TelefonoCliente, items);

        // El stock se controla en la unidad del producto y sumando todas sus líneas: suelto + bolsas del mismo
        // producto salen del mismo stock.
        foreach (var lineasDelProducto in venta.Items.GroupBy(i => i.ProductoId))
        {
            var producto = productos[lineasDelProducto.Key];
            if (producto.StockActual < lineasDelProducto.Sum(i => i.CantidadBase))
                throw new AppException($"Stock insuficiente de '{producto.Nombre}' (disponible: {TextoDisponible(producto)}).");
        }

        if (!string.IsNullOrWhiteSpace(request.TelefonoCliente))
        {
            var clienteId = await _clienteService.ObtenerOCrearPorTelefonoAsync(
                request.TelefonoCliente, request.NombreCliente, ct);
            venta.AsignarCliente(clienteId);
        }

        var productosQueCruzanMinimo = new List<Producto>();
        foreach (var item in venta.Items)
        {
            var producto = productos[item.ProductoId];
            var estabaBajoAntes = producto.EnStockMinimo;
            producto.AjustarStock(-item.CantidadBase);
            _movimientoStockService.Registrar(producto.Id, producto.Nombre, -item.CantidadBase, producto.StockActual, "Venta");

            if (!estabaBajoAntes && producto.EnStockMinimo && !productosQueCruzanMinimo.Contains(producto))
                productosQueCruzanMinimo.Add(producto);
        }

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync(ct);

        // Avisa solo en el momento en que CADA producto CRUZA el mínimo, no en cada venta posterior
        // mientras sigue bajo — mismo criterio que ProductoService.AjustarStockAsync.
        foreach (var producto in productosQueCruzanMinimo)
            await _notificacionPushService.NotificarStockBajoAsync(producto.Nombre, producto.StockActual, producto.Unidad.Simbolo(), ct);

        return await ObtenerAsync(venta.Id, ct);
    }

    public async Task<VentaDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var venta = await _db.Ventas
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("La venta no existe.");

        var nombreNegocio = await _db.Tenants
            .Where(t => t.Id == venta.TenantId)
            .Select(t => t.Nombre)
            .FirstOrDefaultAsync(ct) ?? "";

        return ToDto(venta, nombreNegocio);
    }

    public async Task<IReadOnlyList<VentaResumenDto>> ListarAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
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

        return await query
            .OrderByDescending(v => v.FechaCreacion)
            .Select(v => new VentaResumenDto(v.Id, v.FechaCreacion, v.Total, v.MedioPago))
            .ToListAsync(ct);
    }

    private static VentaDto ToDto(Venta venta, string nombreNegocio)
    {
        var items = venta.Items
            .Select(i => new VentaItemDto(
                i.ProductoId, i.ProductoNombre, i.Cantidad, i.PrecioUnitario, i.Subtotal,
                i.PresentacionNombre, i.Unidad.ToString()))
            .ToList();

        var ticketTexto = ConstruirTicket(venta, items, nombreNegocio);
        var mensajeWhatsApp = ConstruirMensajeWhatsApp(venta, items, nombreNegocio);
        var whatsAppLink = ConstruirLinkWhatsApp(venta.TelefonoCliente, mensajeWhatsApp);

        return new VentaDto(
            venta.Id, venta.FechaCreacion, venta.Total, venta.MedioPago,
            venta.TelefonoCliente, items, ticketTexto, whatsAppLink);
    }

    /// <summary>"3x Gaseosa" (por unidad, igual que siempre), "2x Alimento (Bolsa 15 kg)" o "0.35 kg Alimento".</summary>
    private static string DescripcionLinea(VentaItemDto item)
    {
        var cantidad = ReglasCantidad.Texto(item.Cantidad);
        if (item.PresentacionNombre is not null)
            return $"{cantidad}x {item.ProductoNombre} ({item.PresentacionNombre})";
        if (Enum.TryParse<UnidadMedida>(item.Unidad, out var unidad) && unidad != UnidadMedida.Unidad)
            return $"{cantidad} {unidad.Simbolo()} {item.ProductoNombre}";
        return $"{cantidad}x {item.ProductoNombre}";
    }

    private static string TextoDisponible(Producto producto) =>
        producto.Unidad == UnidadMedida.Unidad
            ? ReglasCantidad.Texto(producto.StockActual)
            : $"{ReglasCantidad.Texto(producto.StockActual)} {producto.Unidad.Simbolo()}";

    private static string ConstruirTicket(Venta venta, IReadOnlyList<VentaItemDto> items, string nombreNegocio)
    {
        var ci = CultureInfo.InvariantCulture;
        var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(venta.FechaCreacion, ZonaHoraria.Argentina);
        var sb = new StringBuilder();
        sb.AppendLine(nombreNegocio);
        sb.AppendLine($"Fecha: {fechaLocal.ToString("dd/MM/yyyy HH:mm", ci)}");
        sb.AppendLine("--------------------------------");
        foreach (var item in items)
            sb.AppendLine($"{DescripcionLinea(item)} - ${item.Subtotal.ToString("0.00", ci)}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"TOTAL: ${venta.Total.ToString("0.00", ci)}");
        sb.AppendLine($"Medio de pago: {venta.MedioPago}");
        sb.AppendLine("¡Gracias por su compra!");
        return sb.ToString();
    }

    private static string ConstruirMensajeWhatsApp(Venta venta, IReadOnlyList<VentaItemDto> items, string nombreNegocio)
    {
        var ci = CultureInfo.InvariantCulture;
        var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(venta.FechaCreacion, ZonaHoraria.Argentina);
        var sb = new StringBuilder();
        sb.AppendLine($"🧾 *{nombreNegocio}*");
        sb.AppendLine(fechaLocal.ToString("dd/MM/yyyy HH:mm", ci));
        sb.AppendLine();
        foreach (var item in items)
            sb.AppendLine($"{DescripcionLinea(item)} - ${item.Subtotal.ToString("0.00", ci)}");
        sb.AppendLine("──────────────────");
        sb.AppendLine($"*TOTAL: ${venta.Total.ToString("0.00", ci)}*");
        sb.AppendLine($"Medio de pago: {venta.MedioPago}");
        sb.AppendLine();
        sb.AppendLine("¡Gracias por tu compra! 🙌");
        return sb.ToString();
    }

    private static string? ConstruirLinkWhatsApp(string? telefonoCliente, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(telefonoCliente)) return null;

        var soloDigitos = new string(telefonoCliente.Where(char.IsDigit).ToArray());
        if (soloDigitos.Length == 0) return null;

        return $"https://wa.me/{soloDigitos}?text={Uri.EscapeDataString(mensaje)}";
    }
}
