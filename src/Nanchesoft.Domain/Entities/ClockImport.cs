using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ClockImport : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? ClockImportMappingId { get; set; }
    public ClockImportMapping? Mapping { get; set; }

    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileFormat { get; set; } = string.Empty;

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public string ImportedBy { get; set; } = string.Empty;

    public int RowsRead { get; set; }
    public int RowsCreated { get; set; }
    public int RowsSkipped { get; set; }
    public int RowsError { get; set; }

    // Status: Pending | Processing | Done | Error
    public string Status { get; set; } = "Done";
    public string ErrorSummary { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
