using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class PagoCuentaConfiguration : IEntityTypeConfiguration<PagoCuenta>
{
    public void Configure(EntityTypeBuilder<PagoCuenta> builder)
    {
        builder.ToTable("PagosCuenta");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Monto).HasColumnType("numeric(12,2)");
        builder.Property(p => p.MedioPago).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => new { p.TenantId, p.ClienteId });
        builder.HasIndex(p => new { p.TenantId, p.FechaCreacion });
    }
}
