using GestorPOS.Domain.Entities;
using GestorPOS.Domain.Enums;
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
        builder.Property(i => i.Cantidad).HasColumnType("numeric(12,3)");
        builder.Property(i => i.PresentacionNombre).HasMaxLength(100);
        builder.Property(i => i.FactorBase).HasColumnType("numeric(12,3)").HasDefaultValue(1m);
        builder.Property(i => i.Unidad).HasDefaultValue(UnidadMedida.Unidad);
        builder.Property(i => i.PrecioUnitario).HasColumnType("numeric(12,2)");
        builder.Property(i => i.CostoUnitario).HasColumnType("numeric(12,2)");
        builder.Property(i => i.Subtotal).HasColumnType("numeric(12,2)");
        builder.Ignore(i => i.GananciaNeta);
        builder.Ignore(i => i.CantidadBase);
    }
}
