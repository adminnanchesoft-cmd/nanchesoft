using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollDispersionBatch : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public string BatchCode { get; set; } = string.Empty;
    public DateTime DispersionDate { get; set; } = DateTime.UtcNow;
    public string LayoutFormat { get; set; } = "spei";
    public string BankName { get; set; } = string.Empty;
    public string FundingAccount { get; set; } = string.Empty;
    public int BeneficiariesCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExportedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string FileReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
