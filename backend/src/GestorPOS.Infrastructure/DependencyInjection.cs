using GestorPOS.Application.Admin;
using GestorPOS.Application.Auth;
using GestorPOS.Application.Caja;
using GestorPOS.Application.Catalog;
using GestorPOS.Application.Clientes;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Configuracion;
using GestorPOS.Application.MovimientosStock;
using GestorPOS.Application.Notificaciones;
using GestorPOS.Application.Reportes;
using GestorPOS.Application.Ventas;
using GestorPOS.Infrastructure.Admin;
using GestorPOS.Infrastructure.Auth;
using GestorPOS.Infrastructure.Caja;
using GestorPOS.Infrastructure.Catalog;
using GestorPOS.Infrastructure.Clientes;
using GestorPOS.Infrastructure.Configuracion;
using GestorPOS.Infrastructure.MovimientosStock;
using GestorPOS.Infrastructure.Notificaciones;
using GestorPOS.Infrastructure.Persistence;
using GestorPOS.Infrastructure.Reportes;
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
        services.AddScoped<IMedioPagoService, MedioPagoService>();
        services.AddScoped<INegocioService, NegocioService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IProductoImportService, ProductoImportService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IMovimientoStockService, MovimientoStockService>();
        services.AddScoped<INotificacionPushService, NotificacionPushService>();
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<ICajaService, CajaService>();
        services.AddScoped<IReporteService, ReporteService>();
        services.AddScoped<IReportePdfService, ReportePdfService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
