using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductStyle : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? ProductLineId { get; set; }
    public ProductLine? ProductLine { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CustomerLabel1 { get; set; } = string.Empty;
    public string CustomerLabel2 { get; set; } = string.Empty;
    public string ColorLabel { get; set; } = string.Empty;
    public string DieCutReference { get; set; } = string.Empty;
    public decimal MaxLotSize { get; set; }
    public bool HasAuthorizedConsumption { get; set; }
    public bool HandlesFractionsByStyle { get; set; }
    public string TechnicalNotes { get; set; } = string.Empty;
    public string ProductionCardNotes { get; set; } = string.Empty;
    public string OutsourcedProcessName { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
}
