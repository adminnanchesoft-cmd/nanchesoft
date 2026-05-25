using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionOrder : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string Folio { get; set; } = string.Empty;
    public string WeekCode { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly DeliveryDate { get; set; }

    public string Status { get; set; } = "draft";
    // draft | planned | exploded | reserved | in_progress | completed | closed | cancelled

    public int Priority { get; set; } = 1;
    // 1=normal | 2=alta | 3=urgente

    public string Notes { get; set; } = string.Empty;

    public int TotalUnitsPlanned { get; set; }
    public int TotalUnitsProduced { get; set; }
    public int TotalUnitsShipped { get; set; }

    public string ExplosionStatus { get; set; } = "pending";
    // pending | calculated | with_shortages | complete

    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public DateOnly? CancellationDate { get; set; }

    // Commercial fields stored at order level
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    // Shipping & commercial identity fields
    public Guid? WarehouseId { get; set; }
    public Guid? ShipToAddressId { get; set; }
    public string ShipToAddressText { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;

    public ICollection<ProductionOrderLine> Lines { get; set; } = new List<ProductionOrderLine>();
    public ICollection<MaterialRequirement> MaterialRequirements { get; set; } = new List<MaterialRequirement>();
    public ICollection<ProductionPhaseProgress> PhaseProgress { get; set; } = new List<ProductionPhaseProgress>();
    public ICollection<ProductionVoucher> Vouchers { get; set; } = new List<ProductionVoucher>();
}
