using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SalesInvoice : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public Guid? SalesShipmentId { get; set; }
    public SalesShipment? SalesShipment { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public Guid? SeriesId { get; set; }
    public DocumentSeries? Series { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;
    public string Folio { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }

    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
}
