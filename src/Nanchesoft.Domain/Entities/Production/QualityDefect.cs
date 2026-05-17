using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class QualityDefect : BaseEntity
{
    public Guid QualityControlRecordId { get; set; }
    public QualityControlRecord? QualityControlRecord { get; set; }

    public string DefectCode { get; set; } = string.Empty;
    public string DefectDescription { get; set; } = string.Empty;

    public string Severity { get; set; } = "low";
    // low | medium | high | critical

    public int QuantityAffected { get; set; }

    public string ResolutionNotes { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}
