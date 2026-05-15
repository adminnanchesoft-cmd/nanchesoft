using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollAccountingPosting : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public string PostingCode { get; set; } = string.Empty;
    public DateTime PostingDate { get; set; } = DateTime.UtcNow;
    public string LedgerBook { get; set; } = string.Empty;
    public string JournalNumber { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public int LinesCount { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? ExportedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public string ExportReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
