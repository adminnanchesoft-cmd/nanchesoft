using System;

namespace Nanchesoft.Domain.Entities;

public class PaymentApplication
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid PurchaseInvoiceId { get; set; }
    public DateTime ApplicationDate { get; set; }
    public decimal AppliedAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
