using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SilvaSoftSyncLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }

    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RecordsRead { get; set; }
    public int RecordsImported { get; set; }
    public int RecordsSkipped { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public string? TriggeredBy { get; set; }
}
