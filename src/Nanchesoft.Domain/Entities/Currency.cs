using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Currency : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<ExchangeRate> ExchangeRates { get; set; } = new List<ExchangeRate>();
    public ICollection<CompanySetting> CompanySettings { get; set; } = new List<CompanySetting>();
}
