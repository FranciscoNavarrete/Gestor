using GestorPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorPOS.Infrastructure.Persistence.Configurations;

public class TareaConfiguration : IEntityTypeConfiguration<Tarea>
{
    public void Configure(EntityTypeBuilder<Tarea> builder)
    {
        builder.ToTable("Tareas");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Notas).HasMaxLength(1000);
    }
}
