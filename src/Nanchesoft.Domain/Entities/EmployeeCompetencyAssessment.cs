using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeeCompetencyAssessment : BaseEntity
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

    public string AssessmentCode { get; set; } = string.Empty;
    public string CompetencyCode { get; set; } = string.Empty;
    public string CompetencyName { get; set; } = string.Empty;
    public int ExpectedLevel { get; set; }
    public int AchievedLevel { get; set; }
    public int GapLevel { get; set; }
    public DateTime AssessedAt { get; set; }
    public string AssessorName { get; set; } = string.Empty;
    public string DevelopmentAction { get; set; } = string.Empty;
    public string Status { get; set; } = "captured";
    public string Notes { get; set; } = string.Empty;
}
