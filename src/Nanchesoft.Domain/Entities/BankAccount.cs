using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class BankAccount : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? BankId { get; set; }
    public Bank? Bank { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public string BankBranch { get; set; } = string.Empty;
    public string AccountExecutive { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal ReconciledBalance { get; set; }
}
