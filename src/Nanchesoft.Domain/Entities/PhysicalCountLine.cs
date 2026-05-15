namespace Nanchesoft.Domain.Entities;

public class PhysicalCountLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PhysicalCountId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialId { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public Guid? UnitId { get; set; }
}
