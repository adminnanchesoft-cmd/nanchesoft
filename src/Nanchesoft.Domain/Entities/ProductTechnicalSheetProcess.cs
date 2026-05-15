namespace Nanchesoft.Domain.Entities;

public sealed class ProductTechnicalSheetProcess
{
    public Guid Id { get; set; }
    public Guid ProductTechnicalSheetId { get; set; }
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string WorkstationCode { get; set; } = string.Empty;
    public string DeliverToWarehouseCode { get; set; } = string.Empty;
    public bool RequiresVoucherCard { get; set; }
    public bool ShowMaterialsOnVoucher { get; set; }
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
}
