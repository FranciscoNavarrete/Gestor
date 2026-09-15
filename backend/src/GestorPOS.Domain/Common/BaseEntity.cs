namespace GestorPOS.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime FechaCreacion { get; protected set; } = DateTime.UtcNow;
}
