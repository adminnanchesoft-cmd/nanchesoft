using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeePerformanceReview : BaseEntity
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

    public string ReviewCode { get; set; } = string.Empty;
    public string ReviewCycle { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime ReviewDate { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal CalibrationScore { get; set; }
    public decimal GoalCompletionPercent { get; set; }
    public string PotentialLevel { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;
}
