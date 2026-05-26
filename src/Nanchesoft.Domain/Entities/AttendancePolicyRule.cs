using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class AttendancePolicyRule : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid AttendancePolicyId { get; set; }
    public AttendancePolicy? Policy { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // RuleType: "Absence" | "Lateness" | "EarlyLeave" | "Overtime" | "MissingPunch"
    public string RuleType { get; set; } = string.Empty;

    // ConditionType: "GreaterThan" | "GreaterThanOrEqual" | "Equal" | "Always"
    public string ConditionType { get; set; } = "GreaterThan";
    public int? ThresholdMinutes { get; set; }
    public decimal? ThresholdDays { get; set; }

    // ActionType: "Deduct" | "Flag" | "NotifyOnly" | "CreateIncident"
    public string ActionType { get; set; } = "CreateIncident";
    public decimal ActionValue { get; set; }

    // Incident type code to create when triggered (links to NomPayrollIncidentType.Code)
    public string? IncidentTypeCode { get; set; }

    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
