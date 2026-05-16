using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ConsumptionTemplateDetail : BaseEntity
{
    public Guid ConsumptionTemplateId { get; set; }
    public ConsumptionTemplate? ConsumptionTemplate { get; set; }

    public Guid ProductComponentId { get; set; }
    public ProductComponent? ProductComponent { get; set; }

    public int Pieces { get; set; }

    public string DispersionMode { get; set; } = "paired";

    public string Notes { get; set; } = string.Empty;

    public ICollection<ConsumptionTemplateSize> Sizes { get; set; } = new List<ConsumptionTemplateSize>();
}
