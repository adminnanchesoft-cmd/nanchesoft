using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Receipt : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid? SeriesId { get; set; }
    public DocumentSeries? Series { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public Guid? CashAccountId { get; set; }
    public CashAccount? CashAccount { get; set; }
    public Guid? BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow.Date;
    public string TargetType { get; set; } = "cash";
    public decimal ExchangeRate { get; set; } = 1m;
    public string Status { get; set; } = "draft";
    public string Reference { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public ICollection<ReceiptLine> Lines { get; set; } = new List<ReceiptLine>();
}
