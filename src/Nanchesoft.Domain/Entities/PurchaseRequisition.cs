using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PurchaseRequisition : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? SeriesId { get; set; }
    public DocumentSeries? Series { get; set; }

    public DateTime RequisitionDate { get; set; } = DateTime.UtcNow.Date;
    public string Folio { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }

    public ICollection<PurchaseRequisitionLine> Lines { get; set; } = new List<PurchaseRequisitionLine>();
}
