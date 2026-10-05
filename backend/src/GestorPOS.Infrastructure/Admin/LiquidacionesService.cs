using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestorPOS.Infrastructure.Admin;

public class LiquidacionesService : ILiquidacionesService
{
    private static readonly string[] Meses =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    private readonly AppDbContext _db;
    private readonly ICurrentAdminContext _currentAdmin;
    private readonly IFluxoService _fluxo;
    private readonly TarifaComision _tarifa;

    public LiquidacionesService(AppDbContext db, ICurrentAdminContext currentAdmin, IFluxoService fluxo, IConfiguration config)
    {
        _db = db;
        _currentAdmin = currentAdmin;
        _fluxo = fluxo;
        _tarifa = TarifaComisionConfiguracion.Leer(config);
    }

    // Ventas de un mes (por fecha de alta, hora de Argentina) con su estado en Fluxo.
    private sealed record VentaDelMes(Guid TenantId, string Nombre, Guid VendedorId, DateTime FechaAltaUtc, bool Cobrada, bool EsperandoCobro);

    public async Task<LiquidacionesMesDto> ResumenAsync(int anio, int mes, CancellationToken ct = default)
    {
        ValidarMesTerminado(anio, mes);

        var ventas = await VentasDelMesAsync(anio, mes, null, ct);
        var liquidaciones = await _db.Liquidaciones.AsNoTracking().Include(l => l.Items)
            .Where(l => l.Anio == anio && l.Mes == mes).ToListAsync(ct);

        var idsVendedores = ventas.Select(v => v.VendedorId).Concat(liquidaciones.Select(l => l.VendedorId)).Distinct().ToList();
        var nombres = await _db.AdminUsuarios.Where(a => idsVendedores.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Nombre, ct);

        var yaLiquidadas = liquidaciones.SelectMany(l => l.Items).Select(i => i.TenantId).ToHashSet();
        var asignaciones = CalculadoraComisiones.Asignar(
            ventas.Where(v => v.Cobrada).Select(v => new VentaConfirmada(v.TenantId, v.VendedorId, v.FechaAltaUtc)), _tarifa, yaLiquidadas);

        var filas = idsVendedores.Select(vendedorId =>
        {
            var propias = liquidaciones.Where(l => l.VendedorId == vendedorId).OrderBy(l => l.FechaCierreUtc).ToList();
            var sinLiquidar = ventas.Where(v => v.VendedorId == vendedorId && v.Cobrada && !yaLiquidadas.Contains(v.TenantId)).ToList();
            var comisionNueva = sinLiquidar.Sum(v => asignaciones[v.TenantId].Comision);
            var bonoNuevo = sinLiquidar.Sum(v => asignaciones[v.TenantId].Bono);

            return new LiquidacionVendedorDto(
                vendedorId, nombres.GetValueOrDefault(vendedorId, "Vendedor"),
                Ventas: propias.Sum(l => l.Items.Count) + sinLiquidar.Count,
                Comision: propias.Sum(l => l.TotalComision) + comisionNueva,
                Bono: propias.Sum(l => l.TotalBono) + bonoNuevo,
                VentasSinLiquidar: sinLiquidar.Count,
                ComisionSinLiquidar: comisionNueva,
                BonoSinLiquidar: bonoNuevo,
                EsperandoCobro: ventas.Count(v => v.VendedorId == vendedorId && v.EsperandoCobro),
                Liquidaciones: propias.Select(ADto).ToList());
        })
        .OrderByDescending(f => f.Comision + f.Bono).ThenBy(f => f.Nombre)
        .ToList();

        return new LiquidacionesMesDto(anio, mes, filas);
    }

    public async Task<PrevisualizacionLiquidacionDto> PrevisualizarAsync(Guid vendedorId, int anio, int mes, CancellationToken ct = default)
    {
        ValidarMesTerminado(anio, mes);
        var (items, esperando) = await ItemsPorLiquidarAsync(vendedorId, anio, mes, ct);
        var nombre = await NombreDeVendedorAsync(vendedorId, ct);
        return new PrevisualizacionLiquidacionDto(
            vendedorId, nombre, anio, mes, items.Select(AItemDto).ToList(),
            items.Sum(i => i.Comision), items.Sum(i => i.Bono), items.Sum(i => i.Comision + i.Bono), esperando);
    }

