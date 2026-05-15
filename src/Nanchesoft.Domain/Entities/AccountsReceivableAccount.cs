using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class AccountsReceivableAccount : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = "active";

    public decimal TotalCharges { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime? LastMovementAt { get; set; }

    public ICollection<AccountsReceivableMovement> Movements { get; set; } = new List<AccountsReceivableMovement>();
}
