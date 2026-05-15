using System;

namespace Nanchesoft.Domain.Entities;

public class AccountsPayableMovement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid AccountsPayableAccountId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public DateTime MovementDate { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Reference { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
