using System.ComponentModel.DataAnnotations;

namespace Nanchesoft.Domain.Entities;

public sealed class FinishedProductMaterial
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FinishedProductId { get; set; }
    public Guid ProductComponentId { get; set; }
    public Guid MaterialItemId { get; set; }
    [MaxLength(40)] public string SizeCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public bool IsRequired { get; set; } = true;
    [MaxLength(1200)] public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(120)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(120)] public string? UpdatedBy { get; set; }

    public FinishedProduct? FinishedProduct { get; set; }
    public ProductComponent? ProductComponent { get; set; }
    public MaterialItem? MaterialItem { get; set; }
}
