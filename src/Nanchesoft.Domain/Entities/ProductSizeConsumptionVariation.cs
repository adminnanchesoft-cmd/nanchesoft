namespace Nanchesoft.Domain.Entities;

public sealed class ProductSizeConsumptionVariation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FinishedProductId { get; set; }
    public Guid? ProductComponentId { get; set; }
    public string BaseSizeCode { get; set; } = string.Empty;
    public string TargetSizeCode { get; set; } = string.Empty;
    public decimal VariationPercent { get; set; }
    public decimal QuantityDelta { get; set; }
    public bool AppliesToConsumption { get; set; } = true;
    public bool AppliesToCosting { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
