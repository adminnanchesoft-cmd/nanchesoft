using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeeLoan : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }

    public string LoanNumber { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int Installments { get; set; }
    public int InstallmentsPaid { get; set; }
    public string Status { get; set; } = "active";
    public string Notes { get; set; } = string.Empty;
}
