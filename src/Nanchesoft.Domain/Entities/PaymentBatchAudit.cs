using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PaymentBatchAudit : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid PaymentBatchId { get; set; }
    public PaymentBatch? PaymentBatch { get; set; }
    public Guid? PaymentBatchLineId { get; set; }
    public PaymentBatchLine? PaymentBatchLine { get; set; }

    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    // created | updated | line_added | line_removed | line_modified |
    // authorized | rejected | partially_authorized | executed | reverted

    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
