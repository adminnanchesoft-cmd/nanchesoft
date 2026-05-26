using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ClockImportMapping : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DeviceCode { get; set; } = string.Empty;

    // Column name in source file for employee number
    public string EmployeeNumberColumn { get; set; } = "NoEmpleado";
    // Combined datetime column (or empty if split)
    public string DateTimeColumn { get; set; } = string.Empty;
    // Separate date/time columns
    public string DateColumn { get; set; } = string.Empty;
    public string TimeInColumn { get; set; } = string.Empty;
    public string TimeOutColumn { get; set; } = string.Empty;
    // Optional punch type column
    public string PunchTypeColumn { get; set; } = string.Empty;
    public string DefaultPunchType { get; set; } = "entry";

    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    // CSV delimiter character
    public string Delimiter { get; set; } = ",";

    public bool IsDefault { get; set; }
    public string Notes { get; set; } = string.Empty;
}
