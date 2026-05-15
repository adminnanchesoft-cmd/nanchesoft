using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class AttendanceDailySummary : BaseEntity
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

    public DateTime WorkDate { get; set; }
    public DateTime? ScheduledEntryTime { get; set; }
    public DateTime? ScheduledExitTime { get; set; }
    public DateTime? FirstPunchDateTime { get; set; }
    public DateTime? LastPunchDateTime { get; set; }
    public decimal WorkedHours { get; set; }
    public int DelayMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal AbsenceUnits { get; set; }
    public string DayType { get; set; } = "workday";
    public string Status { get; set; } = "calculated";
    public string Source { get; set; } = "time-clock";
    public string Notes { get; set; } = string.Empty;
}
