using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Infrastructure.Admin;
using GestorPOS.Application.Admin;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorPOS.Infrastructure.Suscripcion;

/// <summary>Desactiva los negocios cuya suscripción fue cancelada (la cancelación la hace el operador en
/// Fluxo). Si el cliente ya había pagado, el acceso sigue vigente hasta la fecha de su próximo cobro; si
/// nunca llegó a pagar, se corta enseguida. Pausada o suspendida no corta: Fluxo cancela solo las que
/// acumulan cobros fallidos, y recién ahí se corta. Usa el mismo mecanismo que desactivar a mano (cortar el
/// acceso de los usuarios y liberar sus emails).</summary>
public class CorteAccesoPorSuscripcionService
{
    private readonly AppDbContext _db;
    private readonly IFluxoService _fluxo;
    private readonly ILogger<CorteAccesoPorSuscripcionService> _logger;

    public CorteAccesoPorSuscripcionService(AppDbContext db, IFluxoService fluxo, ILogger<CorteAccesoPorSuscripcionService> logger)
    {
        _db = db;
        _fluxo = fluxo;
        _logger = logger;
    }

    /// <summary>Devuelve cuántos negocios desactivó. Si Fluxo no responde no toca nada.</summary>
    public async Task<int> EjecutarAsync(CancellationToken ct = default)
    {
        // Cruza todos los negocios a propósito: es un proceso de fondo, no hay un tenant actual.
        var activos = await _db.Tenants.IgnoreQueryFilters()
            .Where(t => t.Activo && t.FluxoSuscripcionId != null)
            .ToListAsync(ct);
        if (activos.Count == 0) return 0;

        var estados = await _fluxo.ObtenerConfirmacionesAsync(activos.Select(t => t.FluxoSuscripcionId!.Value), ct);
        if (estados is null)
        {
            _logger.LogWarning("Corte por suscripción: Fluxo no respondió, no se toca ningún negocio.");
            return 0;
        }

        var pagosManuales = await VentaCobrada.ConPagoManualConfirmadoAsync(_db, activos.Select(t => t.Id), ct);
        var ahora = DateTime.UtcNow;
        var desactivados = 0;

        foreach (var tenant in activos)
        {
            if (!estados.TryGetValue(tenant.FluxoSuscripcionId!.Value, out var estado) || estado.Estado != "cancelled")
                continue;

            if (AccesoVigenteHasta(estado, pagosManuales.Contains(tenant.Id)) is { } hasta && hasta > ahora)
                continue;

            await DesactivarAsync(tenant, ct);
            desactivados++;
        }

        if (desactivados > 0)
            await _db.SaveChangesAsync(ct);

        return desactivados;
    }

    /// <summary>Activa los negocios que esperaban su activación y ya cumplen todo: el primer pago confirmado y la
    /// suscripción autorizada (p. ej. el cliente recién abrió el link). Devuelve cuántos activó.</summary>
    public async Task<int> ActivarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _db.Tenants.IgnoreQueryFilters()
            .Where(t => t.PendienteActivacion && t.FluxoSuscripcionId != null)
            .ToListAsync(ct);
        if (pendientes.Count == 0) return 0;

        var estados = await _fluxo.ObtenerConfirmacionesAsync(pendientes.Select(t => t.FluxoSuscripcionId!.Value), ct);
        if (estados is null) return 0;

        var confirmados = await VentaCobrada.ConPagoManualConfirmadoAsync(_db, pendientes.Select(t => t.Id), ct);
        var activados = 0;

        foreach (var tenant in pendientes)
        {
            if (!confirmados.Contains(tenant.Id)) continue;
            if (!estados.TryGetValue(tenant.FluxoSuscripcionId!.Value, out var estado) || !estado.Confirmada) continue;

            tenant.ConfirmarActivacion();
            _db.MovimientosAdmin.Add(MovimientoAdmin.Crear(
                Guid.Empty, "Sistema", "Sistema", "negocio.activado", "negocio", tenant.Id, tenant.Nombre,
                "Primer pago confirmado y suscripción autorizada"));
            _logger.LogInformation("Negocio {Nombre} activado: pago confirmado y suscripción autorizada.", tenant.Nombre);
            activados++;
        }

        if (activados > 0) await _db.SaveChangesAsync(ct);
        return activados;
    }

    /// <summary>Hasta cuándo sigue vigente el acceso de una suscripción cancelada; null si ya no lo está
    /// (nunca pagó, o ya pasó la fecha de su próximo cobro).</summary>
    public static DateTime? AccesoVigenteHasta(FluxoEstadoSuscripcion estado, bool pagoManualConfirmado = false) =>
        estado.Estado == "cancelled" && (estado.PrimerCobroAprobado || pagoManualConfirmado) ? estado.ProximoCobro : null;

    private async Task DesactivarAsync(Tenant tenant, CancellationToken ct)
    {
        tenant.Desactivar();

        var usuarios = await _db.Usuarios.IgnoreQueryFilters().Where(u => u.TenantId == tenant.Id).ToListAsync(ct);
        foreach (var usuario in usuarios)
        {
            usuario.Desactivar();
            usuario.LiberarEmail();
        }

        _db.MovimientosAdmin.Add(MovimientoAdmin.Crear(
            Guid.Empty, "Sistema", "Sistema", "negocio.desactivado", "negocio", tenant.Id, tenant.Nombre,
            "Suscripción cancelada (corte automático)"));

        _logger.LogInformation("Negocio {Nombre} desactivado: su suscripción está cancelada.", tenant.Nombre);
    }
}
