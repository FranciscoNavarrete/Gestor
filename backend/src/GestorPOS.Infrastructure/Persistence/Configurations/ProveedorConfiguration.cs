using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Cuit).HasMaxLength(20);
        builder.Property(p => p.Telefono).HasMaxLength(40);
        builder.Property(p => p.Email).HasMaxLength(200);
    }
}
