using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class MedioPagoConfiguracionConfiguration : IEntityTypeConfiguration<MedioPagoConfiguracion>
{
    public void Configure(EntityTypeBuilder<MedioPagoConfiguracion> builder)
    {
        builder.ToTable("MediosPago");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Nombre).IsRequired().HasMaxLength(50);
        builder.HasIndex(m => new { m.TenantId, m.Nombre }).IsUnique();
    }
}
