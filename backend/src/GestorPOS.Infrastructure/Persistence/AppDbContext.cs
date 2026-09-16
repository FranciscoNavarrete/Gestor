using System.Linq.Expressions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Common;
using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<MedioPagoConfiguracion> MediosPago => Set<MedioPagoConfiguracion>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<CajaDiaria> CajasDiarias => Set<CajaDiaria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter: toda entidad con TenantId queda automáticamente
        // acotada al tenant de la request actual — evita fugas de datos entre negocios.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
            var currentTenantId = Expression.Property(
                Expression.Constant(this), nameof(CurrentTenantId));
            var body = Expression.Equal(tenantIdProperty, currentTenantId);
            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }

        base.OnModelCreating(modelBuilder);
    }

    // Usado por la expresión del query filter de arriba (debe ser una propiedad, no un método).
    private Guid CurrentTenantId => _tenantContext?.TenantId ?? Guid.Empty;
}
