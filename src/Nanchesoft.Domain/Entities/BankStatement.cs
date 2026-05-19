using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class BankStatement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public DateTime StatementDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Source { get; set; } = "manual";
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public ICollection<BankStatementEntry> Entries { get; set; } = new List<BankStatementEntry>();
}
