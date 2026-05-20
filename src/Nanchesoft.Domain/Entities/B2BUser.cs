using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class B2BUser : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid B2BAccountId { get; set; }
    public B2BAccount? B2BAccount { get; set; }

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "buyer";

    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public string? Locale { get; set; }
    public string? Timezone { get; set; }
}
