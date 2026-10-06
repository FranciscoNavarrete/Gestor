using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

/// <summary>Efectivo que los vendedores reciben de los clientes (el primer pago en efectivo del alta): queda a su
/// cargo hasta que lo entregan al operador o se descuenta de su liquidación.</summary>
public interface IEfectivoService
{
    /// <summary>Un Operador ve a todos los vendedores; un Vendedor, solo lo suyo.</summary>
    Task<EfectivoResumenDto> ResumenAsync(CancellationToken ct = default);

    /// <summary>Cobros, entregas y compensaciones de un vendedor, del más nuevo al más viejo.</summary>
    Task<IReadOnlyList<EfectivoMovimientoDto>> MovimientosAsync(Guid vendedorId, CancellationToken ct = default);

    /// <summary>El operador registra que un vendedor le entregó efectivo.</summary>
    Task<EfectivoVendedorDto> RegistrarEntregaAsync(RegistrarEntregaRequest request, CancellationToken ct = default);

    Task AnularEntregaAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lo que el vendedor tiene hoy a su cargo: cobrado menos entregado y compensado.</summary>
    Task<decimal> EnPoderAsync(Guid vendedorId, CancellationToken ct = default);
}
