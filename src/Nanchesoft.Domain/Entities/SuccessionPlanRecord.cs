using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SuccessionPlanRecord : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid PositionId { get; set; }
    public Position? Position { get; set; }

    public Guid? IncumbentEmployeeId { get; set; }
    public Employee? IncumbentEmployee { get; set; }

    public Guid SuccessorEmployeeId { get; set; }
    public Employee? SuccessorEmployee { get; set; }

    public string PlanCode { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string ReadinessLevel { get; set; } = string.Empty;
    public string RiskOfLoss { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
    public DateTime? TargetReadyDate { get; set; }
    public bool IsNominationApproved { get; set; }
    public string DevelopmentPlan { get; set; } = string.Empty;
    public string Status { get; set; } = "candidate_pool";
    public string Notes { get; set; } = string.Empty;
}
