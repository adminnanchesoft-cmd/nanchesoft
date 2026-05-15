using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ItemEngineeringProfile : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public Guid? ProductStyleId { get; set; }
    public ProductStyle? ProductStyle { get; set; }

    public Guid? ProductSizeRunId { get; set; }
    public ProductSizeRun? ProductSizeRun { get; set; }

    public Guid? EmbroideryPatternId { get; set; }
    public EmbroideryPattern? EmbroideryPattern { get; set; }

    public Guid? PrimaryMaterialItemId { get; set; }
    public Item? PrimaryMaterialItem { get; set; }

    public string FolioPattern { get; set; } = string.Empty;
    public string TechnicalSheetMode { get; set; } = "style";
    public string ProcessVoucherProfile { get; set; } = string.Empty;
    public string TechnicalSheetNotes { get; set; } = string.Empty;
    public string ProductionCardNotes { get; set; } = string.Empty;
    public bool HasPhoto { get; set; }
    public bool HasConsumptionDefinition { get; set; }
    public bool HasMaterialAssignments { get; set; }
    public bool IsAuthorizedForExplosion { get; set; }
}
