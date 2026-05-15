using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PaymentLine : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
}
