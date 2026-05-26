using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollPeriodType : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DaysPerPeriod { get; set; }
    public int PeriodsPerYear { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Campos operativos
    public int PaymentDays { get; set; }
    public int WorkingDays { get; set; }
    public bool AdjustToCalendarMonth { get; set; }
    public string QuinceaAdjustType { get; set; } = "LaborDays";
    public int? SeventhDayPosition { get; set; }
    public int PaymentDayPosition { get; set; }

    public ICollection<PayrollPeriod> Periods { get; set; } = new List<PayrollPeriod>();
}
