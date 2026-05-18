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

    // Monday
    public string MonEntryTime { get; set; } = string.Empty;
    public int MonToleranceMinutes { get; set; }
    public string MonLunchStartTime { get; set; } = string.Empty;
    public string MonLunchEndTime { get; set; } = string.Empty;
    public string MonExitTime { get; set; } = string.Empty;

    // Tuesday
    public string TueEntryTime { get; set; } = string.Empty;
    public int TueToleranceMinutes { get; set; }
    public string TueLunchStartTime { get; set; } = string.Empty;
    public string TueLunchEndTime { get; set; } = string.Empty;
    public string TueExitTime { get; set; } = string.Empty;

    // Wednesday
    public string WedEntryTime { get; set; } = string.Empty;
    public int WedToleranceMinutes { get; set; }
    public string WedLunchStartTime { get; set; } = string.Empty;
    public string WedLunchEndTime { get; set; } = string.Empty;
    public string WedExitTime { get; set; } = string.Empty;

    // Thursday
    public string ThuEntryTime { get; set; } = string.Empty;
    public int ThuToleranceMinutes { get; set; }
    public string ThuLunchStartTime { get; set; } = string.Empty;
    public string ThuLunchEndTime { get; set; } = string.Empty;
    public string ThuExitTime { get; set; } = string.Empty;

    // Friday
    public string FriEntryTime { get; set; } = string.Empty;
    public int FriToleranceMinutes { get; set; }
    public string FriLunchStartTime { get; set; } = string.Empty;
    public string FriLunchEndTime { get; set; } = string.Empty;
    public string FriExitTime { get; set; } = string.Empty;

    // Saturday
    public string SatEntryTime { get; set; } = string.Empty;
    public int SatToleranceMinutes { get; set; }
    public string SatLunchStartTime { get; set; } = string.Empty;
    public string SatLunchEndTime { get; set; } = string.Empty;
    public string SatExitTime { get; set; } = string.Empty;

    // Sunday
    public string SunEntryTime { get; set; } = string.Empty;
    public int SunToleranceMinutes { get; set; }
    public string SunLunchStartTime { get; set; } = string.Empty;
    public string SunLunchEndTime { get; set; } = string.Empty;
    public string SunExitTime { get; set; } = string.Empty;

    public decimal WeeklyHours { get; set; }
    public bool IsFlexible { get; set; }
    public string Notes { get; set; } = string.Empty;
}
