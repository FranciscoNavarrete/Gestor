namespace GestorPOS.Application.Reportes.Dtos;

public record RankingProductoDto(Guid ProductoId, string ProductoNombre, int CantidadVendida, decimal TotalVendido);

public record GananciasDto(int CantidadVentas, decimal TotalVentas, decimal TotalCosto, decimal GananciaNeta);

public record DashboardDto(
    decimal VentasHoy,
    int CantidadVentasHoy,
    decimal GananciaHoy,
    decimal VentasMes,
    int ProductosBajoStockMinimo,
    RankingProductoDto? ProductoMasVendidoHoy);
