using GestorPOS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GestorPOS.WebAPI.Filters;

/// <summary>Bloquea un controller/action entero si el tenant no tiene ninguna de las features
/// habilitada (OR, no AND) — mismo mecanismo de TenantFeature/TieneFeature que ya usa VentaService
/// para "A cuenta", pero acá se aplica a nivel de endpoint completo en vez de una validación puntual
/// dentro de un service. Se activa/desactiva por negocio desde el panel admin (CATALOGO_FEATURES en
/// el frontend). Pasar más de una clave cuando el endpoint es compartido por varias features (ej.
/// ProveedoresController lo usan tanto Compras como Facturas a proveedores).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFeatureAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _claves;

    public RequireFeatureAttribute(params string[] claves)
    {
        _claves = claves;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();

        if (!_claves.Any(tenantContext.TieneFeature))
        {
            context.Result = new ObjectResult(new { error = "Esta funcionalidad no está habilitada para tu negocio." })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
            return;
        }

        await next();
    }
}
