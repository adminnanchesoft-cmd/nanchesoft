using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class RecruitmentVacancy : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }

    public string VacancyCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public DateTime OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public int Headcount { get; set; }
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public string HiringManager { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public string Notes { get; set; } = string.Empty;
}
