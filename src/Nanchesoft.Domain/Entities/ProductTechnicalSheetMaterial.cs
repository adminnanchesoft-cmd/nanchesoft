namespace Nanchesoft.Domain.Entities;

public sealed class ProductTechnicalSheetMaterial
{
    public Guid Id { get; set; }
    public Guid ProductTechnicalSheetId { get; set; }
    public Guid MaterialItemId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal WastePercent { get; set; }
    public int SortOrder { get; set; }
    public bool ShowOnTechnicalSheet { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}
