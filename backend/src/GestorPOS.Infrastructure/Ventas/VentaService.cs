using System.Globalization;
using System.Text;
using GestorPOS.Application.Caja;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Configuracion;
using GestorPOS.Application.Ventas;
using GestorPOS.Application.Ventas.Dtos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Ventas;

public class VentaService : IVentaService
{
    private static readonly TimeZoneInfo ZonaHorariaArgentina =
        TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICajaService _cajaService;
    private readonly IMedioPagoService _medioPagoService;

    public VentaService(AppDbContext db, ITenantContext tenantContext, ICajaService cajaService, IMedioPagoService medioPagoService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cajaService = cajaService;
        _medioPagoService = medioPagoService;
    }

    public async Task<VentaDto> CrearAsync(CrearVentaRequest request, CancellationToken ct = default)
    {
        if (await _cajaService.ObtenerActualAsync(ct) is null)
            throw new AppException("No hay una caja abierta. Abrí la caja antes de registrar una venta.");

        if (request.Items.Count == 0)
            throw new AppException("La venta debe tener al menos un producto.");

        if (!await _medioPagoService.EsValidoYActivoAsync(request.MedioPago, ct))
            throw new AppException($"Medio de pago inválido: '{request.MedioPago}'.");

        var productoIds = request.Items.Select(i => i.ProductoId).ToList();
        var productos = await _db.Productos
            .Where(p => productoIds.Contains(p.Id) && p.Activo)
            .ToDictionaryAsync(p => p.Id, ct);

        var items = new List<(Producto Producto, int Cantidad)>();
        foreach (var itemRequest in request.Items)
        {
            if (!productos.TryGetValue(itemRequest.ProductoId, out var producto))
                throw new AppException("Uno de los productos de la venta no existe o está inactivo.");

            if (producto.StockActual < itemRequest.Cantidad)
                throw new AppException($"Stock insuficiente de '{producto.Nombre}' (disponible: {producto.StockActual}).");

            items.Add((producto, itemRequest.Cantidad));
        }

        var venta = Venta.Crear(_tenantContext.TenantId, _tenantContext.UsuarioId, request.MedioPago, request.TelefonoCliente, items);

        foreach (var (producto, cantidad) in items)
            producto.AjustarStock(-cantidad);

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync(ct);

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
            var desdeUtc = desde.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(v => v.FechaCreacion >= desdeUtc);
        }

        if (hasta is not null)
        {
            var hastaUtc = hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
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
            .Select(i => new VentaItemDto(i.ProductoId, i.ProductoNombre, i.Cantidad, i.PrecioUnitario, i.Subtotal))
            .ToList();

        var ticketTexto = ConstruirTicket(venta, items, nombreNegocio);
        var whatsAppLink = ConstruirLinkWhatsApp(venta.TelefonoCliente, ticketTexto);

        return new VentaDto(
            venta.Id, venta.FechaCreacion, venta.Total, venta.MedioPago,
            venta.TelefonoCliente, items, ticketTexto, whatsAppLink);
    }

    private static string ConstruirTicket(Venta venta, IReadOnlyList<VentaItemDto> items, string nombreNegocio)
    {
        var ci = CultureInfo.InvariantCulture;
        var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(venta.FechaCreacion, ZonaHorariaArgentina);
        var sb = new StringBuilder();
        sb.AppendLine(nombreNegocio);
        sb.AppendLine($"Fecha: {fechaLocal.ToString("dd/MM/yyyy HH:mm", ci)}");
        sb.AppendLine("--------------------------------");
        foreach (var item in items)
            sb.AppendLine($"{item.Cantidad}x {item.ProductoNombre} - ${item.Subtotal.ToString("0.00", ci)}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"TOTAL: ${venta.Total.ToString("0.00", ci)}");
        sb.AppendLine($"Medio de pago: {venta.MedioPago}");
        sb.AppendLine("¡Gracias por su compra!");
        return sb.ToString();
    }

    private static string? ConstruirLinkWhatsApp(string? telefonoCliente, string ticketTexto)
    {
        if (string.IsNullOrWhiteSpace(telefonoCliente)) return null;

        var soloDigitos = new string(telefonoCliente.Where(char.IsDigit).ToArray());
        if (soloDigitos.Length == 0) return null;

        return $"https://wa.me/{soloDigitos}?text={Uri.EscapeDataString(ticketTexto)}";
    }
}
