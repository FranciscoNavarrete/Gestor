namespace GestorPOS.Application.MovimientosStock.Dtos;

public record MovimientoStockDto(
    Guid Id,
    Guid ProductoId,
    string ProductoNombre,
    decimal Cantidad,
    decimal StockResultante,
    string Motivo,
    string UsuarioNombre,
    DateTime Fecha,
    string Unidad = "Unidad");
