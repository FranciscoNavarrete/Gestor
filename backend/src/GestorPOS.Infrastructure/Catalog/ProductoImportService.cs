using System.Globalization;
using ClosedXML.Excel;
using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Catalog;

public class ProductoImportService : IProductoImportService
{
    private static readonly string[] Encabezados =
        ["SKU", "Nombre", "Categoría", "Precio", "Costo", "Stock inicial", "Stock mínimo"];

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ProductoImportService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public byte[] GenerarPlantilla()
    {
        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Productos");

        for (var i = 0; i < Encabezados.Length; i++)
        {
            var celda = hoja.Cell(1, i + 1);
            celda.Value = Encabezados[i];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDE7F6");
        }

        // Fila de ejemplo para que quede claro el formato esperado.
        hoja.Cell(2, 1).Value = "SKU-001";
        hoja.Cell(2, 2).Value = "Producto de ejemplo";
        hoja.Cell(2, 3).Value = "Categoría ejemplo";
        hoja.Cell(2, 4).Value = 1000;
        hoja.Cell(2, 5).Value = 600;
        hoja.Cell(2, 6).Value = 10;
        hoja.Cell(2, 7).Value = 2;

        hoja.Columns().AdjustToContents();
        hoja.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ImportarProductosResultado> ImportarAsync(Stream archivoExcel, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(archivoExcel);
        var hoja = workbook.Worksheets.First();
        var filas = hoja.RowsUsed().Skip(1).ToList(); // saltea el encabezado

        var tenantId = _tenantContext.TenantId;

        var categoriasPorNombre = await _db.Categorias
            .ToDictionaryAsync(c => c.Nombre.Trim().ToLowerInvariant(), c => c, ct);
        var productosPorSku = await _db.Productos
            .ToDictionaryAsync(p => p.Sku, p => p, ct);

        var errores = new List<ImportarProductoErrorDto>();
        var creados = 0;
        var actualizados = 0;

        foreach (var fila in filas)
        {
            var numeroFila = fila.RowNumber();
            try
            {
                var sku = fila.Cell(1).GetString().Trim();
                var nombre = fila.Cell(2).GetString().Trim();
                var categoriaNombre = fila.Cell(3).GetString().Trim();
                var precio = LeerDecimal(fila.Cell(4), "Precio");
                var costo = LeerDecimal(fila.Cell(5), "Costo");
                var stockInicial = LeerEntero(fila.Cell(6), "Stock inicial");
                var stockMinimo = LeerEntero(fila.Cell(7), "Stock mínimo");

                if (string.IsNullOrWhiteSpace(sku))
                    throw new FormatException("Falta el SKU.");
                if (string.IsNullOrWhiteSpace(nombre))
                    throw new FormatException("Falta el nombre.");

                Categoria? categoria = null;
                if (!string.IsNullOrWhiteSpace(categoriaNombre))
                {
                    var clave = categoriaNombre.ToLowerInvariant();
                    if (!categoriasPorNombre.TryGetValue(clave, out categoria))
                    {
                        categoria = Categoria.Crear(tenantId, categoriaNombre);
                        _db.Categorias.Add(categoria);
                        categoriasPorNombre[clave] = categoria;
                    }
                }

                if (productosPorSku.TryGetValue(sku, out var existente))
                {
                    existente.Editar(nombre, categoria?.Id, precio, costo, stockMinimo);
                    actualizados++;
                }
                else
                {
                    var nuevo = Producto.Crear(tenantId, sku, nombre, categoria?.Id, precio, costo, stockInicial, stockMinimo);
                    _db.Productos.Add(nuevo);
                    productosPorSku[sku] = nuevo;
                    creados++;
                }
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                errores.Add(new ImportarProductoErrorDto(numeroFila, ex.Message));
            }
        }

        await _db.SaveChangesAsync(ct);

        return new ImportarProductosResultado(filas.Count, creados, actualizados, errores);
    }

    private static decimal LeerDecimal(IXLCell celda, string nombreColumna)
    {
        var texto = celda.GetString().Trim();
        if (string.IsNullOrWhiteSpace(texto))
            throw new FormatException($"Falta el valor de '{nombreColumna}'.");

        if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariante))
            return invariante;
        if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.GetCultureInfo("es-AR"), out var esAr))
            return esAr;

        throw new FormatException($"'{nombreColumna}' no es un número válido: '{texto}'.");
    }

    private static int LeerEntero(IXLCell celda, string nombreColumna)
    {
        var texto = celda.GetString().Trim();
        if (string.IsNullOrWhiteSpace(texto))
            return 0;

        if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor))
            return valor;

        // Por si viene como "10.0" en vez de entero.
        var comoDecimal = LeerDecimal(celda, nombreColumna);
        return (int)decimal.Round(comoDecimal);
    }
}
