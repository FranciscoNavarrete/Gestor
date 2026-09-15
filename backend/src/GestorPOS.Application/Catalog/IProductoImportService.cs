using GestorPOS.Application.Catalog.Dtos;

namespace GestorPOS.Application.Catalog;

public interface IProductoImportService
{
    /// <summary>Genera el archivo .xlsx de plantilla (encabezados + una fila de ejemplo) para descargar.</summary>
    byte[] GenerarPlantilla();

    /// <summary>Lee un .xlsx y crea o actualiza productos por SKU. No aborta ante errores puntuales:
    /// cada fila inválida se reporta en el resultado y se sigue con el resto.</summary>
    Task<ImportarProductosResultado> ImportarAsync(Stream archivoExcel, CancellationToken ct = default);
}
