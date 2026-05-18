using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class WorkSchedule : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? WorkShiftId { get; set; }
    public WorkShift? WorkShift { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public string EntryTime { get; set; } = string.Empty;
    public int ToleranceMinutes { get; set; }
    public string LunchStartTime { get; set; } = string.Empty;
    public string LunchEndTime { get; set; } = string.Empty;
    public string ExitTime { get; set; } = string.Empty;
    public decimal WeeklyHours { get; set; }
    public bool IsFlexible { get; set; }
    public string Notes { get; set; } = string.Empty;
}
