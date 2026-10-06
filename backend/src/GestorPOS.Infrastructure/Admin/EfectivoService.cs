using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Admin;

public class EfectivoService : IEfectivoService
{
    private readonly AppDbContext _db;
    private readonly ICurrentAdminContext _currentAdmin;

    public EfectivoService(AppDbContext db, ICurrentAdminContext currentAdmin)
    {
        _db = db;
        _currentAdmin = currentAdmin;
    }

    // Lo cobrado en efectivo por cada vendedor: los primeros pagos en efectivo que él mismo registró.
    private IQueryable<PagoManual> CobrosEnEfectivo() =>
        _db.PagosManuales.AsNoTracking().Where(p =>
            p.Metodo == MetodoPagoManual.Efectivo && p.Estado == EstadoPagoManual.Confirmado
            && p.RegistradoPorRol == nameof(AdminRol.Vendedor));

    public async Task<EfectivoResumenDto> ResumenAsync(CancellationToken ct = default)
    {
        var soloEste = _currentAdmin.EsOperador ? (Guid?)null : _currentAdmin.AdminId;

        var cobros = CobrosEnEfectivo();
        var movimientos = _db.MovimientosEfectivo.AsNoTracking().AsQueryable();
        if (soloEste is not null)
        {
            cobros = cobros.Where(p => p.RegistradoPorId == soloEste);
            movimientos = movimientos.Where(m => m.VendedorId == soloEste);
        }

        var cobrado = await cobros.GroupBy(p => p.RegistradoPorId)
            .Select(g => new { VendedorId = g.Key, Cantidad = g.Count(), Total = g.Sum(p => p.Monto) })
            .ToListAsync(ct);
        var saldado = await movimientos.GroupBy(m => new { m.VendedorId, m.Tipo })
            .Select(g => new { g.Key.VendedorId, g.Key.Tipo, Total = g.Sum(m => m.Monto) })
            .ToListAsync(ct);

        var ids = cobrado.Select(c => c.VendedorId).Concat(saldado.Select(s => s.VendedorId)).Distinct().ToList();
        var nombres = await _db.AdminUsuarios.Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Nombre, ct);

        var filas = ids.Select(id =>
        {
            var c = cobrado.FirstOrDefault(x => x.VendedorId == id);
            var entregado = saldado.Where(s => s.VendedorId == id && s.Tipo == TipoMovimientoEfectivo.Entrega).Sum(s => s.Total);
            var compensado = saldado.Where(s => s.VendedorId == id && s.Tipo == TipoMovimientoEfectivo.Compensacion).Sum(s => s.Total);
            var total = c?.Total ?? 0;
            return new EfectivoVendedorDto(id, nombres.GetValueOrDefault(id, "Vendedor"), c?.Cantidad ?? 0, total, entregado, compensado, total - entregado - compensado);
        })
        .OrderByDescending(f => f.EnPoder).ThenBy(f => f.Nombre)
        .ToList();

