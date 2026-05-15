using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PurchaseOrder : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public Guid? PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }

    public Guid? SeriesId { get; set; }
    public DocumentSeries? Series { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow.Date;
    public string Folio { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public decimal ExchangeRate { get; set; } = 1m;
    public int PaymentTermDays { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
