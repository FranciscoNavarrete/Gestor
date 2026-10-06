using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class AceptacionTerminosConfiguration : IEntityTypeConfiguration<AceptacionTerminos>
{
    public void Configure(EntityTypeBuilder<AceptacionTerminos> builder)
    {
        builder.ToTable("AceptacionesTerminos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Version).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Origen).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.UsuarioNombre).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Ip).HasMaxLength(64);
        builder.HasIndex(a => new { a.TenantId, a.Version });
    }
}
