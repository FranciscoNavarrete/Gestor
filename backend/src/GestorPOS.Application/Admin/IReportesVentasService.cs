using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

/// <summary>Reportes de ventas para el panel interno. Un Vendedor ve solo las suyas; el Operador ve
/// las de todos y puede filtrar por vendedor.</summary>
public interface IReportesVentasService
{
    /// <summary>Tira <see cref="GestorPOS.Application.Common.Exceptions.AppException"/> si las fechas no son
    /// válidas (hasta mayor que hoy, desde mayor que hasta) o si Fluxo no responde.</summary>
    Task<ReporteVentasDto> ObtenerVentasAsync(
        DateOnly desde, DateOnly hasta, Guid? vendedorId, CancellationToken ct = default);
}
