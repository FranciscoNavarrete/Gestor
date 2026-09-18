using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
{
    public void Configure(EntityTypeBuilder<MovimientoStock> builder)
    {
        builder.ToTable("MovimientosStock");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ProductoNombre).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Motivo).IsRequired().HasMaxLength(50);
        builder.Property(m => m.UsuarioNombre).IsRequired().HasMaxLength(150);
        builder.HasIndex(m => new { m.TenantId, m.ProductoId });
        builder.HasIndex(m => new { m.TenantId, m.FechaCreacion });
    }
}