    public async Task<LiquidacionDto> LiquidarAsync(LiquidarRequest request, CancellationToken ct = default)
    {
        ValidarMesTerminado(request.Anio, request.Mes);
        var (items, _) = await ItemsPorLiquidarAsync(request.VendedorId, request.Anio, request.Mes, ct);
        if (items.Count == 0)
            throw new AppException("No hay ventas cobradas para liquidar en ese mes.");

        var vendedorNombre = await NombreDeVendedorAsync(request.VendedorId, ct);
        var adminNombre = await NombreDeAdminActualAsync(ct);
        var liquidacion = Liquidacion.Crear(
            request.VendedorId, vendedorNombre, request.Anio, request.Mes, _currentAdmin.AdminId, adminNombre, items);

        _db.Liquidaciones.Add(liquidacion);
        RegistrarMovimiento("liquidacion.cerrada", request.VendedorId, vendedorNombre, adminNombre,
            $"{Periodo(request.Anio, request.Mes)} · {liquidacion.Items.Count} ventas · {Dinero(liquidacion.Total)}");

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // El índice único por venta: alguien liquidó una de estas ventas en el medio.
            throw new AppException("Alguna de estas ventas ya fue liquidada. Actualizá la pantalla y probá de nuevo.");
        }

        return ADto(liquidacion);
    }

    public async Task<LiquidacionDto> PagarAsync(Guid id, PagarLiquidacionRequest request, CancellationToken ct = default)
    {
        var liquidacion = await _db.Liquidaciones.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new AppException("La liquidación no existe.");
        if (liquidacion.Estado == EstadoLiquidacion.Pagada)
            throw new AppException("Esta liquidación ya está pagada.");

        var hoy = DateOnly.FromDateTime(CalculadoraComisiones.AHoraArgentina(DateTime.UtcNow));
        var fecha = request.FechaPago ?? hoy;
        if (fecha > hoy)
            throw new AppException("La fecha de pago no puede ser mayor a hoy.");
        if (request.Nota is { Length: > 300 })
            throw new AppException("La nota no puede superar los 300 caracteres.");

        var adminNombre = await NombreDeAdminActualAsync(ct);
        liquidacion.MarcarPagada(fecha, request.Nota, adminNombre);
        RegistrarMovimiento("liquidacion.pagada", liquidacion.VendedorId, liquidacion.VendedorNombre, adminNombre,
            $"{Periodo(liquidacion.Anio, liquidacion.Mes)} · {Dinero(liquidacion.Total)}");

        await _db.SaveChangesAsync(ct);
        return ADto(liquidacion);
    }

    public async Task AnularAsync(Guid id, CancellationToken ct = default)
    {
        var liquidacion = await _db.Liquidaciones.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new AppException("La liquidación no existe.");
        if (liquidacion.Estado == EstadoLiquidacion.Pagada)
            throw new AppException("Una liquidación pagada no se puede anular.");

        var adminNombre = await NombreDeAdminActualAsync(ct);
        _db.Liquidaciones.Remove(liquidacion);
        RegistrarMovimiento("liquidacion.anulada", liquidacion.VendedorId, liquidacion.VendedorNombre, adminNombre,
            $"{Periodo(liquidacion.Anio, liquidacion.Mes)} · {Dinero(liquidacion.Total)}");
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LiquidacionDto>> ListarAsync(Guid? vendedorId, CancellationToken ct = default)
    {
        // Un Vendedor siempre ve lo suyo, aunque mande otro vendedorId.
        var filtro = _currentAdmin.EsOperador ? vendedorId : _currentAdmin.AdminId;

        var consulta = _db.Liquidaciones.AsNoTracking().Include(l => l.Items).AsQueryable();
        if (filtro is not null)
            consulta = consulta.Where(l => l.VendedorId == filtro);

        var lista = await consulta
            .OrderByDescending(l => l.Anio).ThenByDescending(l => l.Mes).ThenByDescending(l => l.FechaCierreUtc)
            .ToListAsync(ct);
        return lista.Select(ADto).ToList();
    }

    // ---- cálculo -------------------------------------------------------------------------------------

    // Ventas cobradas de un vendedor que todavía no están en ninguna liquidación, con su importe.
    private async Task<(List<LiquidacionItem> Items, int Esperando)> ItemsPorLiquidarAsync(
        Guid vendedorId, int anio, int mes, CancellationToken ct)
    {
        var ventas = await VentasDelMesAsync(anio, mes, vendedorId, ct);
        var cobradas = ventas.Where(v => v.Cobrada).ToList();
        var ids = cobradas.Select(v => v.TenantId).ToList();
        var yaLiquidadas = (await _db.LiquidacionItems.Where(i => ids.Contains(i.TenantId)).Select(i => i.TenantId).ToListAsync(ct))
            .ToHashSet();

        var asignaciones = CalculadoraComisiones.Asignar(
            cobradas.Select(v => new VentaConfirmada(v.TenantId, v.VendedorId, v.FechaAltaUtc)), _tarifa, yaLiquidadas);

        var items = cobradas
            .Where(v => !yaLiquidadas.Contains(v.TenantId))
            .OrderBy(v => asignaciones[v.TenantId].Orden)
            .Select(v =>
            {
                var a = asignaciones[v.TenantId];
                return LiquidacionItem.Crear(v.TenantId, v.Nombre, v.FechaAltaUtc, a.Orden, a.Comision, a.Bono);
            })
            .ToList();

        return (items, ventas.Count(v => v.EsperandoCobro));
    }

    private async Task<List<VentaDelMes>> VentasDelMesAsync(int anio, int mes, Guid? vendedorId, CancellationToken ct)
    {
        var desdeUtc = InicioDelDiaEnUtc(new DateOnly(anio, mes, 1));
        var hastaUtc = InicioDelDiaEnUtc(new DateOnly(anio, mes, 1).AddMonths(1));

        var consulta = _db.Tenants.IgnoreQueryFilters()
            .Where(t => t.VendedorId != null && t.FluxoSuscripcionId != null && t.FechaCreacion >= desdeUtc && t.FechaCreacion < hastaUtc);
        if (vendedorId is not null)
            consulta = consulta.Where(t => t.VendedorId == vendedorId);

        var negocios = await consulta.OrderBy(t => t.FechaCreacion).ToListAsync(ct);
        var estados = await _fluxo.ObtenerConfirmacionesAsync(negocios.Select(t => t.FluxoSuscripcionId!.Value), ct)
            ?? throw new AppException("No se pudo consultar a Fluxo para armar la liquidación. Probá de nuevo en un momento.");

        return negocios
            .Where(t => estados.ContainsKey(t.FluxoSuscripcionId!.Value))
            .Select(t =>
            {
                var e = estados[t.FluxoSuscripcionId!.Value];
                var cobrada = e.Confirmada && e.PrimerCobroAprobado;
                return new VentaDelMes(
                    t.Id, t.Nombre, t.VendedorId!.Value, t.FechaCreacion, cobrada,
                    EsperandoCobro: e.Confirmada && !e.PrimerCobroAprobado && e.Estado != "cancelled");
            })
            .ToList();
    }

    // ---- utilidades ----------------------------------------------------------------------------------

    private static void ValidarMesTerminado(int anio, int mes)
    {
        if (mes is < 1 or > 12 || anio < 2020)
            throw new AppException("El mes no es válido.");
        var hoy = CalculadoraComisiones.AHoraArgentina(DateTime.UtcNow);
        if ((anio, mes).CompareTo((hoy.Year, hoy.Month)) >= 0)
            throw new AppException("Solo se pueden liquidar meses terminados.");
    }

    private async Task<string> NombreDeVendedorAsync(Guid vendedorId, CancellationToken ct) =>
        await _db.AdminUsuarios.Where(a => a.Id == vendedorId).Select(a => a.Nombre).FirstOrDefaultAsync(ct)
        ?? throw new AppException("El vendedor no existe.");

    private async Task<string> NombreDeAdminActualAsync(CancellationToken ct) =>
        await _db.AdminUsuarios.Where(a => a.Id == _currentAdmin.AdminId).Select(a => a.Nombre).FirstOrDefaultAsync(ct) ?? "Admin";

    private void RegistrarMovimiento(string accion, Guid vendedorId, string vendedorNombre, string adminNombre, string detalle) =>
        _db.MovimientosAdmin.Add(MovimientoAdmin.Crear(
            _currentAdmin.AdminId, adminNombre, _currentAdmin.Rol, accion, "usuario", vendedorId, vendedorNombre, detalle));

    private static string Periodo(int anio, int mes) => $"{Meses[mes - 1]} {anio}";

    private static string Dinero(decimal valor) => "$" + Math.Round(valor).ToString("N0", new System.Globalization.CultureInfo("es-AR"));

    private static LiquidacionItemDto AItemDto(LiquidacionItem i) =>
        new(i.TenantId, i.TenantNombre, i.FechaAltaUtc, i.Orden, i.Comision, i.Bono);

    private static LiquidacionDto ADto(Liquidacion l) =>
        new(l.Id, l.VendedorId, l.VendedorNombre, l.Anio, l.Mes, l.Estado.ToString(), l.FechaCierreUtc, l.FechaPago, l.Nota,
            l.Items.Count, l.TotalComision, l.TotalBono, l.Total, l.Items.OrderBy(i => i.Orden).Select(AItemDto).ToList());

    // Medianoche de un día de Argentina, expresada en UTC (Argentina es UTC-3).
    private static DateTime InicioDelDiaEnUtc(DateOnly dia) =>
        DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue) - CalculadoraComisiones.OffsetArgentina, DateTimeKind.Utc);
}
