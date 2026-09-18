using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Registro de auditoría de cada cambio de stock — ventas y ajustes manuales (reposición,
/// merma, corrección). ProductoNombre y UsuarioNombre quedan denormalizados a propósito, mismo
/// patrón que VentaItem.ProductoNombre: el historial no debe cambiar si después renombran el
/// producto o el usuario deja de existir.</summary>
public class MovimientoStock : TenantEntity
{
    public Guid ProductoId { get; private set; }
    public string ProductoNombre { get; private set; } = string.Empty;
    public int Cantidad { get; private set; }
    public int StockResultante { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public Guid UsuarioId { get; private set; }
    public string UsuarioNombre { get; private set; } = string.Empty;

    private MovimientoStock() { }

    public static MovimientoStock Crear(
        Guid tenantId, Guid productoId, string productoNombre, int cantidad, int stockResultante,
        string motivo, Guid usuarioId, string usuarioNombre)
    {
        if (cantidad == 0)
            throw new ArgumentException("El movimiento de stock no puede ser de cantidad cero.", nameof(cantidad));

        return new MovimientoStock
        {
            TenantId = tenantId,
            ProductoId = productoId,
            ProductoNombre = productoNombre,
            Cantidad = cantidad,
            StockResultante = stockResultante,
            Motivo = motivo,
            UsuarioId = usuarioId,
            UsuarioNombre = usuarioNombre
        };
    }
}
