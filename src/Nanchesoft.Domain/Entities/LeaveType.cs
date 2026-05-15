using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class LeaveType : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool WithPay { get; set; }
    public bool ImpactsPayroll { get; set; }
    public decimal DefaultDays { get; set; }
    public string Notes { get; set; } = string.Empty;
}
