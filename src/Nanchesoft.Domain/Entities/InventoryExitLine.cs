namespace Nanchesoft.Domain.Entities;

public class InventoryExitLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InventoryExitId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? UnitId { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}
