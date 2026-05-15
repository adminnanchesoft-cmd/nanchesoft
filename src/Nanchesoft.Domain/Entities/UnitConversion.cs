using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class UnitConversion : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid FromUnitId { get; set; }
    public Unit? FromUnit { get; set; }

    public Guid ToUnitId { get; set; }
    public Unit? ToUnit { get; set; }

    public decimal ConversionFactor { get; set; }
    public bool IsBidirectional { get; set; }
    public string Notes { get; set; } = string.Empty;
}
