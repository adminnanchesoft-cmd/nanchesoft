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

    // "review" = Entrada por Revisión | "invoice" = Compra con Factura
    public string ReceiptType { get; set; } = "review";
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow.Date;
    public string Folio { get; set; } = string.Empty;
    // draft | reviewed | authorized | rejected | cancelled
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;

    // Supplier document
    public string SupplierDocumentNumber { get; set; } = string.Empty;
    public DateTime? SupplierDocumentDate { get; set; }

    // Financials
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    // Review
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }

    // Authorization
    public DateTime? AuthorizedAt { get; set; }
    public string? AuthorizedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }

    // Differences vs PO
    public bool HasDifferences { get; set; }
    public bool DifferencesAuthorized { get; set; }
    public DateTime? DifferencesAuthorizedAt { get; set; }
    public string? DifferencesAuthorizedBy { get; set; }

    // Payment
    // pending | partial | paid
    public string PaymentStatus { get; set; } = "pending";
    public decimal PaidAmount { get; set; }

    // Conversion: review → invoice
    public Guid? ConvertedToInvoiceId { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public string? ConvertedBy { get; set; }

    public DateTime? PostedAt { get; set; }

    public ICollection<PurchaseReceiptLine> Lines { get; set; } = new List<PurchaseReceiptLine>();
    public ICollection<PurchasePayment> Payments { get; set; } = new List<PurchasePayment>();
}
