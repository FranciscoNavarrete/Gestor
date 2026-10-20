namespace GestorPOS.Application.Catalog.Dtos;

/// <summary>Presentación de un producto ("Bolsa 15 kg"). CantidadBase está en la unidad del producto.</summary>
public record PresentacionDto(Guid Id, Guid ProductoId, string Nombre, decimal CantidadBase, decimal Precio, bool Activo);

public record CrearPresentacionRequest(string Nombre, decimal CantidadBase, decimal Precio);

public record EditarPresentacionRequest(string Nombre, decimal CantidadBase, decimal Precio);

/// <summary>Unidad es "Unidad", "Kilo", "Litro" o "Metro". Presentaciones trae solo las activas.
/// En un producto por unidad sin presentaciones (lo de siempre) Unidad es "Unidad" y Presentaciones viene vacía.</summary>
public record ProductoDto(
    Guid Id,
    string Sku,
    string Nombre,
    Guid? CategoriaId,
    string? CategoriaNombre,
    Guid? MarcaId,
    string? MarcaNombre,
    decimal Precio,
    decimal Costo,
    decimal StockActual,
    decimal StockMinimo,
    bool EnStockMinimo,
    bool Activo,
    string Unidad,
    IReadOnlyList<PresentacionDto> Presentaciones);

/// <summary>Unidad (opcional, "Unidad" por defecto) y Presentaciones (opcional) solo se pueden usar si el
/// negocio tiene prendida la función "Medidas y presentaciones".</summary>
public record CrearProductoRequest(
    string Sku,
    string Nombre,
    Guid? CategoriaId,
    decimal Precio,
    decimal Costo,
    decimal StockActual,
    decimal StockMinimo,
    string? Unidad = null,
    IReadOnlyList<CrearPresentacionRequest>? Presentaciones = null,
    Guid? MarcaId = null);

/// <summary>MarcaId vacío saca la marca del producto (igual que CategoriaId).</summary>
public record EditarProductoRequest(
    string Nombre,
    Guid? CategoriaId,
    decimal Precio,
    decimal Costo,
    decimal StockMinimo,
    Guid? MarcaId = null);

/// <summary>Sube o baja un porcentaje el precio de los productos activos y el de sus presentaciones, opcionalmente
/// solo los de una categoría y/o una marca. Ej: 10 = +10%, -5 = -5%.</summary>
public record ActualizarPreciosMasivoRequest(decimal Porcentaje, Guid? CategoriaId, Guid? MarcaId = null);

/// <summary>Cuántos productos y presentaciones activos toca (o tocaría) un aumento masivo.</summary>
public record AlcancePreciosMasivoDto(int Productos, int Presentaciones);

/// <summary>Cantidad positiva para reponer stock, negativa para descontarlo, en la unidad del producto.
/// Motivo es texto libre (el frontend ofrece Reposición/Merma/Corrección/Otro como sugerencias rápidas,
/// no es un enum fijo).</summary>
public record AjustarStockRequest(decimal Cantidad, string Motivo);

public record ImportarProductoErrorDto(int Fila, string Mensaje);

public record ImportarProductosResultado(
    int TotalFilas,
    int Creados,
    int Actualizados,
    IReadOnlyList<ImportarProductoErrorDto> Errores);
