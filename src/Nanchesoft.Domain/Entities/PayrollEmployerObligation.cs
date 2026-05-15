using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollEmployerObligation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public string ObligationCode { get; set; } = string.Empty;
    public string ObligationName { get; set; } = string.Empty;
    public string ObligationType { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal Amount { get; set; }
    public int EmployeesCount { get; set; }
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date;
    public string Status { get; set; } = "draft";
    public DateTime? PaidAt { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
