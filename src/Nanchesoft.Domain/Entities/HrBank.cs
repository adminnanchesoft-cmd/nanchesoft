using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class HrBank : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Code { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SatCode { get; set; }
}
