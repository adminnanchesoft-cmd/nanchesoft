using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class NomPayrollIncidentType : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IncidentCategory { get; set; } = string.Empty;
    public string AffectType { get; set; } = string.Empty;
    public string PayrollConceptType { get; set; } = string.Empty;
    public Guid? PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }
    public string SatCode { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsDiscount { get; set; }
    public bool IsPerception { get; set; }
    public bool IsInformative { get; set; }
    public bool RequiresAmount { get; set; }
    public bool RequiresQuantity { get; set; }
    public bool RequiresAuthorization { get; set; }
    public bool AppliesToPayroll { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
