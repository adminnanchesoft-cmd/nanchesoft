using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class BankStatementEntry : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BankStatementId { get; set; }
    public BankStatement? BankStatement { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow.Date;
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public decimal? BalanceAfter { get; set; }
    public Guid? MatchedMovementId { get; set; }
    public bool IsMatched { get; set; }
}
