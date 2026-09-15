namespace GestorPOS.Application.Reportes;

public interface IReportePdfService
{
    Task<byte[]> GenerarVentasPdfAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);

    Task<byte[]> GenerarStockPdfAsync(string? busqueda, bool soloBajoStock, CancellationToken ct = default);
}
