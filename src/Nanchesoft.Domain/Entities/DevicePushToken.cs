using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class DevicePushToken : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid UserId { get; set; }

    public string Platform { get; set; } = "android";
    public string Token { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? AppVersion { get; set; }

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool RevokedFlag { get; set; }
}
