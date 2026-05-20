using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Opportunity : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Stage { get; set; } = "prospect";
    public string? ForecastCategory { get; set; }
    public string? LostReason { get; set; }

    public decimal ExpectedAmount { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public int Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Guid? OwnerUserId { get; set; }
}
