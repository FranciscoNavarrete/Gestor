namespace GestorPOS.Application.Caja.Dtos;

public record CajaDto(
    Guid Id,
    DateTime FechaApertura,
    decimal MontoApertura,
    bool Abierta,
    decimal? MontoCierreEsperado,
    decimal? MontoCierreReal,
    decimal? Diferencia,
    DateTime? FechaCierre);

public record AbrirCajaRequest(decimal MontoApertura);

public record CerrarCajaRequest(decimal MontoCierreReal);
