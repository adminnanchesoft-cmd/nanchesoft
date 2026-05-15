using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Company : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Timezone { get; set; } = "America/Mexico_City";

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}