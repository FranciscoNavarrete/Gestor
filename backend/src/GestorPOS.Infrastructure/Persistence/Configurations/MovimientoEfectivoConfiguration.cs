using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class MovimientoEfectivoConfiguration : IEntityTypeConfiguration<MovimientoEfectivo>
{
    public void Configure(EntityTypeBuilder<MovimientoEfectivo> builder)
    {
        builder.ToTable("MovimientosEfectivo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Monto).HasPrecision(18, 2);
        builder.Property(m => m.Nota).HasMaxLength(300);
        builder.Property(m => m.RegistradoPorNombre).HasMaxLength(100).IsRequired();
        builder.HasIndex(m => m.VendedorId);
        builder.HasIndex(m => m.LiquidacionId);
    }
}
