using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeeLoanDeduction : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid EmployeeLoanId { get; set; }
    public EmployeeLoan? EmployeeLoan { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public Guid? PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid? PayrollRunLineId { get; set; }
    public PayrollRunLine? PayrollRunLine { get; set; }

    public DateTime DeductionDate { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalApplied { get; set; }
    public decimal InterestApplied { get; set; }
    public decimal RemainingBalance { get; set; }
    public string Status { get; set; } = "applied";
    public string Notes { get; set; } = string.Empty;
}
