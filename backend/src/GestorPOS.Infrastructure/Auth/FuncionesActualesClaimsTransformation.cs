using System.Security.Claims;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GestorPOS.Infrastructure.Auth;

/// <summary>
/// Las funciones habilitadas de un negocio (Compras, Tareas, Medidas y presentaciones...) viajan en el token de
/// login, así que antes un cambio del operador no llegaba hasta que el dueño cerraba sesión. Acá se reemplazan,
/// en cada pedido, por las que el negocio tiene hoy en la base (con una caché de unos segundos para no sumar una
/// consulta a cada llamada). Los tokens ya emitidos siguen sirviendo.
/// </summary>
public class FuncionesActualesClaimsTransformation : IClaimsTransformation
{
    public const string TipoClaim = "feature";
    private static readonly TimeSpan Vigencia = TimeSpan.FromSeconds(10);

    public static string ClaveDeCache(Guid tenantId) => $"funciones:{tenantId}";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public FuncionesActualesClaimsTransformation(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true }
            || !Guid.TryParse(principal.FindFirst("tenant_id")?.Value, out var tenantId))
            return principal;

        var funciones = await _cache.GetOrCreateAsync(ClaveDeCache(tenantId), async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = Vigencia;
            return await _db.TenantFeatures.IgnoreQueryFilters().AsNoTracking()
                .Where(f => f.TenantId == tenantId && f.Habilitado)
                .Select(f => f.Clave)
                .ToListAsync();
        }) ?? [];

        var clon = principal.Clone();
        var identidad = (ClaimsIdentity)clon.Identity!;
        foreach (var claim in identidad.FindAll(TipoClaim).ToList())
            identidad.RemoveClaim(claim);
        foreach (var clave in funciones)
            identidad.AddClaim(new Claim(TipoClaim, clave));

        return clon;
    }
}
