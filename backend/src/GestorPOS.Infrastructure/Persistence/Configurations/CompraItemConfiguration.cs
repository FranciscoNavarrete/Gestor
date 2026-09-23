using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class CompraItemConfiguration : IEntityTypeConfiguration<CompraItem>
{
    public void Configure(EntityTypeBuilder<CompraItem> builder)
    {
        builder.ToTable("CompraItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProductoNombre).IsRequired().HasMaxLength(200);
        builder.Property(i => i.CostoUnitario).HasColumnType("numeric(12,2)");
        builder.Property(i => i.Subtotal).HasColumnType("numeric(12,2)");
    }
}
