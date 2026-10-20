using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Persistence;

public static class BloqueoDeStock
{
    /// <summary>
    /// Bloquea las filas de esos productos hasta que termine la transacción actual. Dos ventas (o compras o
    /// ajustes) simultáneas del mismo producto hacen fila: la segunda espera, ve el stock ya descontado por la
    /// primera y recién ahí decide si alcanza. Hay que llamarlo dentro de una transacción y ANTES de leer los
    /// productos. Se bloquea siempre en el mismo orden para que dos operaciones con varios productos no se
    /// traben entre sí.
    /// </summary>
    public static Task BloquearProductosAsync(
        this AppDbContext db, Guid tenantId, IEnumerable<Guid> productoIds, CancellationToken ct)
    {
        var ordenados = productoIds.Distinct().OrderBy(id => id).ToArray();
        return db.Database.ExecuteSqlRawAsync(
            "SELECT \"Id\" FROM \"Productos\" WHERE \"TenantId\" = {0} AND \"Id\" = ANY({1}) ORDER BY \"Id\" FOR UPDATE",
            new object[] { tenantId, ordenados },
            ct);
    }
}
