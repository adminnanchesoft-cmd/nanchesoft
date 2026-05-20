using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PaymentBatchLine : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid PaymentBatchId { get; set; }
    public PaymentBatch? PaymentBatch { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string SupplierName { get; set; } = string.Empty;

    public Guid? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public string InvoiceFolio { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; } = 1m;

    public decimal OriginalAmount { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountToPay { get; set; }

    public string Priority { get; set; } = "normal"; // high | normal | low | critical
    public string PaymentType { get; set; } = "transfer";
    // transfer | check | spei | deposit | cash | card | offset | other

    public Guid? BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public Guid? CashAccountId { get; set; }
    public CashAccount? CashAccount { get; set; }
    public Guid? CheckBookId { get; set; }
    public CheckBook? CheckBook { get; set; }

    public DateTime? ScheduledDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public string LineStatus { get; set; } = "pending";
    // pending | authorized | rejected | executed | cancelled

    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public Guid? CheckId { get; set; }
    public Check? Check { get; set; }
    public Guid? BankMovementId { get; set; }
    public BankMovement? BankMovement { get; set; }

    public string ExecutedFolio { get; set; } = string.Empty;
    public DateTime? ExecutedAt { get; set; }
    public string RejectedReason { get; set; } = string.Empty;
}
