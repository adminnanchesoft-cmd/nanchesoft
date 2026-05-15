using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollDispersionLine : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollDispersionBatchId { get; set; }
    public PayrollDispersionBatch? PayrollDispersionBatch { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid PayrollRunLineId { get; set; }
    public PayrollRunLine? PayrollRunLine { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int Sequence { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = "ready";
    public bool IsRejected { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = "pending";
    public string Notes { get; set; } = string.Empty;
}
