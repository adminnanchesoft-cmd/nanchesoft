using Nanchesoft.Domain.Common;
using Nanchesoft.Domain.Enums;

namespace Nanchesoft.Domain.Entities;

public sealed class Tenant : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    public ICollection<Company> Companies { get; set; } = new List<Company>();
}