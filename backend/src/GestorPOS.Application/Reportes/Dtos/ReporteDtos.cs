namespace GestorPOS.Application.Reportes.Dtos;

public record RankingProductoDto(Guid ProductoId, string ProductoNombre, int CantidadVendida, decimal TotalVendido);

public record RankingClienteDto(Guid ClienteId, string? Nombre, string Telefono, int CantidadCompras, decimal TotalGastado);

public record DeudaClienteDto(Guid ClienteId, string? Nombre, string Telefono, int CantidadVentasACuenta, decimal Saldo);

public record DeudasDto(decimal TotalAdeudado, int CantidadClientes, IReadOnlyList<DeudaClienteDto> Clientes);

public record GananciasDto(int CantidadVentas, decimal TotalVentas, decimal TotalCosto, decimal GananciaNeta);

public record DashboardDto(
    decimal VentasHoy,
    int CantidadVentasHoy,
    decimal GananciaHoy,
    decimal VentasMes,
    int ProductosBajoStockMinimo,
    RankingProductoDto? ProductoMasVendidoHoy);
