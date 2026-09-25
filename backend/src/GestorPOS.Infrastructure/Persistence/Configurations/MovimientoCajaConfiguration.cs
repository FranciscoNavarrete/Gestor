using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class MovimientoCajaConfiguration : IEntityTypeConfiguration<MovimientoCaja>
{
    public void Configure(EntityTypeBuilder<MovimientoCaja> builder)
    {
        builder.ToTable("MovimientosCaja");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tipo).IsRequired().HasMaxLength(10);
        builder.Property(m => m.Monto).HasColumnType("numeric(12,2)");
        builder.Property(m => m.Motivo).IsRequired().HasMaxLength(200);
    }
}
