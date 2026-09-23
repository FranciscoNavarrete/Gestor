using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class FacturaProveedorConfiguration : IEntityTypeConfiguration<FacturaProveedor>
{
    public void Configure(EntityTypeBuilder<FacturaProveedor> builder)
    {
        builder.ToTable("FacturasProveedor");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.ProveedorNombre).IsRequired().HasMaxLength(200);
        builder.Property(f => f.NumeroFactura).IsRequired().HasMaxLength(40);
        builder.Property(f => f.Monto).HasColumnType("numeric(12,2)");
    }
}