        return new EfectivoResumenDto(filas);
    }

    public async Task<IReadOnlyList<EfectivoMovimientoDto>> MovimientosAsync(Guid vendedorId, CancellationToken ct = default)
    {
        if (!_currentAdmin.EsOperador && vendedorId != _currentAdmin.AdminId)
            throw new AppException("No tenés acceso a esos movimientos.");

        var cobros = await CobrosEnEfectivo().Where(p => p.RegistradoPorId == vendedorId).ToListAsync(ct);
        var negocios = await _db.Tenants.IgnoreQueryFilters()
            .Where(t => cobros.Select(c => c.TenantId).Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Nombre, ct);
        var otros = await _db.MovimientosEfectivo.AsNoTracking().Where(m => m.VendedorId == vendedorId).ToListAsync(ct);

        var lista = new List<(DateTime Orden, EfectivoMovimientoDto Item)>();
        lista.AddRange(cobros.Select(p => (p.FechaRegistroUtc, new EfectivoMovimientoDto(
            p.Id, "Cobro", p.FechaRecepcion ?? DateOnly.FromDateTime(CalculadoraComisiones.AHoraArgentina(p.FechaRegistroUtc)),
            p.Monto, $"Alta de {negocios.GetValueOrDefault(p.TenantId, "un negocio")}", p.RegistradoPorNombre, false))));
        lista.AddRange(otros.Select(m => (m.FechaRegistroUtc, new EfectivoMovimientoDto(
            m.Id, m.Tipo.ToString(), m.Fecha, m.Monto,
            m.Tipo == TipoMovimientoEfectivo.Compensacion ? "Descontado de una liquidación" : (m.Nota ?? "Entrega de efectivo"),
            m.RegistradoPorNombre, m.Tipo == TipoMovimientoEfectivo.Entrega && _currentAdmin.EsOperador))));

        return lista.OrderByDescending(x => x.Orden).Select(x => x.Item).ToList();
    }

    public async Task<EfectivoVendedorDto> RegistrarEntregaAsync(RegistrarEntregaRequest request, CancellationToken ct = default)
    {
        var vendedor = await _db.AdminUsuarios.FirstOrDefaultAsync(a => a.Id == request.VendedorId && a.Rol == AdminRol.Vendedor, ct)
            ?? throw new AppException("El vendedor no existe.");
        if (request.Monto <= 0)
            throw new AppException("El monto tiene que ser mayor a cero.");
        if (request.Nota is { Length: > 300 })
            throw new AppException("La nota no puede superar los 300 caracteres.");

        var hoy = DateOnly.FromDateTime(CalculadoraComisiones.AHoraArgentina(DateTime.UtcNow));
        var fecha = request.Fecha ?? hoy;
        if (fecha > hoy)
            throw new AppException("La fecha de la entrega no puede ser mayor a hoy.");

        var enPoder = await EnPoderAsync(vendedor.Id, ct);
        if (request.Monto > enPoder)
            throw new AppException($"{vendedor.Nombre} tiene a su cargo {Dinero(enPoder)}: no puede entregar más que eso.");

        var admin = await _db.AdminUsuarios.Where(a => a.Id == _currentAdmin.AdminId).Select(a => a.Nombre).FirstOrDefaultAsync(ct) ?? "Admin";
        _db.MovimientosEfectivo.Add(MovimientoEfectivo.Entrega(vendedor.Id, request.Monto, fecha, request.Nota, _currentAdmin.AdminId, admin));
        _db.MovimientosAdmin.Add(MovimientoAdmin.Crear(
            _currentAdmin.AdminId, admin, _currentAdmin.Rol, "efectivo.entrega", "usuario", vendedor.Id, vendedor.Nombre,
            Dinero(request.Monto)));
        await _db.SaveChangesAsync(ct);

        var resumen = await ResumenAsync(ct);
        return resumen.Vendedores.First(v => v.VendedorId == vendedor.Id);
    }

    public async Task AnularEntregaAsync(Guid id, CancellationToken ct = default)
    {
        var entrega = await _db.MovimientosEfectivo.FirstOrDefaultAsync(m => m.Id == id && m.Tipo == TipoMovimientoEfectivo.Entrega, ct)
            ?? throw new AppException("La entrega no existe.");

        var vendedor = await _db.AdminUsuarios.Where(a => a.Id == entrega.VendedorId).Select(a => a.Nombre).FirstOrDefaultAsync(ct) ?? "Vendedor";
        var admin = await _db.AdminUsuarios.Where(a => a.Id == _currentAdmin.AdminId).Select(a => a.Nombre).FirstOrDefaultAsync(ct) ?? "Admin";
        _db.MovimientosEfectivo.Remove(entrega);
        _db.MovimientosAdmin.Add(MovimientoAdmin.Crear(
            _currentAdmin.AdminId, admin, _currentAdmin.Rol, "efectivo.entrega_anulada", "usuario", entrega.VendedorId, vendedor,
            Dinero(entrega.Monto)));
        await _db.SaveChangesAsync(ct);
    }

    private static string Dinero(decimal valor) =>
        "$" + Math.Round(valor).ToString("N0", new System.Globalization.CultureInfo("es-AR"));

    public async Task<decimal> EnPoderAsync(Guid vendedorId, CancellationToken ct = default)
    {
        var cobrado = await CobrosEnEfectivo().Where(p => p.RegistradoPorId == vendedorId).SumAsync(p => (decimal?)p.Monto, ct) ?? 0;
        var saldado = await _db.MovimientosEfectivo.AsNoTracking().Where(m => m.VendedorId == vendedorId).SumAsync(m => (decimal?)m.Monto, ct) ?? 0;
        return cobrado - saldado;
    }
}
