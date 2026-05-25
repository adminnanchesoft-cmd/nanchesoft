using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PurchaseReceiptDiff : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }

    public Guid PurchaseReceiptId { get; set; }
    public PurchaseReceipt? PurchaseReceipt { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public bool Authorized { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public string? AuthorizedBy { get; set; }
    public string? AuthorizationNotes { get; set; }

    public ICollection<PurchaseReceiptDiffLine> Lines { get; set; } = new List<PurchaseReceiptDiffLine>();
}

public sealed class PurchaseReceiptDiffLine : BaseEntity
{
    public Guid PurchaseReceiptDiffId { get; set; }
    public PurchaseReceiptDiff? Diff { get; set; }

    public Guid? MaterialItemId { get; set; }
    public MaterialItem? MaterialItem { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    // "quantity_diff" | "cost_diff" | "material_added" | "material_missing" | "material_changed"
    public string DiffType { get; set; } = string.Empty;

    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal QuantityDiff { get; set; }

    public decimal OrderedUnitPrice { get; set; }
    public decimal ReceivedUnitPrice { get; set; }
    public decimal PriceDiff { get; set; }

    public decimal OrderedTotal { get; set; }
    public decimal ReceivedTotal { get; set; }
    public decimal TotalDiff { get; set; }
}
