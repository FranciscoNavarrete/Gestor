using System.Security.Cryptography;
using System.Text;

namespace GestorPOS.WebAPI.Middleware;

/// <summary>Protege /api/admin con una API key propia en vez del JWT de tenant — este panel
/// es del operador de GestorPOS, no de un negocio, así que no tiene sentido meterlo en el
/// esquema de auth multi-tenant.</summary>
public class AdminApiKeyMiddleware
{
    private const string HeaderName = "X-Admin-Api-Key";
    private readonly RequestDelegate _next;

    public AdminApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (!context.Request.Path.StartsWithSegments("/api/admin"))
        {
            await _next(context);
            return;
        }

        var apiKeyConfigurada = configuration["Admin:ApiKey"];
        var apiKeyRecibida = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKeyConfigurada) ||
            string.IsNullOrEmpty(apiKeyRecibida) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(apiKeyRecibida), Encoding.UTF8.GetBytes(apiKeyConfigurada)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key de administrador inválida o faltante." });
            return;
        }

        await _next(context);
    }
}
