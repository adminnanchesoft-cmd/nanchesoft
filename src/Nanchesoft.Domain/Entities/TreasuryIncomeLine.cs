using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class TreasuryIncomeLine : BaseEntity
{
    public Guid TreasuryIncomeId { get; set; }
    public TreasuryIncome? TreasuryIncome { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
}
