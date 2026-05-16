using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ConsumptionTemplateSize : BaseEntity
{
    public Guid ConsumptionTemplateDetailId { get; set; }
    public ConsumptionTemplateDetail? ConsumptionTemplateDetail { get; set; }

    public Guid ProductSizeRunSizeId { get; set; }
    public ProductSizeRunSize? ProductSizeRunSize { get; set; }

    public decimal Consumption { get; set; }
}
