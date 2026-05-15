using System.ComponentModel.DataAnnotations;

namespace Nanchesoft.Domain.Entities;

public sealed class FinishedProduct
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? ProductStyleId { get; set; }
    public Guid? ItemModelId { get; set; }
    public Guid? ItemBrandId { get; set; }
    public Guid? ProductLeatherTypeId { get; set; }
    public Guid? ProductColorId { get; set; }
    public Guid? ProductToeCapId { get; set; }
    public Guid? ProductSoleId { get; set; }
    public Guid? ProductSoleColorId { get; set; }
    public Guid? ProductFolioPatternId { get; set; }
    public Guid? ProductSizeRunId { get; set; }
    public Guid? ProductLineId { get; set; }
    public Guid? ProductLastId { get; set; }
    public Guid? MainMaterialItemId { get; set; }
    [MaxLength(60)] public string Code { get; set; } = string.Empty;
    [MaxLength(220)] public string? Name { get; set; }
    [MaxLength(220)] public string BillingName { get; set; } = string.Empty;
    public bool HasPhoto { get; set; }
    public bool HasConsumptionDefinition { get; set; }
    public bool HasMaterialAssignments { get; set; }
    public bool IsAuthorizedForExplosion { get; set; }
    [MaxLength(1200)] public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(120)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(120)] public string? UpdatedBy { get; set; }

    public ProductStyle? ProductStyle { get; set; }
    public ItemModel? ItemModel { get; set; }
    public ItemBrand? ItemBrand { get; set; }
    public ProductLeatherType? ProductLeatherType { get; set; }
    public ProductColor? ProductColor { get; set; }
    public ProductToeCap? ProductToeCap { get; set; }
    public ProductSole? ProductSole { get; set; }
    public ProductSoleColor? ProductSoleColor { get; set; }
    public ProductFolioPattern? ProductFolioPattern { get; set; }
    public ProductSizeRun? ProductSizeRun { get; set; }
    public ProductLine? ProductLine { get; set; }
    public ProductLast? ProductLast { get; set; }
    public MaterialItem? MainMaterialItem { get; set; }
}
