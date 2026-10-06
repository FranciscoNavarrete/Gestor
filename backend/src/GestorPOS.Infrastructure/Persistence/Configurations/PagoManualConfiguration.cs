using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class PagoManualConfiguration : IEntityTypeConfiguration<PagoManual>
{
    public void Configure(EntityTypeBuilder<PagoManual> builder)
    {
        builder.ToTable("PagosManuales");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Monto).HasPrecision(18, 2);
        builder.Property(p => p.Metodo).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Nota).HasMaxLength(300);
        builder.Property(p => p.RegistradoPorNombre).HasMaxLength(100).IsRequired();
        builder.Property(p => p.RegistradoPorRol).HasMaxLength(20).IsRequired();
        builder.Property(p => p.ConfirmadoPorNombre).HasMaxLength(100);
        builder.HasIndex(p => p.TenantId);
    }
}
