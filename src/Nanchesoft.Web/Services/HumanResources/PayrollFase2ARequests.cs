namespace Nanchesoft.Web.Services.HumanResources;

public sealed class PrePayrollColumnPreferenceResult
{
    public string Source { get; set; } = "default";
    public List<Guid> ConceptIds { get; set; } = [];
    public bool HasBase { get; set; }
    public bool HasPeriodOverride { get; set; }
}

public sealed class PayrollGlobalMovementListItem
{
    public Guid PayrollGlobalMovementId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PayrollConceptId { get; set; }
    public string PayrollConceptCode { get; set; } = string.Empty;
    public string PayrollConceptName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public string CalculationMode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TimesToApply { get; set; }
    public int TimesApplied { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal AccumulatedAmount { get; set; }
    public string ControlNumber { get; set; } = string.Empty;
    public string FilterDepartmentIds { get; set; } = string.Empty;
    public string FilterPositionIds { get; set; } = string.Empty;
    public string FilterBranchIds { get; set; } = string.Empty;
    public string FilterEmployerRegistrationIds { get; set; } = string.Empty;
    public string FilterWorkShiftIds { get; set; } = string.Empty;
    public string FilterEmployeeIds { get; set; } = string.Empty;
    public string ExcludeEmployeeIds { get; set; } = string.Empty;
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public bool MakeRecurring { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PayrollGlobalMovementSubmit
{
    public Guid? PayrollConceptId { get; set; }
    public string? BatchCode { get; set; }
    public string? BatchName { get; set; }
    public string? MovementType { get; set; }
    public string? CalculationMode { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TimesToApply { get; set; }
    public decimal MaxAmount { get; set; }
    public string? ControlNumber { get; set; }
    public List<Guid> FilterDepartmentIds { get; set; } = [];
    public List<Guid> FilterPositionIds { get; set; } = [];
    public List<Guid> FilterBranchIds { get; set; } = [];
    public List<Guid> FilterEmployerRegistrationIds { get; set; } = [];
    public List<Guid> FilterWorkShiftIds { get; set; } = [];
    public List<Guid> FilterEmployeeIds { get; set; } = [];
    public List<Guid> ExcludeEmployeeIds { get; set; } = [];
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public bool MakeRecurring { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollGlobalMovementCreatedResponse
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
}

public sealed class PayrollGlobalMovementPreview
{
    public Guid PayrollGlobalMovementId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PayrollGlobalMovementPreviewRow> Employees { get; set; } = [];
}

public sealed class PayrollGlobalMovementPreviewRow
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class PayrollGlobalMovementApplyResponse
{
    public bool Success { get; set; }
    public int Applied { get; set; }
    public int Skipped { get; set; }
    public decimal TotalAmount { get; set; }
    public int TimesApplied { get; set; }
    public decimal AccumulatedAmount { get; set; }
}

public sealed class PayrollGlobalMovementLineItem
{
    public Guid PayrollGlobalMovementLineId { get; set; }
    public Guid PayrollGlobalMovementId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public Guid? PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public DateTime AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
    public Guid? ResultingAdjustmentId { get; set; }
    public Guid? ResultingRecurringMovementId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
