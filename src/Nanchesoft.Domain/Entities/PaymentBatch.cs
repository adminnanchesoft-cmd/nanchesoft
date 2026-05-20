using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PaymentBatch : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Folio { get; set; } = string.Empty;
    public DateTime BatchDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ScheduledDate { get; set; }

    public string Status { get; set; } = "draft";
    // draft | pending | in_review | authorized | rejected | executed | partially_executed | cancelled

    public int LineCount { get; set; }
    public int CompanyCount { get; set; }
    public int SupplierCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AuthorizedAmount { get; set; }
    public decimal ExecutedAmount { get; set; }

    public string Notes { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";

    public Guid? RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;

    public Guid? AuthorizedByUserId { get; set; }
    public string AuthorizedByName { get; set; } = string.Empty;
    public DateTime? AuthorizedAt { get; set; }

    public string RejectedReason { get; set; } = string.Empty;
    public DateTime? RejectedAt { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public string RejectedByName { get; set; } = string.Empty;

    public DateTime? ExecutedAt { get; set; }
    public Guid? ExecutedByUserId { get; set; }
    public string ExecutedByName { get; set; } = string.Empty;

    public ICollection<PaymentBatchLine> Lines { get; set; } = new List<PaymentBatchLine>();
}
