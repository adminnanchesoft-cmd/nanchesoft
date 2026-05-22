using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class HrRecurringIncidentRule : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid NomPayrollIncidentTypeId { get; set; }
    public NomPayrollIncidentType? NomPayrollIncidentType { get; set; }

    public decimal Amount { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Frequency { get; set; } = "cada_periodo";
    public string Notes { get; set; } = string.Empty;
    public bool RequiresAuthorization { get; set; }
    public string? AuthorizedBy { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
