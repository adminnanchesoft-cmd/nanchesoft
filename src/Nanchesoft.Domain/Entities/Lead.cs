using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Lead : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Source { get; set; } = "manual";
    public string Status { get; set; } = "new";

    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Rfc { get; set; }

    public string City { get; set; } = string.Empty;
    public string StateRegion { get; set; } = string.Empty;
    public decimal? EstimatedRevenue { get; set; }

    public Guid? OwnerUserId { get; set; }
    public int Score { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? Notes { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
}
