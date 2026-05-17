using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PieceWorkRecord : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? ProductionVoucherId { get; set; }
    public ProductionVoucher? ProductionVoucher { get; set; }

    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public Guid ProductionPhaseId { get; set; }
    public ProductionPhase? ProductionPhase { get; set; }

    public Guid? PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public DateOnly WorkDate { get; set; }
    public int UnitsProduced { get; set; }
    public int UnitsRejected { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal QualityDeduction { get; set; }
    public decimal NetAmount { get; set; }

    public string Status { get; set; } = "pending";
    // pending | approved | processed | cancelled

    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public string Notes { get; set; } = string.Empty;
}
