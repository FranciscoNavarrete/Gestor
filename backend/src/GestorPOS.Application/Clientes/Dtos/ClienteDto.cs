namespace GestorPOS.Application.Clientes.Dtos;

public record ClienteDto(
    Guid Id, string Telefono, string? Nombre, DateTime FechaAlta, int CantidadCompras, decimal SaldoCuentaCorriente);

public record ClienteDetalleDto(
    Guid Id,
    string Telefono,
    string? Nombre,
    DateTime FechaAlta,
    int CantidadCompras,
    decimal TotalGastado,
    DateTime? UltimaCompra,
    decimal SaldoCuentaCorriente);

public record ClienteVentaDto(Guid Id, DateTime Fecha, decimal Total, string MedioPago);

public record EditarClienteRequest(string? Nombre, string Telefono);

/// <summary>Un movimiento de cuenta corriente: "Fiado" (monto positivo, suma deuda) o
/// "Pago" (monto negativo, resta deuda). MedioPago solo viene cargado en los pagos.</summary>
public record MovimientoCuentaDto(DateTime Fecha, string Tipo, decimal Monto, string? MedioPago);

public record RegistrarPagoCuentaRequest(decimal Monto, string MedioPago);
