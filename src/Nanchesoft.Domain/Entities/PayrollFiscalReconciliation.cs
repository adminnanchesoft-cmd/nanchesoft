using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollFiscalReconciliation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public Guid? PayrollDispersionBatchId { get; set; }
    public PayrollDispersionBatch? PayrollDispersionBatch { get; set; }

    public Guid? PayrollAccountingPostingId { get; set; }
    public PayrollAccountingPosting? PayrollAccountingPosting { get; set; }

    public string ReconciliationCode { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public int ReceiptsStampedCount { get; set; }
    public int DispersionValidatedCount { get; set; }
    public int AccountingPostedCount { get; set; }
    public int TaxAccumulatorsCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal WithheldIsrAmount { get; set; }
    public decimal EmployerTaxesAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? ReconciledAt { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
