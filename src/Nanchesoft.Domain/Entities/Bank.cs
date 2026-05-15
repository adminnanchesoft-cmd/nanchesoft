using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Bank : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
}
