using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

/// <summary>
/// Restricciones de fase por producto o estilo (equivalente a NoPasa_Estilo_Fases).
/// Impide programar un producto en una fase específica.
/// </summary>
public sealed class PhaseRestriction : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? FinishedProductId { get; set; }
    public FinishedProduct? FinishedProduct { get; set; }

    public Guid? ProductStyleId { get; set; }
    public ProductStyle? ProductStyle { get; set; }

    public Guid ProductionPhaseId { get; set; }
    public ProductionPhase? ProductionPhase { get; set; }

    public string RestrictionType { get; set; } = "style_phase";
    // style_phase | sole_phase | product_phase

    public string Reason { get; set; } = string.Empty;
}
