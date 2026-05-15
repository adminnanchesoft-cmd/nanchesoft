using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeeContract : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string PaymentFrequency { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal IntegratedSalary { get; set; }
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;
}
