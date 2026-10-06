using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class LiquidacionConfiguration : IEntityTypeConfiguration<Liquidacion>
{
    public void Configure(EntityTypeBuilder<Liquidacion> builder)
    {
        builder.ToTable("Liquidaciones");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.VendedorNombre).HasMaxLength(100).IsRequired();
        builder.Property(l => l.CerradaPorNombre).HasMaxLength(100).IsRequired();
        builder.Property(l => l.PagadaPorNombre).HasMaxLength(100);
        builder.Property(l => l.Nota).HasMaxLength(300);
        builder.Property(l => l.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.TotalComision).HasPrecision(18, 2);
        builder.Property(l => l.TotalBono).HasPrecision(18, 2);
        builder.Property(l => l.EfectivoCompensado).HasPrecision(18, 2);
        builder.Ignore(l => l.Total);
        builder.Ignore(l => l.Neto);
        builder.HasIndex(l => new { l.VendedorId, l.Anio, l.Mes });

        builder.HasMany(l => l.Items).WithOne().HasForeignKey(i => i.LiquidacionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class LiquidacionItemConfiguration : IEntityTypeConfiguration<LiquidacionItem>
{
    public void Configure(EntityTypeBuilder<LiquidacionItem> builder)
    {
        builder.ToTable("LiquidacionItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.TenantNombre).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Comision).HasPrecision(18, 2);
        builder.Property(i => i.Bono).HasPrecision(18, 2);
        // Una venta se liquida una sola vez: nunca se le paga dos veces al vendedor.
        builder.HasIndex(i => i.TenantId).IsUnique();
    }
}
