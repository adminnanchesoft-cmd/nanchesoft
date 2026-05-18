using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Employee : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }

    public Guid? WorkScheduleId { get; set; }
    public WorkSchedule? WorkSchedule { get; set; }

    // Identificadores
    public string Code { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string? ClockKey { get; set; }            // ClaveReloj
    public string? NoiKey { get; set; }              // ClaveNoi

    // Nombre
    public string FirstName { get; set; } = string.Empty;   // NombreSolo
    public string LastName { get; set; } = string.Empty;    // Paterno
    public string? SecondLastName { get; set; }              // Materno
    public string MiddleName { get; set; } = string.Empty;

    // Contacto
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EmergencyPhone { get; set; }      // TelefonoEmergencia

    // Datos personales
    public string TaxId { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }              // Sexo: M/F
    public string? BloodType { get; set; }           // TipoDeSangre
    public string? MaritalStatus { get; set; }       // EstadoCivil
    public string? PlaceOfBirth { get; set; }        // LugarDeNacimiento
    public string? Nationality { get; set; }         // Nacionalidad
    public string? FatherName { get; set; }          // Padre
    public string? MotherName { get; set; }          // Madre

    // Domicilio
    public string? AddressStreet { get; set; }       // CalleYNumero
    public string? AddressColony { get; set; }       // Colonia
    public string? AddressCity { get; set; }         // Poblacion
    public string? AddressState { get; set; }        // Estado
    public string? AddressZipCode { get; set; }      // CodigoPostal

    // Salario
    public decimal DailySalary { get; set; }
    public decimal IntegratedDailySalary { get; set; }
    public decimal SbcFija { get; set; }
    public string Status { get; set; } = "active";

    // Baja
    public DateTime? TerminationDate { get; set; }   // FechaBaja
    public string? TerminationReason { get; set; }   // MotivoBaja
    public DateTime? ReentryDate { get; set; }       // FechaReingreso

    // Datos IMSS / SAT
    public string Curp { get; set; } = string.Empty;
    public string Nss { get; set; } = string.Empty;
    public string ImssRegId { get; set; } = string.Empty;
    public bool IsImssRegistered { get; set; }
    public DateTime? ImssRegistrationDate { get; set; }  // FechaIMSS
    public DateTime? ImssTerminationDate { get; set; }   // FechaIMSS_Baja
    public string? Umf { get; set; }                 // UMF
    public string ContractType { get; set; } = "indefinite";
    public string CotizationBase { get; set; } = "fixed";
    public string TaxRegime { get; set; } = "sueldos_salarios";
    public string EmployeeType { get; set; } = "base";
    public string SalaryZone { get; set; } = "A";
    public string PayrollPeriodType { get; set; } = "semanal";

    // Fondos / créditos
    public string? Afore { get; set; }
    public string? Fonacot { get; set; }             // NoFonacot
    public string? Infonavit { get; set; }           // NumeroCreditoInfonavit

    // Datos bancarios
    public string PaymentForm { get; set; } = "tarjeta";
    public string BankCode { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public string? BankBranch { get; set; }          // SucursalPago

    // Otros
    public string? ImmediateSupervisor { get; set; } // JefeDirecto
    public string? Category { get; set; }            // Categoria
    public string? Notes { get; set; }               // Expediente
    public bool PrintReceipt { get; set; } = true;  // ImprimeRecibo

    public string GetFullName()
    {
        return string.Join(" ", new[] { FirstName, LastName, SecondLastName }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()));
    }

    public string GetFullNameInverted()
    {
        var apellidos = string.Join(" ", new[] { LastName, SecondLastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(apellidos) ? FirstName : $"{apellidos}, {FirstName}";
    }
}
