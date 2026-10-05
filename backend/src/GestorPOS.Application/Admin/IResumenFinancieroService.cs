using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

public interface IResumenFinancieroService
{
    /// <summary>Resumen de ingresos, clientes y cobros en riesgo, armado con las suscripciones de Fluxo.
    /// Tira AppException si Fluxo no responde (mejor avisar que mostrar números falsos).</summary>
    Task<ResumenFinancieroDto> ObtenerAsync(CancellationToken ct = default);
}
