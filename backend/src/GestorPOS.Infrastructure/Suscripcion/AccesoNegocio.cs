using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Suscripcion;

/// <summary>Devuelve el acceso a los usuarios de un negocio al que se le había cortado: al desactivarlo se les liberó el
/// email y se los desactivó, así que al reactivarlo hay que restaurar ambas cosas.</summary>
public static class AccesoNegocio
{
    /// <summary>Tira <see cref="AppException"/> (sin tocar nada) si el email de algún usuario ya lo usa otro negocio.
    /// Con <paramref name="aplicar"/> en false solo verifica.</summary>
    public static async Task RestaurarUsuariosAsync(AppDbContext db, Guid tenantId, bool aplicar, CancellationToken ct)
    {
        var usuarios = await db.Usuarios.IgnoreQueryFilters().Where(u => u.TenantId == tenantId).ToListAsync(ct);
        var aRestaurar = usuarios
            .Select(u => (Usuario: u, Original: u.EmailOriginalLiberado()))
            .Where(x => x.Original is not null)
            .ToList();

        foreach (var (usuario, original) in aRestaurar)
        {
            var enUso = await db.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.Email == original && x.Id != usuario.Id, ct);
            if (enUso)
                throw new AppException($"No se puede reactivar el negocio: el email {original} ya lo usa otro negocio.");
        }

        if (!aplicar) return;
        foreach (var (usuario, original) in aRestaurar)
        {
            usuario.RestaurarEmail(original!);
            usuario.Activar();
        }
    }
}
