using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class OnboardingChecklistRecord : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? CandidateApplicationId { get; set; }
    public CandidateApplication? CandidateApplication { get; set; }

    public string ChecklistCode { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public DateTime PlannedDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ResponsibleArea { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public decimal CompletionPercent { get; set; }
    public bool AssetsAssigned { get; set; }
    public bool CredentialsIssued { get; set; }
    public bool InductionCompleted { get; set; }
    public string Notes { get; set; } = string.Empty;
}
