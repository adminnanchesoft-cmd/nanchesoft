using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

/// <summary>
/// Fase / operación productiva (equivalente a Fraccion de Silvasoft Orange).
/// Representa una operación dentro del proceso de fabricación: cortar, pespunte, montar, pegar, etc.
/// </summary>
public sealed class ProductionPhase : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Sequence { get; set; }

    // Clave numérica corta visible (Fraccion.Clave en Orange)
    public int ClaveNumber { get; set; }

    // Costo base y tipo
    public decimal BaseCost { get; set; }
    public string CostType { get; set; } = "fixed"; // fixed | variable | percent

    // Agrupación (autoreferencia opcional a fase "padre")
    public Guid? PhaseGroupId { get; set; }
    public ProductionPhase? PhaseGroup { get; set; }

    // Flags de comportamiento (de Fraccion en Orange)
    public bool ShowOnProductionCard { get; set; }
    public bool PrintBarcode { get; set; }
    public bool GenerateForAllProducts { get; set; }
    public bool RegistersProgress { get; set; }
    public bool IsPayable { get; set; }
    public bool IsPieceWorkPayable { get; set; }
    public bool GenerateFromTransferOut { get; set; }
    public bool AppliesToAllProducts { get; set; }
    public bool TracksProducedQuantity { get; set; }
    public bool IncludeInProjection { get; set; }
    public bool AffectsInventory { get; set; }
    public bool TracksQuantityInTransferOut { get; set; }

    // Sucursal heredada (Orange ClaveSucursal)
    public int BranchKey { get; set; }

    // FKs opcionales
    public Guid? PieceWorkLocationId { get; set; }
    public Warehouse? PieceWorkLocation { get; set; }

    public Guid? FactoryBranchId { get; set; }
    public Branch? FactoryBranch { get; set; }

    public Guid? PrePayrollClassificationId { get; set; }

    public Guid? ManufacturingTypeId { get; set; }
    public ProductManufacturingType? ManufacturingType { get; set; }

    // Mapeo Silvasoft (para sincronización)
    public Guid? SilvasoftFraccionId { get; set; }

    /// <summary>Texto computado: "Clave Nombre"</summary>
    public string DisplayText => $"{ClaveNumber} {Name}".Trim();
}
