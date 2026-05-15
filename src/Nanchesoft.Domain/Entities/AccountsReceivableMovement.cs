using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class AccountsReceivableMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid AccountsReceivableAccountId { get; set; }
    public AccountsReceivableAccount? AccountsReceivableAccount { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public Guid? CreditNoteId { get; set; }
    public CreditNote? CreditNote { get; set; }

    public Guid? ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public DateTime MovementDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = "posted";
    public decimal ChargeAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAfter { get; set; }
}
