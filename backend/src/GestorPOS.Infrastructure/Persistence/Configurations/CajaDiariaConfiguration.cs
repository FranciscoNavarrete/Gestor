using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class CajaDiariaConfiguration : IEntityTypeConfiguration<CajaDiaria>
{
    public void Configure(EntityTypeBuilder<CajaDiaria> builder)
    {
        builder.ToTable("CajasDiarias");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.MontoApertura).HasColumnType("numeric(12,2)");
        builder.Property(c => c.MontoCierreEsperado).HasColumnType("numeric(12,2)");
        builder.Property(c => c.MontoCierreReal).HasColumnType("numeric(12,2)");
        builder.Ignore(c => c.Diferencia);
    }
}
