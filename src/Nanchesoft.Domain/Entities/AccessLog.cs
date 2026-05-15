using Nanchesoft.Domain.Common;
using Nanchesoft.Domain.Enums;

namespace Nanchesoft.Domain.Entities;

public sealed class AccessLog : BaseEntity
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }

    public AccessEventType EventType { get; set; }
    public string EventResult { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
}