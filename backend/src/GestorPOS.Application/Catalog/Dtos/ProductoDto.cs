namespace GestorPOS.Application.Catalog.Dtos;

public record ProductoDto(
    Guid Id,
    string Sku,
    string Nombre,
    Guid? CategoriaId,
    string? CategoriaNombre,
    decimal Precio,
    decimal Costo,
    int StockActual,
    int StockMinimo,
    bool EnStockMinimo,
    bool Activo);

public record CrearProductoRequest(
    string Sku,
    string Nombre,
    Guid? CategoriaId,
    decimal Precio,
    decimal Costo,
    int StockActual,
    int StockMinimo);

public record EditarProductoRequest(
    string Nombre,
    Guid? CategoriaId,
    decimal Precio,
    decimal Costo,
    int StockMinimo);

/// <summary>Sube o baja el precio de todos los productos activos (opcionalmente filtrados por categoría)
/// un porcentaje dado. Ej: 10 = +10%, -5 = -5%.</summary>
public record ActualizarPreciosMasivoRequest(decimal Porcentaje, Guid? CategoriaId);

/// <summary>Cantidad positiva para reponer stock, negativa para descontarlo. Motivo es texto libre
/// (el frontend ofrece Reposición/Merma/Corrección/Otro como sugerencias rápidas, no es un enum fijo).</summary>
public record AjustarStockRequest(int Cantidad, string Motivo);

public record ImportarProductoErrorDto(int Fila, string Mensaje);

public record ImportarProductosResultado(
    int TotalFilas,
    int Creados,
    int Actualizados,
    IReadOnlyList<ImportarProductoErrorDto> Errores);
