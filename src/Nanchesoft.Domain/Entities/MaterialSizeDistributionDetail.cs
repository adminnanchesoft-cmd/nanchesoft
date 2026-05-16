using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialSizeDistributionDetail : BaseEntity
{
    public Guid MaterialSizeDistributionId { get; set; }
    public MaterialSizeDistribution? Distribution { get; set; }

    public Guid ProductSizeRunSizeId { get; set; }
    public ProductSizeRunSize? SizeRunSize { get; set; }

    public Guid? MaterialItemId { get; set; }
    public MaterialItem? MaterialItem { get; set; }

    public string Notes { get; set; } = string.Empty;
}
