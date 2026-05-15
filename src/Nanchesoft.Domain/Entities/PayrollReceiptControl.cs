using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollReceiptControl : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid PayrollRunLineId { get; set; }
    public PayrollRunLine? PayrollRunLine { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;
    public string ReceiptStatus { get; set; } = "generated";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? StampedAt { get; set; }
    public string DeliveryChannel { get; set; } = string.Empty;
    public string DeliveryReference { get; set; } = string.Empty;
    public string AckBy { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}
