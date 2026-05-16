using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialSizeDistribution : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }

    public Guid MaterialSubfamilyId { get; set; }
    public MaterialSubfamily? MaterialSubfamily { get; set; }

    public Guid ProductSizeRunId { get; set; }
    public ProductSizeRun? ProductSizeRun { get; set; }

    public Guid? ProductLastId { get; set; }
    public ProductLast? ProductLast { get; set; }

    public string Notes { get; set; } = string.Empty;

    public ICollection<MaterialSizeDistributionDetail> Details { get; set; } = new List<MaterialSizeDistributionDetail>();
}
