namespace Nanchesoft.Domain.Entities;

public sealed class ProductAuthorizationRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FinishedProductId { get; set; }
    public Guid? ProductTechnicalSheetId { get; set; }
    public Guid? ProductCostSheetId { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public bool RequiresPhoto { get; set; } = true;
    public bool RequiresConsumption { get; set; } = true;
    public bool RequiresMaterialAssignment { get; set; } = true;
    public bool RequiresCostSheet { get; set; } = true;
    public bool HasPhoto { get; set; }
    public bool HasConsumption { get; set; }
    public bool HasMaterialAssignment { get; set; }
    public bool HasCostSheet { get; set; }
    public DateTime? AuthorizedAtUtc { get; set; }
    public string AuthorizedBy { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
