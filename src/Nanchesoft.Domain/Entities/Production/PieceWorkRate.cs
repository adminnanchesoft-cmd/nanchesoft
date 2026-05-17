using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PieceWorkRate : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ProductionPhaseId { get; set; }
    public ProductionPhase? ProductionPhase { get; set; }

    public DateOnly EffectiveDate { get; set; }
    public decimal PricePerUnit { get; set; }
    public string Notes { get; set; } = string.Empty;
}
