using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.FacturasProveedor;
using GestorPOS.Application.FacturasProveedor.Dtos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.FacturasProveedor;

public class FacturaProveedorService : IFacturaProveedorService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public FacturaProveedorService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<FacturaProveedorDto> CrearAsync(CrearFacturaProveedorRequest request, CancellationToken ct = default)
    {
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == request.ProveedorId && p.Activo, ct)
            ?? throw new AppException("El proveedor no existe o está inactivo.");

        if (string.IsNullOrWhiteSpace(proveedor.Cuit))
            throw new AppException($"\"{proveedor.Nombre}\" no tiene CUIT cargado — agregalo en Proveedores antes de facturarlo.");

        var factura = FacturaProveedor.Crear(
            _tenantContext.TenantId, proveedor, request.NumeroFactura, request.Monto,
            request.FechaEmision, request.FechaVencimiento, _tenantContext.UsuarioId);

        _db.FacturasProveedor.Add(factura);
        await _db.SaveChangesAsync(ct);

        return ToDto(factura, 0m);
    }

    public async Task<IReadOnlyList<FacturaProveedorDto>> ListarAsync(
        Guid? proveedorId, DateOnly? desde, DateOnly? hasta, bool incluirPagadas, CancellationToken ct = default)
    {
        var query = _db.FacturasProveedor.AsQueryable();

        if (proveedorId is not null)
            query = query.Where(f => f.ProveedorId == proveedorId);
        if (desde is not null)
            query = query.Where(f => f.FechaEmision >= desde.Value);
        if (hasta is not null)
            query = query.Where(f => f.FechaEmision <= hasta.Value);

        var facturas = await query.OrderByDescending(f => f.FechaEmision).ToListAsync(ct);
        var pagosPorFactura = await ObtenerPagosPorFacturaAsync(facturas.Select(f => f.Id), ct);

        var dtos = facturas
            .Select(f => ToDto(f, pagosPorFactura.GetValueOrDefault(f.Id)))
            .Where(dto => incluirPagadas || dto.Saldo > 0)
            .ToList();

        return dtos;
    }

    public async Task<ResumenFacturasProveedorDto> ResumenAsync(CancellationToken ct = default)
    {
        var facturas = await _db.FacturasProveedor.ToListAsync(ct);
        var pagosPorFactura = await ObtenerPagosPorFacturaAsync(facturas.Select(f => f.Id), ct);

        var saldos = facturas.Select(f => f.Monto - pagosPorFactura.GetValueOrDefault(f.Id)).Where(s => s > 0).ToList();

        return new ResumenFacturasProveedorDto(saldos.Sum(), saldos.Count);
    }

    public async Task<FacturaProveedorDto> RegistrarPagoAsync(Guid id, RegistrarPagoFacturaRequest request, CancellationToken ct = default)
    {
        var factura = await _db.FacturasProveedor.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new AppException("La factura no existe.");

        var totalPagado = await _db.PagosFacturaProveedor
            .Where(p => p.FacturaProveedorId == id)
            .SumAsync(p => (decimal?)p.Monto, ct) ?? 0m;
        var saldoActual = factura.Monto - totalPagado;

        if (request.Monto > saldoActual)
            throw new AppException($"La factura tiene un saldo de ${saldoActual}, no se puede registrar un pago mayor.");

        var pago = PagoFacturaProveedor.Crear(_tenantContext.TenantId, id, request.Monto, request.MedioPago, _tenantContext.UsuarioId);
        _db.PagosFacturaProveedor.Add(pago);
        await _db.SaveChangesAsync(ct);

        return ToDto(factura, totalPagado + request.Monto);
    }

    private async Task<Dictionary<Guid, decimal>> ObtenerPagosPorFacturaAsync(IEnumerable<Guid> facturaIds, CancellationToken ct)
    {
        var ids = facturaIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, decimal>();

        return await _db.PagosFacturaProveedor
            .Where(p => ids.Contains(p.FacturaProveedorId))
            .GroupBy(p => p.FacturaProveedorId)
            .Select(g => new { FacturaId = g.Key, Total = g.Sum(p => p.Monto) })
            .ToDictionaryAsync(x => x.FacturaId, x => x.Total, ct);
    }

    private static FacturaProveedorDto ToDto(FacturaProveedor factura, decimal totalPagado)
    {
        var saldo = factura.Monto - totalPagado;
        var estado = saldo <= 0 ? "Pagada" : totalPagado > 0 ? "Pago parcial" : "Pendiente";

        return new FacturaProveedorDto(
            factura.Id, factura.ProveedorId, factura.ProveedorNombre, factura.NumeroFactura,
            factura.Monto, saldo, estado, factura.FechaEmision, factura.FechaVencimiento, factura.FechaCreacion);
    }
}
