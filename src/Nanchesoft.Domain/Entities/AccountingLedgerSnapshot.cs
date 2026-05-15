namespace Nanchesoft.Domain.Entities;

public class AccountingLedgerSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid AccountId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal ClosingBalance { get; set; }
}
