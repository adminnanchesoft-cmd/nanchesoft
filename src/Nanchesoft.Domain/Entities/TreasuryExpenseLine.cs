using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class TreasuryExpenseLine : BaseEntity
{
    public Guid TreasuryExpenseId { get; set; }
    public TreasuryExpense? TreasuryExpense { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
}
