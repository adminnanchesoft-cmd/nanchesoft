using System.ComponentModel.DataAnnotations;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialItem
{
    public const string DraftCostStatus = "draft";
    public const string AuthorizedCostStatus = "authorized";
    public const string ReviewRequiredCostStatus = "review_required";
    public const string ObsoleteCostStatus = "obsolete";

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid MaterialSubfamilyId { get; set; }
    public Guid? MaterialCharacteristicId { get; set; }
    public Guid? MaterialSizeId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public Guid? IssueUnitId { get; set; }
    public Guid? SupplierId { get; set; }
    [MaxLength(60)] public string Code { get; set; } = string.Empty;
    [MaxLength(220)] public string Name { get; set; } = string.Empty;
    [MaxLength(600)] public string Description { get; set; } = string.Empty;
    [MaxLength(80)] public string LegacyMaterialName { get; set; } = string.Empty;
    public decimal AuthorizedCost { get; set; }
    public decimal LastPurchaseCost { get; set; }
    public decimal StandardCost { get; set; }
    [MaxLength(40)] public string CostStatus { get; set; } = DraftCostStatus;
    public bool IsServiceItem { get; set; }
    [MaxLength(1200)] public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(120)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(120)] public string? UpdatedBy { get; set; }

    public MaterialSubfamily? MaterialSubfamily { get; set; }
    public MaterialCharacteristic? MaterialCharacteristic { get; set; }
    public MaterialSize? MaterialSize { get; set; }
    public Unit? PurchaseUnit { get; set; }
    public Unit? IssueUnit { get; set; }
    public Supplier? Supplier { get; set; }

    public static string BuildName(string characteristicName, string sizeName)
        => string.Join(" ", new[] { characteristicName.Trim(), sizeName.Trim() }
            .Where(s => !string.IsNullOrWhiteSpace(s)))
            .ToUpperInvariant();

    public bool RequiresUnits => !IsServiceItem;

    public void NormalizeCostStatus()
    {
        CostStatus = NormalizeCostStatus(CostStatus);
    }

    public static string NormalizeCostStatus(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

        return normalized switch
        {
            AuthorizedCostStatus or "autorizado" => AuthorizedCostStatus,
            ReviewRequiredCostStatus or "review" or "revision" or "revisar" => ReviewRequiredCostStatus,
            ObsoleteCostStatus or "obsolete" or "obsoleto" => ObsoleteCostStatus,
            _ => DraftCostStatus
        };
    }
}
