using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ErrorLog : BaseEntity
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
}