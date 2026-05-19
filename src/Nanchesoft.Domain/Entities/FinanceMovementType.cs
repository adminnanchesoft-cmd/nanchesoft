using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class FinanceMovementType : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = "neutral"; // in | out | neutral
    public string Nature { get; set; } = string.Empty; // bank | cash | transfer | fee | interest | adjustment | charge
    public bool AffectsBalance { get; set; } = true;
    public bool IsSystem { get; set; }
    public Guid? AccountingAccountId { get; set; }
    public AccountingAccount? AccountingAccount { get; set; }
    public string Notes { get; set; } = string.Empty;
}
