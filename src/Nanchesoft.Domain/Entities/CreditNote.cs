using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CreditNote : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public Guid? SeriesId { get; set; }
    public DocumentSeries? Series { get; set; }

    public DateTime CreditNoteDate { get; set; } = DateTime.UtcNow.Date;
    public string Folio { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }

    public ICollection<CreditNoteLine> Lines { get; set; } = new List<CreditNoteLine>();
}
