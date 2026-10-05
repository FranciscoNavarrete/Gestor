using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

public interface ILiquidacionesService
{
    /// <summary>Cierre de un mes ya terminado, por vendedor (solo Operador).</summary>
    Task<LiquidacionesMesDto> ResumenAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>Lo que se liquidaría hoy para un vendedor en un mes, sin guardar nada (solo Operador).</summary>
    Task<PrevisualizacionLiquidacionDto> PrevisualizarAsync(Guid vendedorId, int anio, int mes, CancellationToken ct = default);

    Task<LiquidacionDto> LiquidarAsync(LiquidarRequest request, CancellationToken ct = default);
    Task<LiquidacionDto> PagarAsync(Guid id, PagarLiquidacionRequest request, CancellationToken ct = default);
    Task AnularAsync(Guid id, CancellationToken ct = default);

    /// <summary>Liquidaciones, de la más nueva a la más vieja. Un Vendedor solo ve las suyas.</summary>
    Task<IReadOnlyList<LiquidacionDto>> ListarAsync(Guid? vendedorId, CancellationToken ct = default);
}
