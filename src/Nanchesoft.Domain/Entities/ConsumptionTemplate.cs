using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ConsumptionTemplate : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ProductStyleId { get; set; }
    public ProductStyle? ProductStyle { get; set; }

    public Guid ProductSizeRunId { get; set; }
    public ProductSizeRun? ProductSizeRun { get; set; }

    public bool IsAuthorized { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public string? AuthorizedBy { get; set; }

    public string Notes { get; set; } = string.Empty;

    public ICollection<ConsumptionTemplateDetail> Details { get; set; } = new List<ConsumptionTemplateDetail>();
}
