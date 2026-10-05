using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

public interface IMovimientosAdminService
{
    /// <summary>Movimientos del panel entre dos fechas (hora de Argentina), del más nuevo al más viejo.
    /// Tira AppException si las fechas no son válidas.</summary>
    Task<MovimientosAdminDto> ListarAsync(
        DateOnly desde, DateOnly hasta, Guid? adminId, string? accion, string? texto, CancellationToken ct = default);
}
