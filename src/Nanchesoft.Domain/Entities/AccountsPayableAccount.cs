using System;

namespace Nanchesoft.Domain.Entities;

public class AccountsPayableAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? CurrencyId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public DateTime? LastMovementAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
