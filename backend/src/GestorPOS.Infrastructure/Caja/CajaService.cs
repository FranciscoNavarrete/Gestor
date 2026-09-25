using GestorPOS.Application.Caja;
using GestorPOS.Application.Caja.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Caja;

public class CajaService : ICajaService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CajaService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<CajaDto?> ObtenerActualAsync(CancellationToken ct = default)
    {
        var caja = await _db.CajasDiarias.FirstOrDefaultAsync(c => c.Abierta, ct);
        if (caja is null) return null;

        var movimientos = await ObtenerMovimientosAsync(caja.Id, ct);
        return ToDto(caja, [], movimientos);
    }

    public async Task<CajaDto> AbrirAsync(AbrirCajaRequest request, CancellationToken ct = default)
    {
        var yaHayAbierta = await _db.CajasDiarias.AnyAsync(c => c.Abierta, ct);
        if (yaHayAbierta)
            throw new AppException("Ya hay una caja abierta. Cerrala antes de abrir una nueva.");

        var caja = Domain.Entities.CajaDiaria.Abrir(_tenantContext.TenantId, _tenantContext.UsuarioId, request.MontoApertura);
        _db.CajasDiarias.Add(caja);
        await _db.SaveChangesAsync(ct);

        return ToDto(caja, [], []);
    }

    public async Task<CajaDto> AgregarMovimientoAsync(CrearMovimientoCajaRequest request, CancellationToken ct = default)
    {
        var caja = await _db.CajasDiarias.FirstOrDefaultAsync(c => c.Abierta, ct)
            ?? throw new AppException("No hay ninguna caja abierta.");

        var movimiento = Domain.Entities.MovimientoCaja.Crear(
            _tenantContext.TenantId, caja.Id, request.Tipo, request.Monto, request.Motivo, _tenantContext.UsuarioId);
        _db.MovimientosCaja.Add(movimiento);
        await _db.SaveChangesAsync(ct);

        var movimientos = await ObtenerMovimientosAsync(caja.Id, ct);
        return ToDto(caja, [], movimientos);
    }

    public async Task<CajaDto> CerrarAsync(CerrarCajaRequest request, CancellationToken ct = default)
    {
        var caja = await _db.CajasDiarias.FirstOrDefaultAsync(c => c.Abierta, ct)
            ?? throw new AppException("No hay ninguna caja abierta para cerrar.");

        var ventasPorMedioPago = (await _db.Ventas
            .Where(v => v.FechaCreacion >= caja.FechaCreacion)
            .GroupBy(v => v.MedioPago)
            .Select(g => new VentaPorMedioPagoDto(g.Key, g.Sum(v => v.Total)))
            .ToListAsync(ct))
            .ToDictionary(v => v.MedioPago, v => v.Total);

        // Un pago de cuenta corriente cobrado en efectivo es plata real que entra hoy, aunque la
        // venta original (a cuenta) no haya sumado nada en su momento — suma acá, no como línea aparte.
        var cobrosCuentaEnEfectivo = await _db.PagosCuenta
            .Where(p => p.MedioPago == "Efectivo" && p.FechaCreacion >= caja.FechaCreacion)
            .SumAsync(p => (decimal?)p.Monto, ct) ?? 0m;

        if (cobrosCuentaEnEfectivo > 0)
            ventasPorMedioPago["Efectivo"] = ventasPorMedioPago.GetValueOrDefault("Efectivo") + cobrosCuentaEnEfectivo;

        var ventasEfectivo = ventasPorMedioPago.GetValueOrDefault("Efectivo");

        var movimientos = await ObtenerMovimientosAsync(caja.Id, ct);
        var netoMovimientos = movimientos.Sum(m => m.Tipo == "Ingreso" ? m.Monto : -m.Monto);

        caja.Cerrar(ventasEfectivo, netoMovimientos, request.MontoCierreReal);
        await _db.SaveChangesAsync(ct);

        var desglose = ventasPorMedioPago.Select(kv => new VentaPorMedioPagoDto(kv.Key, kv.Value)).ToList();
        return ToDto(caja, desglose, movimientos);
    }

    private async Task<IReadOnlyList<MovimientoCajaDto>> ObtenerMovimientosAsync(Guid cajaDiariaId, CancellationToken ct) =>
        await _db.MovimientosCaja
            .Where(m => m.CajaDiariaId == cajaDiariaId)
            .OrderBy(m => m.FechaCreacion)
            .Select(m => new MovimientoCajaDto(m.Id, m.Tipo, m.Monto, m.Motivo, m.FechaCreacion))
            .ToListAsync(ct);

    private static CajaDto ToDto(
        Domain.Entities.CajaDiaria caja, IReadOnlyList<VentaPorMedioPagoDto> ventasPorMedioPago,
        IReadOnlyList<MovimientoCajaDto> movimientos) => new(
        caja.Id, caja.FechaCreacion, caja.MontoApertura, caja.Abierta,
        caja.MontoCierreEsperado, caja.MontoCierreReal, caja.Diferencia, caja.FechaCierre,
        ventasPorMedioPago, movimientos, movimientos.Sum(m => m.Tipo == "Ingreso" ? m.Monto : -m.Monto));
}
