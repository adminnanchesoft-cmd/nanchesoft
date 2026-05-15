using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeeLaborMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }

    public string MovementCode { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? AppliedAt { get; set; }
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public decimal SalaryBefore { get; set; }
    public decimal SalaryAfter { get; set; }
    public string AuthorizedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public bool ImpactsPayroll { get; set; }
    public string Notes { get; set; } = string.Empty;
}
