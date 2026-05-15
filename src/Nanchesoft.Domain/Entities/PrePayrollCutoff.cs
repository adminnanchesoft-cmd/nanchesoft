using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PrePayrollCutoff : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public string CutoffCode { get; set; } = string.Empty;
    public string CutoffName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int EmployeesReviewed { get; set; }
    public int IncidentsDetected { get; set; }
    public decimal WorkedDaysTotal { get; set; }
    public decimal OvertimeHoursTotal { get; set; }
    public string Status { get; set; } = "draft";
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}
