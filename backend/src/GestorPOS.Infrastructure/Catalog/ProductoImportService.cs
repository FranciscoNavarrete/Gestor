using System.Globalization;
using ClosedXML.Excel;
using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Common;
using GestorPOS.Domain.Entities;
using GestorPOS.Domain.Enums;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Catalog;

public class ProductoImportService : IProductoImportService
{
    private static readonly string[] Encabezados =
        ["SKU", "Nombre", "Categoría", "Precio", "Costo", "Stock inicial", "Stock mínimo", "Marca"];

    /// <summary>La columna "Marca" (8ª) es opcional y la tienen todos los negocios; las planillas viejas de 7 columnas
    /// siguen funcionando. Con la función "Medidas y presentaciones" la planilla suma una novena, "Unidad".</summary>
    private const int ColumnaMarca = 8;
    private const string EncabezadoUnidad = "Unidad";
    private const int ColumnaUnidad = 9;

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

        var conMedidas = _tenantContext.TieneFeature(MedidasFeature.Clave);
        var encabezados = conMedidas ? [.. Encabezados, EncabezadoUnidad] : Encabezados;

        for (var i = 0; i < encabezados.Length; i++)
        {
            var celda = hoja.Cell(1, i + 1);
            celda.Value = encabezados[i];
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
        hoja.Cell(2, ColumnaMarca).Value = "Marca ejemplo";
        if (conMedidas)
            hoja.Cell(2, ColumnaUnidad).Value = "u";

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
        var conMedidas = _tenantContext.TieneFeature(MedidasFeature.Clave);

        var categoriasPorNombre = await _db.Categorias
            .ToDictionaryAsync(c => c.Nombre.Trim().ToLowerInvariant(), c => c, ct);
        var marcasPorNombre = await _db.Marcas
            .ToDictionaryAsync(m => m.Nombre.Trim().ToLowerInvariant(), m => m, ct);
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
                var marcaNombre = fila.Cell(ColumnaMarca).GetString().Trim();
                var unidadPedida = LeerUnidad(fila.Cell(ColumnaUnidad));

                if (string.IsNullOrWhiteSpace(sku))
                    throw new FormatException("Falta el SKU.");
                if (string.IsNullOrWhiteSpace(nombre))
                    throw new FormatException("Falta el nombre.");

                productosPorSku.TryGetValue(sku, out var existente);
                if (unidadPedida is { } pedida && pedida != UnidadMedida.Unidad && !conMedidas)
                    throw new FormatException("La función Medidas y presentaciones no está activada para tu negocio.");
                if (existente is not null && unidadPedida is { } otra && otra != existente.Unidad)
                    throw new FormatException("No se puede cambiar la unidad de un producto que ya existe.");

                var unidad = existente?.Unidad ?? unidadPedida ?? UnidadMedida.Unidad;
                var stockInicial = LeerCantidad(fila.Cell(6), "Stock inicial", unidad);
                var stockMinimo = LeerCantidad(fila.Cell(7), "Stock mínimo", unidad);

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

                // Como con las categorías, una marca nueva se crea sola; si la celda viene vacía el producto
                // existente conserva la que ya tenía.
                Marca? marca = null;
                if (!string.IsNullOrWhiteSpace(marcaNombre))
                {
                    var claveMarca = marcaNombre.ToLowerInvariant();
                    if (!marcasPorNombre.TryGetValue(claveMarca, out marca))
                    {
                        marca = Marca.Crear(tenantId, marcaNombre);
                        _db.Marcas.Add(marca);
                        marcasPorNombre[claveMarca] = marca;
                    }
                }

                if (existente is not null)
                {
                    existente.Editar(nombre, categoria?.Id, precio, costo, stockMinimo, marca?.Id ?? existente.MarcaId);
                    actualizados++;
                }
                else
                {
                    var nuevo = Producto.Crear(tenantId, sku, nombre, categoria?.Id, precio, costo, stockInicial, stockMinimo, unidad, marca?.Id);
                    _db.Productos.Add(nuevo);
                    productosPorSku[sku] = nuevo;
                    creados++;
                }
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                // ArgumentException agrega " (Parameter 'x')" al mensaje: es ruido para quien carga el Excel.
                var mensaje = System.Text.RegularExpressions.Regex.Replace(ex.Message, @"\s*\(Parameter '[^']*'\)\s*$", "");
                errores.Add(new ImportarProductoErrorDto(numeroFila, mensaje));
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

    /// <summary>Stock por unidad: entero, como siempre (acepta "10.0"). Stock medido: hasta 3 decimales.</summary>
    private static decimal LeerCantidad(IXLCell celda, string nombreColumna, UnidadMedida unidad)
    {
        var texto = celda.GetString().Trim();
        if (string.IsNullOrWhiteSpace(texto))
            return 0;

        if (unidad == UnidadMedida.Unidad)
        {
            if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor))
                return valor;

            // Por si viene como "10.0" en vez de entero.
            return (int)decimal.Round(LeerDecimal(celda, nombreColumna));
        }

        return decimal.Round(LeerDecimalConComa(celda, nombreColumna), ReglasCantidad.DecimalesMedida);
    }

    /// <summary>Para cantidades medidas: la coma es el decimal ("12,5" son 12 kilos y medio). Si trae las dos,
    /// el último separador es el decimal ("1.234,5" y "1,234.5" valen 1234,5).</summary>
    private static decimal LeerDecimalConComa(IXLCell celda, string nombreColumna)
    {
        var texto = celda.GetString().Trim().Replace(" ", "");
        var coma = texto.LastIndexOf(',');
        var punto = texto.LastIndexOf('.');
        if (coma >= 0 && punto >= 0)
            texto = coma > punto ? texto.Replace(".", "").Replace(',', '.') : texto.Replace(",", "");
        else if (coma >= 0)
            texto = texto.Replace(',', '.');

        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor))
            return valor;

        throw new FormatException($"'{nombreColumna}' no es un número válido: '{celda.GetString().Trim()}'.");
    }

    /// <summary>Celda vacía = sin dato (producto nuevo por unidad; en uno existente se deja la que tiene).</summary>
    private static UnidadMedida? LeerUnidad(IXLCell celda)
    {
        var texto = celda.GetString().Trim().ToLowerInvariant();
        return texto switch
        {
            "" => null,
            "u" or "un" or "unidad" or "unidades" => UnidadMedida.Unidad,
            "kg" or "kilo" or "kilos" => UnidadMedida.Kilo,
            "l" or "lt" or "litro" or "litros" => UnidadMedida.Litro,
            "m" or "mt" or "metro" or "metros" => UnidadMedida.Metro,
            _ => throw new FormatException($"La unidad '{texto}' no es válida. Usá u, kg, L o m.")
        };
    }
}
