using System.ComponentModel.DataAnnotations;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductComponent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? ConsumptionUnitId { get; set; }
    [MaxLength(60)] public string Code { get; set; } = string.Empty;
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string ProductionPhase { get; set; } = string.Empty;
    [MaxLength(80)] public string WarehouseDeliveryRole { get; set; } = string.Empty;
    public decimal DefaultConsumption { get; set; }
    public bool ActivateForAllProducts { get; set; }
    public bool ShowOnProductionCard { get; set; }
    [MaxLength(1200)] public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(120)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(120)] public string? UpdatedBy { get; set; }

    public Unit? ConsumptionUnit { get; set; }
}
