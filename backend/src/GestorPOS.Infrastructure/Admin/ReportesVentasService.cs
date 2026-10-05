using System.Globalization;
using GestorPOS.Application.Admin;
using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestorPOS.Infrastructure.Admin;

public class ReportesVentasService : IReportesVentasService
{
    private const int MaximoDiasDeRango = 366;

    private readonly AppDbContext _db;
    private readonly ICurrentAdminContext _currentAdmin;
    private readonly IFluxoService _fluxo;
    private readonly TarifaComision _tarifa;

    public ReportesVentasService(
        AppDbContext db, ICurrentAdminContext currentAdmin, IFluxoService fluxo, IConfiguration config)
    {
        _db = db;
        _currentAdmin = currentAdmin;
        _fluxo = fluxo;
        _tarifa = LeerTarifa(config);
    }

    public async Task<ReporteVentasDto> ObtenerVentasAsync(
        DateOnly desde, DateOnly hasta, Guid? vendedorId, CancellationToken ct = default)
    {
        if (desde == default || hasta == default)
            throw new AppException("Elegí las fechas del reporte.");

        var hoy = DateOnly.FromDateTime(CalculadoraComisiones.AHoraArgentina(DateTime.UtcNow));
        if (hasta > hoy)
            throw new AppException("La fecha hasta no puede ser mayor a hoy.");
        if (desde > hasta)
            throw new AppException("La fecha desde no puede ser mayor que la fecha hasta.");
        if (hasta.DayNumber - desde.DayNumber > MaximoDiasDeRango)
            throw new AppException("El rango no puede superar un año.");

        // Un Vendedor siempre ve lo suyo, aunque mande otro vendedorId.
        Guid? filtroVendedor = _currentAdmin.EsOperador ? vendedorId : _currentAdmin.AdminId;

        // Para saber si una venta es de las "primeras 3" hay que ver todo el mes en que cae, no solo
        // el tramo pedido: se trae el mes completo y después se recorta a [desde, hasta].
        var desdeUtc = InicioDelDiaEnUtc(new DateOnly(desde.Year, desde.Month, 1));
        var hastaUtcExclusivo = InicioDelDiaEnUtc(new DateOnly(hasta.Year, hasta.Month, 1).AddMonths(1));

        var consulta = _db.Tenants.IgnoreQueryFilters()
            .Where(t => t.VendedorId != null && t.FluxoSuscripcionId != null
                        && t.FechaCreacion >= desdeUtc && t.FechaCreacion < hastaUtcExclusivo);
        if (filtroVendedor is not null)
            consulta = consulta.Where(t => t.VendedorId == filtroVendedor);

        var negocios = await consulta.OrderBy(t => t.FechaCreacion).ToListAsync(ct);

        var estados = await _fluxo.ObtenerConfirmacionesAsync(negocios.Select(t => t.FluxoSuscripcionId!.Value), ct);
        if (estados is null)
            throw new AppException("No se pudo consultar a Fluxo para armar el reporte. Probá de nuevo en un momento.");

        var conEstado = negocios
            .Where(t => estados.ContainsKey(t.FluxoSuscripcionId!.Value))
            .Select(t => (Negocio: t, Estado: estados[t.FluxoSuscripcionId!.Value]))
            .ToList();

        var comisiones = CalculadoraComisiones.Asignar(
            conEstado.Where(x => EstaCobrada(x.Estado))
                .Select(x => new VentaConfirmada(x.Negocio.Id, x.Negocio.VendedorId!.Value, x.Negocio.FechaCreacion)),
            _tarifa);

        var idsVendedores = conEstado.Select(x => x.Negocio.VendedorId!.Value).Distinct().ToList();
        var nombres = await _db.AdminUsuarios
            .Where(a => idsVendedores.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Nombre, ct);

        var items = conEstado
            .Where(x =>
            {
                var fecha = DateOnly.FromDateTime(CalculadoraComisiones.AHoraArgentina(x.Negocio.FechaCreacion));
                return fecha >= desde && fecha <= hasta;
            })
            .Select(x =>
            {
                var vendedor = x.Negocio.VendedorId!.Value;
                var cobrada = EstaCobrada(x.Estado);
                return new ReporteVentaItemDto(
                    x.Negocio.Id, x.Negocio.Nombre, x.Negocio.FechaCreacion,
                    vendedor, nombres.GetValueOrDefault(vendedor, "Vendedor"),
                    ClasificarEstado(x.Estado),
                    cobrada ? comisiones[x.Negocio.Id].Comision : null);
            })
            .OrderByDescending(i => i.FechaAlta)
            .ToList();

        var confirmadas = items.Where(i => i.Comision is not null).ToList();
        var primeras = confirmadas.Count(i => comisiones[i.TenantId].EsPrimera);

        var resumen = new ReporteResumenDto(
            Ventas: confirmadas.Count,
            Pendientes: items.Count(EsperaCobro),
            Comision: confirmadas.Sum(i => i.Comision!.Value),
            VentasPrimeras: primeras, TarifaPrimeras: _tarifa.Primeras,
            VentasSiguientes: confirmadas.Count - primeras, TarifaSiguientes: _tarifa.Siguientes);

        var vendedores = items
            .GroupBy(i => (i.VendedorId, i.VendedorNombre))
            .Select(g => new ReporteVendedorDto(
                g.Key.VendedorId, g.Key.VendedorNombre,
                Ventas: g.Count(i => i.Comision is not null),
                Pendientes: g.Count(EsperaCobro),
                Comision: g.Sum(i => i.Comision ?? 0)))
            .OrderByDescending(v => v.Ventas).ThenByDescending(v => v.Comision).ThenBy(v => v.Nombre)
            .ToList();

        return new ReporteVentasDto(desde, hasta, resumen, vendedores, items);
    }

    // La venta cuenta (y paga comisión) recién cuando Mercado Pago aprobó el primer cobro.
    private static bool EstaCobrada(FluxoEstadoSuscripcion estado) => estado.Confirmada && estado.PrimerCobroAprobado;

    private static bool EsperaCobro(ReporteVentaItemDto item) => item.Estado is "pendiente" or "esperando";

    private static string ClasificarEstado(FluxoEstadoSuscripcion estado) =>
        (estado.Confirmada, estado.PrimerCobroAprobado, estado.Estado) switch
        {
            (true, true, "cancelled") => "baja",
            (true, true, _) => "suscripto",
            (true, false, "cancelled") => "cancelada",
            (true, false, _) => "esperando",
            (false, _, "pending") => "pendiente",
            _ => "cancelada",
        };

    // Medianoche de un día de Argentina, expresada en UTC (Argentina es UTC-3).
    private static DateTime InicioDelDiaEnUtc(DateOnly dia) =>
        DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue) - CalculadoraComisiones.OffsetArgentina, DateTimeKind.Utc);

    private static TarifaComision LeerTarifa(IConfiguration config)
    {
        var predeterminada = TarifaComision.Predeterminada;
        return new TarifaComision(
            int.TryParse(config["Comisiones:CantidadPrimeras"], out var cantidad) ? cantidad : predeterminada.CantidadPrimeras,
            decimal.TryParse(config["Comisiones:TarifaPrimeras"], NumberStyles.Number, CultureInfo.InvariantCulture, out var primeras) ? primeras : predeterminada.Primeras,
            decimal.TryParse(config["Comisiones:TarifaSiguientes"], NumberStyles.Number, CultureInfo.InvariantCulture, out var siguientes) ? siguientes : predeterminada.Siguientes);
    }
}
