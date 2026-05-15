namespace Nanchesoft.Domain.Entities;

public class InventoryAdjustmentLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InventoryAdjustmentId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? UnitId { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}
