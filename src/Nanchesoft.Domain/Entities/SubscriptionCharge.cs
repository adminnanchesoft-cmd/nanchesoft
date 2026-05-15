using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SubscriptionCharge : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    public string ChargeMonth { get; set; } = string.Empty;
    public int BillingYear { get; set; }
    public int BillingMonth { get; set; }

    public string TenantCodeSnapshot { get; set; } = string.Empty;
    public string TenantNameSnapshot { get; set; } = string.Empty;
    public string PlanCodeSnapshot { get; set; } = string.Empty;
    public string PlanNameSnapshot { get; set; } = string.Empty;

    public DateTime ChargeDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date;

    public decimal PlanPriceMonthly { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CompensationAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    public DateTime? PaidAt { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string Notes { get; set; } = string.Empty;
}
