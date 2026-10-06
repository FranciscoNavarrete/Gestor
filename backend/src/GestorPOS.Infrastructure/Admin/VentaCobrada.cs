using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Admin;

/// <summary>Cuándo una venta cuenta como cobrada (para comisiones, bono, liquidaciones y el resumen): cuando
/// Mercado Pago aprobó el primer cobro, o cuando el primer pago se recibió por fuera de Mercado Pago
/// (efectivo del vendedor o transferencia confirmada) y la suscripción mensual quedó autorizada.</summary>
public static class VentaCobrada
{
    /// <param name="ventaPrevia">El negocio ya se había contado como venta cobrada con una suscripción anterior (ver
    /// <see cref="Tenant.VentaPreviaCobrada"/>): sigue contando mientras la suscripción nueva espera su primer cobro.</param>
    public static bool Es(FluxoEstadoSuscripcion estado, bool pagoManualConfirmado, bool ventaPrevia = false) =>
        ventaPrevia || (estado.Confirmada && (estado.PrimerCobroAprobado || pagoManualConfirmado));

    /// <summary>Negocios (de los pedidos) que ya habían contado como venta cobrada antes de una suscripción nueva.</summary>
    public static async Task<HashSet<Guid>> PreviamenteCobradasAsync(
        AppDbContext db, IEnumerable<Guid> tenantIds, CancellationToken ct)
    {
        var ids = tenantIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        var previas = await db.Tenants.IgnoreQueryFilters()
            .Where(t => ids.Contains(t.Id) && t.VentaPreviaCobrada)
            .Select(t => t.Id)
            .ToListAsync(ct);
        return previas.ToHashSet();
    }

    /// <summary>Negocios (de los pedidos) cuyo primer pago manual ya está confirmado.</summary>
    public static async Task<HashSet<Guid>> ConPagoManualConfirmadoAsync(
        AppDbContext db, IEnumerable<Guid> tenantIds, CancellationToken ct)
    {
        var ids = tenantIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        var confirmados = await db.PagosManuales
            .Where(p => ids.Contains(p.TenantId) && p.Estado == EstadoPagoManual.Confirmado)
            .Select(p => p.TenantId)
            .ToListAsync(ct);
        return confirmados.ToHashSet();
    }
}
