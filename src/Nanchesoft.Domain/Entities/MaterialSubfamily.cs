using System.ComponentModel.DataAnnotations;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialSubfamily
{
    public const string DirectMaterialType = "direct";
    public const string IndirectMaterialType = "indirect";

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid MaterialFamilyId { get; set; }
    [MaxLength(40)] public string Code { get; set; } = string.Empty;
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string MaterialType { get; set; } = DirectMaterialType;
    public bool IsDirectMaterial { get; set; } = true;
    [MaxLength(1200)] public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(120)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(120)] public string? UpdatedBy { get; set; }

    public MaterialFamily? MaterialFamily { get; set; }
    public ICollection<MaterialItem> MaterialItems { get; set; } = new List<MaterialItem>();

    public bool IsIndirectMaterial => !IsDirectMaterial;

    public void NormalizeClassification()
    {
        MaterialType = NormalizeMaterialType(MaterialType, IsDirectMaterial);
        IsDirectMaterial = MaterialType == DirectMaterialType;
    }

    public static string NormalizeMaterialType(string? materialType, bool? fallbackIsDirect = null)
    {
        var normalized = string.IsNullOrWhiteSpace(materialType)
            ? string.Empty
            : materialType.Trim().ToLowerInvariant();

        return normalized switch
        {
            DirectMaterialType or "directo" or "materia_prima" or "materia-prima" or "material-directo" => DirectMaterialType,
            IndirectMaterialType or "indirecto" or "gasto" or "consumible" or "material-indirecto" => IndirectMaterialType,
            _ when fallbackIsDirect.HasValue => fallbackIsDirect.Value ? DirectMaterialType : IndirectMaterialType,
            _ => DirectMaterialType
        };
    }
}
