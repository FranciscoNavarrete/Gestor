using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class GrupoDuenoConfiguration : IEntityTypeConfiguration<GrupoDueno>
{
    public void Configure(EntityTypeBuilder<GrupoDueno> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(g => g.Telefono).HasMaxLength(40);
    }
}
