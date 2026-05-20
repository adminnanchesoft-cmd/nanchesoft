using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SellerRoute : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid SellerUserId { get; set; }

    public DateOnly DayDate { get; set; }
    public string Status { get; set; } = "planned";

    public decimal? StartLat { get; set; }
    public decimal? StartLng { get; set; }
    public decimal? EndLat { get; set; }
    public decimal? EndLng { get; set; }

    public int? OdometerStart { get; set; }
    public int? OdometerEnd { get; set; }

    public ICollection<SellerVisit> Visits { get; set; } = new List<SellerVisit>();
}
