using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialRequirement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string? CalculatedBy { get; set; }

    public string Status { get; set; } = "draft";
    // draft | confirmed | with_shortages | reserved | cancelled

    public int TotalLines { get; set; }
    public int LinesWithShortage { get; set; }
    public int LinesFulyCovered { get; set; }

    public string Notes { get; set; } = string.Empty;

    public ICollection<MaterialRequirementLine> Lines { get; set; } = new List<MaterialRequirementLine>();
}
