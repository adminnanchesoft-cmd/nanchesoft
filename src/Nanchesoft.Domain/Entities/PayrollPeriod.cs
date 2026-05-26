using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollPeriod : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PeriodType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = "draft";
    public bool IsImssInsured { get; set; } = true;
    public bool IsClosed { get; set; }

    // Campos operativos
    public Guid? PayrollPeriodTypeId { get; set; }
    public PayrollPeriodType? PayrollPeriodTypeNav { get; set; }
    public int? FiscalYear { get; set; }
    public int? PeriodNumber { get; set; }
    public bool IsStartOfMonth { get; set; }
    public bool IsEndOfMonth { get; set; }
    public bool IsStartOfYear { get; set; }
    public bool IsEndOfYear { get; set; }
    public bool IsBimesterStart { get; set; }
    public bool IsBimesterEnd { get; set; }
}
