using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PurchaseReceipt : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid? SeriesId { get; set; }
    public DocumentSeries? Series { get; set; }

    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow.Date;
    public string Folio { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;
    public DateTime? PostedAt { get; set; }

    public ICollection<PurchaseReceiptLine> Lines { get; set; } = new List<PurchaseReceiptLine>();
}
