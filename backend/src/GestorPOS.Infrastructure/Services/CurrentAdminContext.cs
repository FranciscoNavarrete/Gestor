using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace GestorPOS.Infrastructure.Services;

public class CurrentAdminContext : ICurrentAdminContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentAdminContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid AdminId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(claim, out var adminId) ? adminId : Guid.Empty;
        }
    }

    public string Rol => _httpContextAccessor.HttpContext?.User.FindFirst("admin_rol")?.Value ?? string.Empty;

    public bool EsOperador => Rol == nameof(AdminRol.Operador);
}
