using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Warehouse : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public Tenant? Tenant { get; set; }
    public Company? Company { get; set; }
    public Branch? Branch { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}