using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class PayrollAdvancedSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        await EnsureSchemaAsync(dbContext);

        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return;

        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var employees = await dbContext.Employees.Where(x => x.CompanyId == company.Id && x.IsActive).OrderBy(x => x.CreatedAt).Take(2).ToListAsync();
        if (employees.Count == 0)
            return;

        var run = await dbContext.PayrollRuns.Where(x => x.CompanyId == company.Id).OrderByDescending(x => x.RunDate).FirstOrDefaultAsync();
        var period = run is not null
            ? await dbContext.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == run.PayrollPeriodId)
            : await dbContext.PayrollPeriods.Where(x => x.CompanyId == company.Id).OrderByDescending(x => x.StartDate).FirstOrDefaultAsync();

        var bonusConcept = await EnsureConceptAsync(dbContext, company.Id, company.TenantId, "BON", "Bono recurrente", "perception", "fixed", "019", "mixed");
        var loanConcept = await EnsureConceptAsync(dbContext, company.Id, company.TenantId, "PRES", "Descuento préstamo", "deduction", "fixed", "004", "taxable");

        var now = DateTime.UtcNow;
        var workDate = now.Date;

        foreach (var employee in employees)
        {
            if (!await dbContext.AttendancePunches.AnyAsync(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.WorkDate == workDate))
            {
                dbContext.AttendancePunches.Add(new AttendancePunch
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = employee.BranchId ?? branch?.Id,
                    EmployeeId = employee.Id,
                    WorkDate = workDate,
                    PunchDateTime = workDate.AddHours(8).AddMinutes(5),
                    PunchType = "entry",
                    Source = "seed-clock",
                    DeviceName = "Reloj demo",
                    DeviceSerial = "CLOCK-001",
                    ExternalReference = $"{employee.EmployeeNumber}-{workDate:yyyyMMdd}-01",
                    Status = "captured",
                    Notes = "Registro demo de entrada.",
                    CreatedBy = "seed"
                });

                dbContext.AttendancePunches.Add(new AttendancePunch
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = employee.BranchId ?? branch?.Id,
                    EmployeeId = employee.Id,
                    WorkDate = workDate,
                    PunchDateTime = workDate.AddHours(17).AddMinutes(10),
                    PunchType = "exit",
                    Source = "seed-clock",
                    DeviceName = "Reloj demo",
                    DeviceSerial = "CLOCK-001",
                    ExternalReference = $"{employee.EmployeeNumber}-{workDate:yyyyMMdd}-02",
                    Status = "captured",
                    Notes = "Registro demo de salida.",
                    CreatedBy = "seed"
                });
            }
        }

        var primaryEmployee = employees[0];
        if (!await dbContext.PayrollRecurringMovements.AnyAsync(x => x.CompanyId == company.Id && x.EmployeeId == primaryEmployee.Id && x.MovementCode == "BONO-MENSUAL"))
        {
            dbContext.PayrollRecurringMovements.Add(new PayrollRecurringMovement
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                EmployeeId = primaryEmployee.Id,
                PayrollConceptId = bonusConcept.Id,
                MovementCode = "BONO-MENSUAL",
                MovementName = "Bono mensual programado",
                MovementType = "perception",
                CalculationMode = "fixed",
                Quantity = 1m,
                Amount = 350m,
                Percentage = 0m,
                EffectiveStartDate = now.Date.AddMonths(-1),
                EffectiveEndDate = null,
                ApplyEveryRun = true,
                DayOfPeriod = null,
                IsProrated = false,
                Status = "active",
                Notes = "Bono automático por productividad.",
                CreatedBy = "seed"
            });
        }

        var employeeWithLoan = employees.Last();
        var loan = await dbContext.EmployeeLoans.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.EmployeeId == employeeWithLoan.Id && x.LoanNumber == "PREST-0001");
        if (loan is null)
        {
            loan = new EmployeeLoan
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                EmployeeId = employeeWithLoan.Id,
                PayrollConceptId = loanConcept.Id,
                LoanNumber = "PREST-0001",
                LoanDate = now.Date.AddDays(-20),
                StartDate = now.Date.AddDays(-15),
                EndDate = null,
                PrincipalAmount = 2500m,
                BalanceAmount = 2500m,
                InstallmentAmount = 250m,
                Installments = 10,
                InstallmentsPaid = 0,
                Status = "active",
                Notes = "Préstamo demo a colaborador.",
                CreatedBy = "seed"
            };
            dbContext.EmployeeLoans.Add(loan);
        }

        await dbContext.SaveChangesAsync();

        if (run is not null && period is not null)
        {
            foreach (var employee in employees)
            {
                if (!await dbContext.EmployeeIncidents.AnyAsync(x => x.EmployeeId == employee.Id && x.PayrollPeriodId == period.Id && x.IncidentType == "attendance_review"))
                {
                    dbContext.EmployeeIncidents.Add(new EmployeeIncident
                    {
                        TenantId = company.TenantId,
                        CompanyId = company.Id,
                        EmployeeId = employee.Id,
                        PayrollPeriodId = period.Id,
                        IncidentDate = now.Date,
                        IncidentType = "attendance_review",
                        Quantity = 1m,
                        Amount = 0m,
                        Notes = "Incidencia demo generada para revisar el reloj checador.",
                        Status = "captured",
                        CreatedBy = "seed"
                    });
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task<PayrollConcept> EnsureConceptAsync(
        NanchesoftDbContext dbContext,
        Guid companyId,
        Guid tenantId,
        string code,
        string name,
        string conceptType,
        string calculationType,
        string satCode,
        string taxableType)
    {
        var existing = await dbContext.PayrollConcepts.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code);
        if (existing is not null)
            return existing;

        var concept = new PayrollConcept
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            Name = name,
            ConceptType = conceptType,
            CalculationType = calculationType,
            SatCode = satCode,
            TaxableType = taxableType,
            IsRecurring = code == "BON",
            IsActive = true,
            CreatedBy = "seed"
        };

        dbContext.PayrollConcepts.Add(concept);
        await dbContext.SaveChangesAsync();
        return concept;
    }

    private static async Task EnsureSchemaAsync(NanchesoftDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr_attendance_punches (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""BranchId"" uuid NULL,
    ""EmployeeId"" uuid NOT NULL,
    ""WorkDate"" timestamp with time zone NOT NULL,
    ""PunchDateTime"" timestamp with time zone NOT NULL,
    ""PunchType"" character varying(20) NOT NULL,
    ""Source"" character varying(30) NOT NULL,
    ""DeviceName"" character varying(120) NOT NULL,
    ""DeviceSerial"" character varying(120) NOT NULL,
    ""ExternalReference"" character varying(120) NOT NULL,
    ""Status"" character varying(30) NOT NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE INDEX IF NOT EXISTS ix_hr_attendance_punches_company_employee_datetime ON hr_attendance_punches (""CompanyId"", ""EmployeeId"", ""PunchDateTime"");
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS payroll_recurring_movements (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""EmployeeId"" uuid NOT NULL,
    ""PayrollConceptId"" uuid NOT NULL,
    ""MovementCode"" character varying(40) NOT NULL,
    ""MovementName"" character varying(160) NOT NULL,
    ""MovementType"" character varying(30) NOT NULL,
    ""CalculationMode"" character varying(40) NOT NULL,
    ""Quantity"" numeric(18,4) NOT NULL,
    ""Amount"" numeric(18,2) NOT NULL,
    ""Percentage"" numeric(18,4) NOT NULL,
    ""EffectiveStartDate"" timestamp with time zone NOT NULL,
    ""EffectiveEndDate"" timestamp with time zone NULL,
    ""ApplyEveryRun"" boolean NOT NULL,
    ""DayOfPeriod"" integer NULL,
    ""IsProrated"" boolean NOT NULL,
    ""Status"" character varying(30) NOT NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_recurring_movements_company_employee_code ON payroll_recurring_movements (""CompanyId"", ""EmployeeId"", ""MovementCode"");
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS employee_loans (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""EmployeeId"" uuid NOT NULL,
    ""PayrollConceptId"" uuid NOT NULL,
    ""LoanNumber"" character varying(40) NOT NULL,
    ""LoanDate"" timestamp with time zone NOT NULL,
    ""StartDate"" timestamp with time zone NOT NULL,
    ""EndDate"" timestamp with time zone NULL,
    ""PrincipalAmount"" numeric(18,2) NOT NULL,
    ""BalanceAmount"" numeric(18,2) NOT NULL,
    ""InstallmentAmount"" numeric(18,2) NOT NULL,
    ""Installments"" integer NOT NULL,
    ""InstallmentsPaid"" integer NOT NULL,
    ""Status"" character varying(30) NOT NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_employee_loans_company_number ON employee_loans (""CompanyId"", ""LoanNumber"");
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS employee_loan_deductions (
    ""Id"" uuid PRIMARY KEY,
    ""TenantId"" uuid NOT NULL,
    ""CompanyId"" uuid NOT NULL,
    ""EmployeeLoanId"" uuid NOT NULL,
    ""EmployeeId"" uuid NOT NULL,
    ""PayrollPeriodId"" uuid NULL,
    ""PayrollRunId"" uuid NULL,
    ""PayrollRunLineId"" uuid NULL,
    ""DeductionDate"" timestamp with time zone NOT NULL,
    ""InstallmentNumber"" integer NOT NULL,
    ""Amount"" numeric(18,2) NOT NULL,
    ""PrincipalApplied"" numeric(18,2) NOT NULL,
    ""InterestApplied"" numeric(18,2) NOT NULL,
    ""RemainingBalance"" numeric(18,2) NOT NULL,
    ""Status"" character varying(30) NOT NULL,
    ""Notes"" character varying(600) NOT NULL,
    ""IsActive"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""CreatedBy"" text NULL,
    ""UpdatedAt"" timestamp with time zone NULL,
    ""UpdatedBy"" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_employee_loan_deductions_loan_installment ON employee_loan_deductions (""EmployeeLoanId"", ""InstallmentNumber"");
");
    }
}
