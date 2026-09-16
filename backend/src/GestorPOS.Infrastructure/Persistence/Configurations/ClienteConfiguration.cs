using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Telefono).IsRequired().HasMaxLength(30);
        builder.Property(c => c.Nombre).HasMaxLength(200);
        builder.HasIndex(c => new { c.TenantId, c.Telefono }).IsUnique();
    }
}
