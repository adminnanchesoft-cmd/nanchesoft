using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CandidateApplication : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid RecruitmentVacancyId { get; set; }
    public RecruitmentVacancy? RecruitmentVacancy { get; set; }

    public Guid? HiredEmployeeId { get; set; }
    public Employee? HiredEmployee { get; set; }

    public string CandidateCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public string Stage { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal OfferAmount { get; set; }
    public string CvFileName { get; set; } = string.Empty;
    public string CvFilePath { get; set; } = string.Empty;
    public string Status { get; set; } = "applied";
    public string Notes { get; set; } = string.Empty;
}
