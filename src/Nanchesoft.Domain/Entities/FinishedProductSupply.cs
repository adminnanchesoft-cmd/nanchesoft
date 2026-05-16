using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class FinishedProductSupply : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }

    public Guid FinishedProductId { get; set; }
    public FinishedProduct? FinishedProduct { get; set; }

    public Guid ProductComponentId { get; set; }
    public ProductComponent? ProductComponent { get; set; }

    public bool IsAuthorized { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public string? AuthorizedBy { get; set; }

    public string Notes { get; set; } = string.Empty;

    public ICollection<FinishedProductSupplySize> Sizes { get; set; } = new List<FinishedProductSupplySize>();
}
