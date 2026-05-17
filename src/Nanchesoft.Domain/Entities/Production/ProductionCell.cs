using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionCell : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid ProductionPhaseId { get; set; }
    public ProductionPhase? ProductionPhase { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CapacityPerDay { get; set; }
    public int CapacityPerWeek { get; set; }
    public string Notes { get; set; } = string.Empty;

    public ICollection<ProductionCellEmployee> CellEmployees { get; set; } = new List<ProductionCellEmployee>();
}
