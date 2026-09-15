using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> builder)
    {
        builder.ToTable("TenantFeatures");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Clave).IsRequired().HasMaxLength(100);
        builder.HasIndex(f => new { f.TenantId, f.Clave }).IsUnique();
    }
}
