using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class HrEmployeeImportLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ConflictMode { get; set; } = "update"; // update | skip | error

    public int TotalRows { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }

    public string Errors { get; set; } = "[]";       // jsonb
    public string Duplicates { get; set; } = "[]";   // jsonb

    public bool Success { get; set; }
    public bool RolledBack { get; set; }

    public string ExecutedBy { get; set; } = "sistema";
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public int DurationMs { get; set; }
}
