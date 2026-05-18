namespace Nanchesoft.Web.Services.HumanResources;

public sealed class DepartmentRequest
{
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PositionRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PayrollGroup { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? WorkScheduleId { get; set; }
    // Identificadores
    public string Code { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string? ClockKey { get; set; }
    public string? NoiKey { get; set; }
    // Nombre
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? SecondLastName { get; set; }
    public string MiddleName { get; set; } = string.Empty;
    // Contacto
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EmergencyPhone { get; set; }
    // Datos personales
    public string TaxId { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? BloodType { get; set; }
    public string? MaritalStatus { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    // Domicilio
    public string? AddressStreet { get; set; }
    public string? AddressColony { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressZipCode { get; set; }
    // Salario
    public decimal DailySalary { get; set; }
    public decimal IntegratedDailySalary { get; set; }
    public decimal SbcFija { get; set; }
    public string Status { get; set; } = string.Empty;
    // Baja
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public DateTime? ReentryDate { get; set; }
    // IMSS / SAT
    public string Curp { get; set; } = string.Empty;
    public string Nss { get; set; } = string.Empty;
    public string ImssRegId { get; set; } = string.Empty;
    public bool IsImssRegistered { get; set; }
    public DateTime? ImssRegistrationDate { get; set; }
    public DateTime? ImssTerminationDate { get; set; }
    public string? Umf { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string CotizationBase { get; set; } = string.Empty;
    public string TaxRegime { get; set; } = string.Empty;
    public string EmployeeType { get; set; } = string.Empty;
    public string SalaryZone { get; set; } = string.Empty;
    public string PayrollPeriodType { get; set; } = string.Empty;
    // Fondos
    public string? Afore { get; set; }
    public string? Fonacot { get; set; }
    public string? Infonavit { get; set; }
    // Banco
    public string PaymentForm { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public string? BankBranch { get; set; }
    // Otros
    public string? ImmediateSupervisor { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public bool PrintReceipt { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeIncidentRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public DateTime? IncidentDate { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeContractRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string PaymentFrequency { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal IntegratedSalary { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollPeriodRequest
{
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PeriodType { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollConceptRequest
{
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public string SatCode { get; set; } = string.Empty;
    public string SatAgrupador { get; set; } = string.Empty;
    public string TaxableType { get; set; } = string.Empty;
    public decimal TaxablePercent { get; set; } = 100m;
    public decimal ExemptPercent { get; set; } = 0m;
    public bool IsRecurring { get; set; }
    public bool IsAutomatic { get; set; } = true;
    public bool PrintOnReceipt { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRunRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime? RunDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRunLineRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public decimal DaysPaid { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal IncidentsAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRunLineDetailRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string ConceptCode { get; set; } = string.Empty;
    public string ConceptName { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty;
    public string SatCode { get; set; } = string.Empty;
    public string TaxableType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public int SortOrder { get; set; }
    public bool IsGenerated { get; set; } = true;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
