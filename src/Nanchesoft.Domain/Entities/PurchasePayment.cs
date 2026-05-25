using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PurchasePayment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid PurchaseReceiptId { get; set; }
    public PurchaseReceipt? PurchaseReceipt { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;
    public string Folio { get; set; } = string.Empty;

    // "transfer" | "cash" | "check" | "card" | "other"
    public string PaymentMethod { get; set; } = "transfer";

    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // draft | posted | cancelled
    public string Status { get; set; } = "posted";

    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
}
