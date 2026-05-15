namespace Nanchesoft.Domain.Entities;

public sealed class ProductTechnicalSheet
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FinishedProductId { get; set; }
    public Guid? ProductStyleId { get; set; }
    public string SheetCode { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string ProductDisplayName { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public string MainMaterialName { get; set; } = string.Empty;
    public string MainColorName { get; set; } = string.Empty;
    public string SizeRunCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public List<ProductTechnicalSheetMaterial> Materials { get; set; } = new();
    public List<ProductTechnicalSheetProcess> Processes { get; set; } = new();
}
