using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Bank : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public string LogoUrl { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string CustomerServicePhone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
}
