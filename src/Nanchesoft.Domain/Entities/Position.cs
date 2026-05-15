using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Position : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PayrollGroup { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
}
