using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Admin;

public class ResumenFinancieroService : IResumenFinancieroService
{
    private const int MesesDeAltas = 6;
    private const int DiasACobrar = 30;

    private readonly AppDbContext _db;
    private readonly IFluxoService _fluxo;

    public ResumenFinancieroService(AppDbContext db, IFluxoService fluxo)
    {
        _db = db;
        _fluxo = fluxo;
    }

    public async Task<ResumenFinancieroDto> ObtenerAsync(CancellationToken ct = default)
    {
        // Cruza todos los negocios a propósito: es el resumen de la plataforma, para el operador.
        var negocios = await _db.Tenants.IgnoreQueryFilters()
            .Where(t => t.FluxoSuscripcionId != null)
            .Select(t => new { t.Id, t.Nombre, SuscripcionId = t.FluxoSuscripcionId!.Value })
            .ToListAsync(ct);

        var estados = await _fluxo.ObtenerConfirmacionesAsync(negocios.Select(n => n.SuscripcionId), ct)
            ?? throw new AppException("No se pudo consultar a Fluxo para armar el resumen. Probá de nuevo en un momento.");

        var conPagoManual = await VentaCobrada.ConPagoManualConfirmadoAsync(_db, negocios.Select(n => n.Id), ct);
        var filas = negocios
            .Where(n => estados.ContainsKey(n.SuscripcionId))
            .Select(n => (n.Id, n.Nombre, E: estados[n.SuscripcionId], Pagado: VentaCobrada.Es(estados[n.SuscripcionId], conPagoManual.Contains(n.Id))))
            .ToList();

        var ahoraUtc = DateTime.UtcNow;
        var ahoraAr = CalculadoraComisiones.AHoraArgentina(ahoraUtc);
        bool EnMes(DateTime? fechaUtc, int anio, int mes)
        {
            if (fechaUtc is null) return false;
            var local = CalculadoraComisiones.AHoraArgentina(fechaUtc.Value);
            return local.Year == anio && local.Month == mes;
        }

        // Activo = autorizada y con el primer cobro ya aprobado. Un cobro mensual rechazado sigue siendo cliente activo.
        var activos = filas.Where(f => f.E.Estado == "authorized" && f.Pagado).ToList();
        var esperando = filas.Where(f => f.E.Estado == "authorized" && !f.Pagado && !f.E.CobroRechazado).ToList();
        var rechazados = filas.Where(f => f.E.Estado == "authorized" && f.E.CobroRechazado).ToList();
        var detenidos = filas.Where(f => f.E.Estado is "paused" or "suspended").ToList();

        var limite = ahoraUtc.AddDays(DiasACobrar);
        var aCobrar = activos
            .Where(f => !f.E.CobroRechazado && f.E.ProximoCobro is { } p && p >= ahoraUtc && p <= limite)
            .Sum(f => f.E.MontoProximoCobro);

        var altasMes = filas.Count(f => f.Pagado && EnMes(f.E.FechaInicio, ahoraAr.Year, ahoraAr.Month));
        var bajasMes = filas.Count(f => f.Pagado && f.E.Estado == "cancelled"
                                        && EnMes(f.E.FechaCancelacion, ahoraAr.Year, ahoraAr.Month));
        var baseBajas = activos.Count + bajasMes;
        var porcentajeBajas = baseBajas == 0 ? 0m : Math.Round(100m * bajasMes / baseBajas, 1);

        var enRiesgo = rechazados
            .Select(f => new ClienteEnRiesgoDto(
                f.Id, f.Nombre, f.Pagado ? "rechazado" : "primer-cobro",
                f.E.MotivoRechazo, f.E.MontoProximoCobro, f.E.ProximoReintento))
            .Concat(detenidos.Select(f => new ClienteEnRiesgoDto(
                f.Id, f.Nombre, f.E.Estado == "paused" ? "pausado" : "suspendido", null, f.E.MontoMensual, null)))
            .OrderBy(c => c.Tipo == "primer-cobro" ? 0 : c.Tipo == "rechazado" ? 1 : 2)
            .ThenBy(c => c.Nombre)
            .ToList();

        var altasPorMes = Enumerable.Range(0, MesesDeAltas)
            .Select(i => new DateTime(ahoraAr.Year, ahoraAr.Month, 1).AddMonths(-(MesesDeAltas - 1 - i)))
            .Select(m => new AltasMesDto(m.Year, m.Month,
                filas.Count(f => f.Pagado && EnMes(f.E.FechaInicio, m.Year, m.Month))))
            .ToList();

        return new ResumenFinancieroDto(
            IngresoMensual: activos.Sum(f => f.E.MontoMensual),
            ClientesActivos: activos.Count,
            EsperandoPrimerCobro: esperando.Count,
            PorCobrarPrimerosCobros: esperando.Sum(f => f.E.MontoProximoCobro),
            AltasMes: altasMes,
            BajasMes: bajasMes,
            PorcentajeBajas: porcentajeBajas,
            ACobrar30Dias: aCobrar,
            CobrosRechazados: rechazados.Count,
            MontoRechazado: rechazados.Sum(f => f.E.MontoProximoCobro),
            PausadosOSuspendidos: detenidos.Count,
            EnRiesgo: enRiesgo,
            AltasPorMes: altasPorMes);
    }
}
