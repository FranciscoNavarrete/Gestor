using GestorPOS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GestorPOS.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var tenantId) ? tenantId : Guid.Empty;
        }
    }

    public bool TieneFeature(string clave)
    {
        var features = _httpContextAccessor.HttpContext?.User.FindAll("feature").Select(c => c.Value) ?? [];
        return features.Contains(clave, StringComparer.OrdinalIgnoreCase);
    }
}
