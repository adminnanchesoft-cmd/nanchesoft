using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollDailyEntry : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public Guid PayrollDayMnemonicId { get; set; }
    public PayrollDayMnemonic? PayrollDayMnemonic { get; set; }

    public DateTime WorkDate { get; set; }
    public decimal Units { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "captured";
    public Guid? ResultingAdjustmentId { get; set; }
}
