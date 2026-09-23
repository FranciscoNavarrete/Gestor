using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class PagoFacturaProveedorConfiguration : IEntityTypeConfiguration<PagoFacturaProveedor>
{
    public void Configure(EntityTypeBuilder<PagoFacturaProveedor> builder)
    {
        builder.ToTable("PagosFacturaProveedor");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Monto).HasColumnType("numeric(12,2)");
        builder.Property(p => p.MedioPago).IsRequired().HasMaxLength(40);
    }
}
