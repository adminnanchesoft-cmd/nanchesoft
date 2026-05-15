using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class UserSession : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}