using System.ComponentModel.DataAnnotations.Schema;
using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeeIncident : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public Guid PayrollIncidentTypeId { get; set; }
    public NomPayrollIncidentType? PayrollIncidentType { get; set; }

    [NotMapped]
    public Guid? NomPayrollIncidentTypeId
    {
        get => PayrollIncidentTypeId == Guid.Empty ? null : PayrollIncidentTypeId;
        set => PayrollIncidentTypeId = value ?? Guid.Empty;
    }

    [NotMapped]
    public NomPayrollIncidentType? NomPayrollIncidentType
    {
        get => PayrollIncidentType;
        set => PayrollIncidentType = value;
    }

    public Guid? RecurrentRuleId { get; set; }
    public HrRecurringIncidentRule? RecurrentRule { get; set; }

    public DateTime IncidentDate { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
