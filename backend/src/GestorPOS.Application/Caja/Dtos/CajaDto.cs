namespace GestorPOS.Application.Caja.Dtos;

public record CajaDto(
    Guid Id,
    DateTime FechaApertura,
    decimal MontoApertura,
    bool Abierta,
    decimal? MontoCierreEsperado,
    decimal? MontoCierreReal,
    decimal? Diferencia,
    DateTime? FechaCierre,
    IReadOnlyList<VentaPorMedioPagoDto> VentasPorMedioPago,
    IReadOnlyList<MovimientoCajaDto> Movimientos,
    decimal NetoMovimientos);

public record VentaPorMedioPagoDto(string MedioPago, decimal Total);

public record MovimientoCajaDto(Guid Id, string Tipo, decimal Monto, string Motivo, DateTime Fecha);

public record AbrirCajaRequest(decimal MontoApertura);

public record CerrarCajaRequest(decimal MontoCierreReal);

public record CrearMovimientoCajaRequest(string Tipo, decimal Monto, string Motivo);
