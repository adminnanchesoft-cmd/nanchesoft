using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeeCertificationRecord : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string CertificationCode { get; set; } = string.Empty;
    public string CertificationName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpirationDate { get; set; }
    public decimal Score { get; set; }
    public string Status { get; set; } = "active";
    public bool IsMandatory { get; set; }
    public bool RenewalRequired { get; set; }
    public string Notes { get; set; } = string.Empty;
}
