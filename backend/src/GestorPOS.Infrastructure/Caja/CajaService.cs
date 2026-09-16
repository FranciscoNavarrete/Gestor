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
        return caja is null ? null : ToDto(caja, []);
    }

    public async Task<CajaDto> AbrirAsync(AbrirCajaRequest request, CancellationToken ct = default)
    {
        var yaHayAbierta = await _db.CajasDiarias.AnyAsync(c => c.Abierta, ct);
        if (yaHayAbierta)
            throw new AppException("Ya hay una caja abierta. Cerrala antes de abrir una nueva.");

        var caja = Domain.Entities.CajaDiaria.Abrir(_tenantContext.TenantId, _tenantContext.UsuarioId, request.MontoApertura);
        _db.CajasDiarias.Add(caja);
        await _db.SaveChangesAsync(ct);

        return ToDto(caja, []);
    }

    public async Task<CajaDto> CerrarAsync(CerrarCajaRequest request, CancellationToken ct = default)
    {
        var caja = await _db.CajasDiarias.FirstOrDefaultAsync(c => c.Abierta, ct)
            ?? throw new AppException("No hay ninguna caja abierta para cerrar.");

        var ventasPorMedioPago = await _db.Ventas
            .Where(v => v.FechaCreacion >= caja.FechaCreacion)
            .GroupBy(v => v.MedioPago)
            .Select(g => new VentaPorMedioPagoDto(g.Key, g.Sum(v => v.Total)))
            .ToListAsync(ct);

        var ventasEfectivo = ventasPorMedioPago
            .FirstOrDefault(v => v.MedioPago == "Efectivo")?.Total ?? 0m;

        caja.Cerrar(ventasEfectivo, request.MontoCierreReal);
        await _db.SaveChangesAsync(ct);

        return ToDto(caja, ventasPorMedioPago);
    }

    private static CajaDto ToDto(Domain.Entities.CajaDiaria caja, IReadOnlyList<VentaPorMedioPagoDto> ventasPorMedioPago) => new(
        caja.Id, caja.FechaCreacion, caja.MontoApertura, caja.Abierta,
        caja.MontoCierreEsperado, caja.MontoCierreReal, caja.Diferencia, caja.FechaCierre,
        ventasPorMedioPago);
}
