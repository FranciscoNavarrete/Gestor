using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class ProductoPresentacionConfiguration : IEntityTypeConfiguration<ProductoPresentacion>
{
    public void Configure(EntityTypeBuilder<ProductoPresentacion> builder)
    {
        builder.ToTable("ProductoPresentaciones");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(p => p.CantidadBase).HasColumnType("numeric(12,3)");
        builder.Property(p => p.Precio).HasColumnType("numeric(12,2)");
        builder.HasIndex(p => new { p.TenantId, p.ProductoId });
    }
}
