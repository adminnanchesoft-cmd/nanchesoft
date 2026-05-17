using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionVoucher : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public Guid ProductionOrderLineId { get; set; }
    public ProductionOrderLine? ProductionOrderLine { get; set; }

    public Guid ProductionPhaseId { get; set; }
    public ProductionPhase? ProductionPhase { get; set; }

    public Guid? ProductionCellId { get; set; }
    public ProductionCell? ProductionCell { get; set; }

    public string Folio { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public int BatchSize { get; set; }

    public string Status { get; set; } = "issued";
    // issued | in_progress | completed | cancelled

    public DateOnly IssuedDate { get; set; }
    public string? IssuedBy { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string? CompletedBy { get; set; }
    public DateOnly? CancelledDate { get; set; }
    public string? CancelledReason { get; set; }

    public bool Printed { get; set; }
    public DateTime? PrintedAt { get; set; }
    public int PrintCount { get; set; }

    public string Notes { get; set; } = string.Empty;

    public ICollection<ProductionVoucherDetail> Details { get; set; } = new List<ProductionVoucherDetail>();
    public ICollection<PieceWorkRecord> PieceWorkRecords { get; set; } = new List<PieceWorkRecord>();
}
