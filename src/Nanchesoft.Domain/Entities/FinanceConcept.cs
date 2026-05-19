using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class FinanceConcept : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // payroll | purchase | sales | tax | service | transfer | loan | other
    public string Direction { get; set; } = "neutral"; // in | out | neutral
    public Guid? AccountingAccountId { get; set; }
    public AccountingAccount? AccountingAccount { get; set; }
    public bool IsSystem { get; set; }
    public string Notes { get; set; } = string.Empty;
}
