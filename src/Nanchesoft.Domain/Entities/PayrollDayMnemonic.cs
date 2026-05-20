using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollDayMnemonic : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "worked";
    public string UnitType { get; set; } = "hours";
    public decimal DefaultUnits { get; set; } = 1m;
    public decimal Multiplier { get; set; } = 1m;
    public string ColorCode { get; set; } = "#0d6efd";
    public string ShortLabel { get; set; } = string.Empty;
    public bool AffectsAttendance { get; set; } = true;
    public bool AffectsPayroll { get; set; } = true;
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
}
