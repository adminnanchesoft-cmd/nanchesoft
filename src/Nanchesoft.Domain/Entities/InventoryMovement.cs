namespace Nanchesoft.Domain.Entities;

public class InventoryMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ItemId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialId { get; set; }
    public string MovementType { get; set; } = "entry";
    public string DocumentType { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public Guid? DocumentLineId { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow.Date;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Reference { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
}
