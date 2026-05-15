using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PurchaseRequisitionLine : BaseEntity
{
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }

    public int LineNumber { get; set; }

    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Notes { get; set; } = string.Empty;
}
