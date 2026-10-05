using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class MovimientoAdminConfiguration : IEntityTypeConfiguration<MovimientoAdmin>
{
    public void Configure(EntityTypeBuilder<MovimientoAdmin> builder)
    {
        builder.ToTable("MovimientosAdmin");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.AdminNombre).HasMaxLength(100).IsRequired();
        builder.Property(m => m.AdminRol).HasMaxLength(20).IsRequired();
        builder.Property(m => m.Accion).HasMaxLength(40).IsRequired();
        builder.Property(m => m.Entidad).HasMaxLength(20).IsRequired();
        builder.Property(m => m.EntidadNombre).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Detalle).HasMaxLength(300);
        builder.HasIndex(m => m.FechaUtc);
    }
}
