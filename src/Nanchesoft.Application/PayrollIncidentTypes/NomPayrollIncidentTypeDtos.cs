namespace Nanchesoft.Application.PayrollIncidentTypes;

public class NomPayrollIncidentTypeRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? IncidentCategory { get; set; }
    public string? AffectType { get; set; }
    public string? PayrollConceptType { get; set; }
    public string? SatCode { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsDiscount { get; set; }
    public bool IsPerception { get; set; }
    public bool IsInformative { get; set; }
    public bool RequiresAmount { get; set; }
    public bool RequiresQuantity { get; set; }
    public bool RequiresAuthorization { get; set; }
    public bool AppliesToPayroll { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class NomPayrollIncidentTypeDto : NomPayrollIncidentTypeRequest
{
    public Guid NomPayrollIncidentTypeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
