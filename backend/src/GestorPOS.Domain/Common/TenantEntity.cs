namespace GestorPOS.Domain.Common;

public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; protected set; }
}
