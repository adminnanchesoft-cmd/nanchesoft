using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SellerVisit : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid SellerRouteId { get; set; }
    public SellerRoute? SellerRoute { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? ActivityId { get; set; }
    public CrmActivity? Activity { get; set; }

    public int Sequence { get; set; }
    public DateTime PlannedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? LeftAt { get; set; }

    public decimal? GeoLat { get; set; }
    public decimal? GeoLng { get; set; }
    public string? CheckInMethod { get; set; }

    public string Status { get; set; } = "planned";
    public string? OutcomeTag { get; set; }
}
