namespace Nanchesoft.Domain.Entities;

public sealed class ProductCostSheet
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FinishedProductId { get; set; }
    public Guid? ProductTechnicalSheetId { get; set; }
    public string CostSheetCode { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public decimal DirectMaterialCost { get; set; }
    public decimal DirectLaborCost { get; set; }
    public decimal IndirectManufacturingCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal ServiceCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TargetMarginPercent { get; set; }
    public decimal SuggestedSalePrice { get; set; }
    public string CurrencyCode { get; set; } = "MXN";
    public string Notes { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
