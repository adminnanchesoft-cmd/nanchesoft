using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CashAccount : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public decimal CurrentBalance { get; set; }
}
