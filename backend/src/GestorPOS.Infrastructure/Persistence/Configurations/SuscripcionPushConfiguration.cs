using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class SuscripcionPushConfiguration : IEntityTypeConfiguration<SuscripcionPush>
{
    public void Configure(EntityTypeBuilder<SuscripcionPush> builder)
    {
        builder.ToTable("SuscripcionesPush");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Endpoint).IsRequired().HasMaxLength(500);
        builder.Property(s => s.P256dh).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Auth).IsRequired().HasMaxLength(200);
        builder.HasIndex(s => s.Endpoint).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.UsuarioId });
    }
}
