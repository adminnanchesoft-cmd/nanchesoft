using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class AttendancePunch : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTime WorkDate { get; set; }
    public DateTime PunchDateTime { get; set; }
    public string PunchType { get; set; } = "entry";
    public string Source { get; set; } = "manual";
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceSerial { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string Status { get; set; } = "captured";
    public string Notes { get; set; } = string.Empty;
}
