using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ThirdPartyBankAccount : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }

    public Guid BankId { get; set; }
    public Bank? Bank { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
