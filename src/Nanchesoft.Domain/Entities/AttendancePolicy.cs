using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class AttendancePolicy : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? WorkShiftId { get; set; }
    public WorkShift? WorkShift { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // "company" | "department" | "shift"
    public string Scope { get; set; } = "company";
    public int Priority { get; set; } = 100;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int ToleranceMinutes { get; set; }
    public int MinOvertimeMinutes { get; set; } = 15;
    public bool RequiresPunchIn { get; set; } = true;
    public bool RequiresPunchOut { get; set; } = true;
    public bool IsDefault { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }

    public ICollection<AttendancePolicyRule> Rules { get; set; } = new List<AttendancePolicyRule>();
}
