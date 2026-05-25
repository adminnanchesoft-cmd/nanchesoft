using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialInventoryMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public Guid MaterialItemId { get; set; }
    public MaterialItem? MaterialItem { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    // "entry" | "exit" | "adjustment" | "transfer_in" | "transfer_out"
    public string MovementType { get; set; } = "entry";

    // "review_receipt" | "purchase_invoice" | "return" | "adjustment"
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string DocumentFolio { get; set; } = string.Empty;

    public DateTime MovementDate { get; set; } = DateTime.UtcNow.Date;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
