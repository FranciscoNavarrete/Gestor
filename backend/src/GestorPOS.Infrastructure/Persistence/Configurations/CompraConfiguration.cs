using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("Compras");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ProveedorNombre).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Total).HasColumnType("numeric(12,2)");

        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(i => i.CompraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
