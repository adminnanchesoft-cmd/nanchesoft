using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EntityChangeLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = "upsert";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Guid? ChangedByUserId { get; set; }

    public string? PayloadJson { get; set; }
}
