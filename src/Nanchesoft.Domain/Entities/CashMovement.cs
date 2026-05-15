using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CashMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid CashAccountId { get; set; }
    public CashAccount? CashAccount { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public decimal BalanceAfter { get; set; }
}
