using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollRunClosing : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public string ClosingCode { get; set; } = string.Empty;
    public DateTime ClosingDate { get; set; } = DateTime.UtcNow;
    public int EmployeesIncluded { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int SourceApplicationsCount { get; set; }
    public int ReceiptsGeneratedCount { get; set; }
    public int IssuesDetected { get; set; }
    public string Status { get; set; } = "draft";
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
