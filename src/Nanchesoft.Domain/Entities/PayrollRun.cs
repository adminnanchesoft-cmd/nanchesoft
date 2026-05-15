using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollRun : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public string Folio { get; set; } = string.Empty;
    public DateTime RunDate { get; set; }
    public string Status { get; set; } = "draft";
    public int EmployeeCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Notes { get; set; } = string.Empty;

    public ICollection<PayrollRunLine> Lines { get; set; } = new List<PayrollRunLine>();
}
