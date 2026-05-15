using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollTaxAccumulator : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid PayrollRunLineId { get; set; }
    public PayrollRunLine? PayrollRunLine { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string AccumulatorCode { get; set; } = string.Empty;
    public string AccumulatorName { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public decimal WithheldIsr { get; set; }
    public decimal SubsidyApplied { get; set; }
    public decimal SocialSecurityBase { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;
}
