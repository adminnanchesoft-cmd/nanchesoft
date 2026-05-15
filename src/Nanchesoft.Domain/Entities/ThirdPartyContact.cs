using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ThirdPartyContact : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
