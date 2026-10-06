using GestorPOS.Application.Admin.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Suscripcion;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Suscripcion;

public class MiSuscripcionService : IMiSuscripcionService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IFluxoService _fluxo;

    public MiSuscripcionService(AppDbContext db, ITenantContext tenant, IFluxoService fluxo)
    {
        _db = db;
        _tenant = tenant;
        _fluxo = fluxo;
    }

    public async Task<MiSuscripcionDto> ObtenerAsync(CancellationToken ct = default)
    {
        var suscripcionId = await ObtenerSuscripcionIdAsync(ct);

        var cobros = await _fluxo.ObtenerCobrosAsync(suscripcionId, ct);
        var estados = await _fluxo.ObtenerConfirmacionesAsync([suscripcionId], ct);
        var estado = estados?.GetValueOrDefault(suscripcionId);

        var email = await _db.Usuarios.Where(u => u.Id == _tenant.UsuarioId).Select(u => u.Email).FirstOrDefaultAsync(ct) ?? string.Empty;

        // El primer pago hecho por fuera de Mercado Pago figura como un pago más.
        var pago = await _db.PagosManuales.AsNoTracking()
            .Where(p => p.TenantId == _tenant.TenantId && p.Estado == GestorPOS.Domain.Entities.EstadoPagoManual.Confirmado)
            .OrderBy(p => p.FechaRegistroUtc)
            .FirstOrDefaultAsync(ct);
        var cobrosMp = cobros.Cobros
            .Select(c => new CobroNegocioDto(c.Fecha, c.Monto, c.Estado, c.Motivo, c.Intento, c.ProximoReintento, c.EsPrimerCobro))
            .ToList();
        if (pago is not null)
        {
            var fecha = pago.FechaRecepcion?.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(12))).AddHours(3)
                        ?? pago.FechaConfirmacionUtc;
            cobrosMp.Add(new CobroNegocioDto(
                fecha is null ? null : DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc), pago.Monto, "aprobado",
                pago.Metodo.ToString(), 0, null, true));
        }

        return new MiSuscripcionDto(
            Estado: cobros.Estado,
            MontoMensual: cobros.MontoMensual,
            ProximoCobro: cobros.ProximoCobro,
            ProximoMonto: cobros.ProximoMonto,
            TarjetaEditable: cobros.TarjetaEditable,
            CobroRechazado: estado?.CobroRechazado == true,
            MotivoRechazo: estado?.MotivoRechazo,
            ProximoReintento: estado?.ProximoReintento,
            EmailPagador: email,
            Cobros: cobrosMp.OrderByDescending(c => c.Fecha ?? DateTime.MinValue).ToList(),
            PrimerPagoManual: pago is not null,
            PlanNombre: cobros.PlanNombre, MontoNormal: cobros.MontoNormal, Promo: cobros.Promo);
    }

    public async Task CambiarTarjetaAsync(CambiarTarjetaRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CardToken))
            throw new AppException("Faltan los datos de la tarjeta.");

        var suscripcionId = await ObtenerSuscripcionIdAsync(ct);
        await _fluxo.CambiarTarjetaAsync(suscripcionId, request.CardToken.Trim(), ct);
    }

    private async Task<int> ObtenerSuscripcionIdAsync(CancellationToken ct)
    {
        // Siempre la suscripción del negocio de la sesión: nunca se recibe un id desde afuera.
        var id = await _db.Tenants.IgnoreQueryFilters()
            .Where(t => t.Id == _tenant.TenantId)
            .Select(t => t.FluxoSuscripcionId)
            .FirstOrDefaultAsync(ct);
        return id ?? throw new AppException("Tu negocio todavía no tiene una suscripción.");
    }
}
