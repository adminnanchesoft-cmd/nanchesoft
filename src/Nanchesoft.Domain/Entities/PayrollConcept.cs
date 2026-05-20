using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollConcept : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public string SatCode { get; set; } = string.Empty;
    public string SatAgrupador { get; set; } = string.Empty;
    public string TaxableType { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public bool IsAutomatic { get; set; } = true;
    public bool PrintOnReceipt { get; set; } = true;
    public decimal TaxablePercent { get; set; } = 100m;
    public decimal ExemptPercent { get; set; } = 0m;
    public int SortOrder { get; set; }

    public string Formula { get; set; } = string.Empty;
    public string TaxableFormula { get; set; } = string.Empty;
    public string ExemptFormula { get; set; } = string.Empty;
    public string ImssTaxableFormula { get; set; } = string.Empty;
    public string SatTipoPercepcionCode { get; set; } = string.Empty;
    public string SatTipoDeduccionCode { get; set; } = string.Empty;
    public string SatTipoOtroPagoCode { get; set; } = string.Empty;
    public bool AutomaticOnGlobalRun { get; set; }
    public bool AutomaticOnTermination { get; set; }
    public bool IsInKind { get; set; }
    public bool AffectsSeventhDay { get; set; }
    public bool AffectsHolidayPay { get; set; }
    public bool AffectsImss { get; set; } = true;
    public bool AffectsIsr { get; set; } = true;
    public bool AffectsAccumulators { get; set; } = true;
    public bool RequiresSatStamping { get; set; } = true;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
}
