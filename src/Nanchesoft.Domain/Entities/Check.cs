using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Check : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public Guid? CheckBookId { get; set; }
    public CheckBook? CheckBook { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string Folio { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? PostingDate { get; set; }
    public DateTime? CashedDate { get; set; }
    public DateTime? CancelDate { get; set; }
    public string BeneficiaryType { get; set; } = "other"; // supplier | employee | other
    public string BeneficiaryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending | printed | issued | cashed | cancelled | bounced
    public bool IsPrinted { get; set; }
    public DateTime? PrintedAt { get; set; }
    public Guid? BankMovementId { get; set; }
    public BankMovement? BankMovement { get; set; }
    public string Notes { get; set; } = string.Empty;
}
