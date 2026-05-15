using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ReceiptLine : BaseEntity
{
    public Guid ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
}
