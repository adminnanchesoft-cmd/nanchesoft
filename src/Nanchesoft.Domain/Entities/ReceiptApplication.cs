using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ReceiptApplication : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public Guid? AccountsReceivableMovementId { get; set; }
    public AccountsReceivableMovement? AccountsReceivableMovement { get; set; }

    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow.Date;
    public decimal AppliedAmount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = "posted";
}
