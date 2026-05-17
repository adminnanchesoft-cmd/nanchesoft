using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class QualityControlRecord : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public string Folio { get; set; } = string.Empty;
    public DateOnly InspectionDate { get; set; }

    public string InspectorName { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";
    // pending | approved | rejected | on_hold

    public string Result { get; set; } = string.Empty;
    // approved | rejected | conditional

    public int TotalUnitsInspected { get; set; }
    public int TotalUnitsApproved { get; set; }
    public int TotalUnitsRejected { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }

    public ICollection<QualityDefect> Defects { get; set; } = new List<QualityDefect>();
}
