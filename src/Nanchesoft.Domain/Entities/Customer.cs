using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Customer : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public Guid? PriceListId { get; set; }
    public ItemPriceList? PriceList { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int PaymentTermDays { get; set; }
}
