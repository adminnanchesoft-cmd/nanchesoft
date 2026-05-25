using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Supplier : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    // Basic identification
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;

    // Fiscal data (Mexico)
    public string TaxId { get; set; } = string.Empty;
    public string FiscalRegime { get; set; } = string.Empty;
    public string CfdiUse { get; set; } = string.Empty;

    // Address
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Colony { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = "México";

    // Contact
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public string SalesContact { get; set; } = string.Empty;
    public string CollectionContact { get; set; } = string.Empty;

    // Commercial terms
    public int PaymentTermDays { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }

    // Accounting
    public string AccountingAccount { get; set; } = string.Empty;

    // Discounts (Silvasoft legacy)
    public decimal DiscountPromptPayment { get; set; }
    public decimal Discount1 { get; set; }
    public decimal Discount2 { get; set; }
    public decimal Discount3 { get; set; }
    public decimal Discount4 { get; set; }

    // Payment preferences
    // "transfer" | "cash" | "check" | "card"
    public string PreferredPaymentMethod { get; set; } = "transfer";
    public string BankClabe { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
