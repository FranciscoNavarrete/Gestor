using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class VentaItemConfiguration : IEntityTypeConfiguration<VentaItem>
{
    public void Configure(EntityTypeBuilder<VentaItem> builder)
    {
        builder.ToTable("VentaItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProductoNombre).IsRequired().HasMaxLength(200);
        builder.Property(i => i.PrecioUnitario).HasColumnType("numeric(12,2)");
        builder.Property(i => i.Subtotal).HasColumnType("numeric(12,2)");
    }
}
