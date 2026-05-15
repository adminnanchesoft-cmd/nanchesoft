using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Tax : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string TaxType { get; set; } = "Traslado";
    public bool IsDefault { get; set; }
}
