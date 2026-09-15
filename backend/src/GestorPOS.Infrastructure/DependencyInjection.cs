using GestorPOS.Application.Auth;
using GestorPOS.Application.Catalog;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Ventas;
using GestorPOS.Infrastructure.Auth;
using GestorPOS.Infrastructure.Catalog;
using GestorPOS.Infrastructure.Persistence;
using GestorPOS.Infrastructure.Services;
using GestorPOS.Infrastructure.Ventas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IVentaService, VentaService>();

        return services;
    }
}
