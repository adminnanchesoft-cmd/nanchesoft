using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class WorkShift : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int BreakMinutes { get; set; }
    public int ToleranceMinutes { get; set; }
    public bool IsOvernight { get; set; }
    public string Notes { get; set; } = string.Empty;
}
