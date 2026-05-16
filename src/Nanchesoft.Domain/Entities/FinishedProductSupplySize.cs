using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class FinishedProductSupplySize : BaseEntity
{
    public Guid FinishedProductSupplyId { get; set; }
    public FinishedProductSupply? Supply { get; set; }

    public Guid ProductSizeRunSizeId { get; set; }
    public ProductSizeRunSize? SizeRunSize { get; set; }

    public Guid? MaterialItemId { get; set; }
    public MaterialItem? MaterialItem { get; set; }

    public string Notes { get; set; } = string.Empty;
}
