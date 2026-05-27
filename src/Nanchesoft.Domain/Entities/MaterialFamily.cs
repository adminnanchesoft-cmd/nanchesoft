using System.ComponentModel.DataAnnotations;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialFamily
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    [MaxLength(40)] public string Code { get; set; } = string.Empty;
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string InventoryGroup { get; set; } = string.Empty;
    [MaxLength(1200)] public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? SilvaSoftComposicionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(120)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(120)] public string? UpdatedBy { get; set; }

    public ICollection<MaterialSubfamily> Subfamilies { get; set; } = new List<MaterialSubfamily>();
}
