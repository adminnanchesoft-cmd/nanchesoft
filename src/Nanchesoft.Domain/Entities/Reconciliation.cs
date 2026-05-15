using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Reconciliation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public DateTime ReconciliationDate { get; set; } = DateTime.UtcNow.Date;
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? ClosedAt { get; set; }
    public ICollection<ReconciliationLine> Lines { get; set; } = new List<ReconciliationLine>();
}
