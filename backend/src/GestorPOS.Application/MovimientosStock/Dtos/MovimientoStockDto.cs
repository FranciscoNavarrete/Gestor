namespace GestorPOS.Application.MovimientosStock.Dtos;

public record MovimientoStockDto(
    Guid Id,
    Guid ProductoId,
    string ProductoNombre,
    int Cantidad,
    int StockResultante,
    string Motivo,
    string UsuarioNombre,
    DateTime Fecha);
