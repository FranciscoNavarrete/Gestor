using GestorPOS.Domain.Entities;
using GestorPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Precio).HasColumnType("numeric(12,2)");
        builder.Property(p => p.Costo).HasColumnType("numeric(12,2)");
        builder.Property(p => p.StockActual).HasColumnType("numeric(12,3)");
        builder.Property(p => p.StockMinimo).HasColumnType("numeric(12,3)");
        builder.Property(p => p.Unidad).HasDefaultValue(UnidadMedida.Unidad);
        builder.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
        builder.Ignore(p => p.EnStockMinimo);
    }
}
