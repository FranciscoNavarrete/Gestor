using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Total).HasColumnType("numeric(12,2)");
        builder.Property(v => v.TelefonoCliente).HasMaxLength(30);

        builder.HasMany(v => v.Items)
            .WithOne()
            .HasForeignKey(i => i.VentaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
