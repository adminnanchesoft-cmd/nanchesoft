namespace Nanchesoft.Web.Services.HumanResources;

public sealed class AttendancePunchRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime? WorkDate { get; set; }
    public DateTime? PunchDateTime { get; set; }
    public string PunchType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceSerial { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRecurringMovementRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string MovementCode { get; set; } = string.Empty;
    public string MovementName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public string CalculationMode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTime? EffectiveStartDate { get; set; }
    public DateTime? EffectiveEndDate { get; set; }
    public bool ApplyEveryRun { get; set; } = true;
    public int? DayOfPeriod { get; set; }
    public bool IsProrated { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeLoanRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public DateTime? LoanDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int Installments { get; set; }
    public int InstallmentsPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeLoanDeductionRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeLoanId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public DateTime? DeductionDate { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalApplied { get; set; }
    public decimal InterestApplied { get; set; }
    public decimal RemainingBalance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollReceiptLineRow
{
    public Guid PayrollRunLineId { get; set; }
    public Guid PayrollRunId { get; set; }
    public string PayrollRunFolio { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public decimal DaysPaid { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal IncidentsAmount { get; set; }
}


public sealed class AttendanceDailySummaryRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public DateTime? WorkDate { get; set; }
    public DateTime? ScheduledEntryTime { get; set; }
    public DateTime? ScheduledExitTime { get; set; }
    public DateTime? FirstPunchDateTime { get; set; }
    public DateTime? LastPunchDateTime { get; set; }
    public decimal WorkedHours { get; set; }
    public int DelayMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal AbsenceUnits { get; set; }
    public string DayType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PrePayrollAdjustmentRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string AdjustmentCode { get; set; } = string.Empty;
    public string AdjustmentName { get; set; } = string.Empty;
    public string AdjustmentType { get; set; } = string.Empty;
    public string CaptureSource { get; set; } = string.Empty;
    public DateTime? ReferenceDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PrePayrollCutoffRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public string CutoffCode { get; set; } = string.Empty;
    public string CutoffName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int EmployeesReviewed { get; set; }
    public int IncidentsDetected { get; set; }
    public decimal WorkedDaysTotal { get; set; }
    public decimal OvertimeHoursTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}


public class PayrollSourceApplicationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public Guid? SourceId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string ApplicationCode { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollSourceApplicationDto : PayrollSourceApplicationRequest
{
    public Guid PayrollSourceApplicationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string PayrollConceptName { get; set; } = string.Empty;
}

public class PayrollReceiptControlRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ReceiptStatus { get; set; } = string.Empty;
    public DateTime? GeneratedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? StampedAt { get; set; }
    public string DeliveryChannel { get; set; } = string.Empty;
    public string DeliveryReference { get; set; } = string.Empty;
    public string AckBy { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollReceiptControlDto : PayrollReceiptControlRequest
{
    public Guid PayrollReceiptControlId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class PayrollRunClosingRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public string ClosingCode { get; set; } = string.Empty;
    public DateTime? ClosingDate { get; set; }
    public int EmployeesIncluded { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int SourceApplicationsCount { get; set; }
    public int ReceiptsGeneratedCount { get; set; }
    public int IssuesDetected { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRunClosingDto : PayrollRunClosingRequest
{
    public Guid PayrollRunClosingId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
}


public class PayrollDispersionBatchRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateTime? DispersionDate { get; set; }
    public string LayoutFormat { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string FundingAccount { get; set; } = string.Empty;
    public int BeneficiariesCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExportedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string FileReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollDispersionBatchDto : PayrollDispersionBatchRequest
{
    public Guid PayrollDispersionBatchId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
}

public class PayrollDispersionLineRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollDispersionBatchId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public int Sequence { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public bool IsRejected { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollDispersionLineDto : PayrollDispersionLineRequest
{
    public Guid PayrollDispersionLineId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class PayrollAccountingPostingRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public string PostingCode { get; set; } = string.Empty;
    public DateTime? PostingDate { get; set; }
    public string LedgerBook { get; set; } = string.Empty;
    public string JournalNumber { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public int LinesCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ExportedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public string ExportReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollAccountingPostingDto : PayrollAccountingPostingRequest
{
    public Guid PayrollAccountingPostingId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
}


public class PayrollTaxAccumulatorRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string AccumulatorCode { get; set; } = string.Empty;
    public string AccumulatorName { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public decimal WithheldIsr { get; set; }
    public decimal SubsidyApplied { get; set; }
    public decimal SocialSecurityBase { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime? LastCalculatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollTaxAccumulatorDto : PayrollTaxAccumulatorRequest
{
    public Guid PayrollTaxAccumulatorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class PayrollEmployerObligationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public string ObligationCode { get; set; } = string.Empty;
    public string ObligationName { get; set; } = string.Empty;
    public string ObligationType { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal Amount { get; set; }
    public int EmployeesCount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollEmployerObligationDto : PayrollEmployerObligationRequest
{
    public Guid PayrollEmployerObligationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
}

public class PayrollFiscalReconciliationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollDispersionBatchId { get; set; }
    public Guid? PayrollAccountingPostingId { get; set; }
    public string ReconciliationCode { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int FiscalMonth { get; set; }
    public int ReceiptsStampedCount { get; set; }
    public int DispersionValidatedCount { get; set; }
    public int AccountingPostedCount { get; set; }
    public int TaxAccumulatorsCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal WithheldIsrAmount { get; set; }
    public decimal EmployerTaxesAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ReconciledAt { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollFiscalReconciliationDto : PayrollFiscalReconciliationRequest
{
    public Guid PayrollFiscalReconciliationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
}
