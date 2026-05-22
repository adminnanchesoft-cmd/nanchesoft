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
            var incidentType = await dbContext.NomPayrollIncidentTypes
                .Where(x => x.TenantId == company.TenantId && x.CompanyId == company.Id && x.IsActive && !x.IsDeleted)
                .OrderByDescending(x => x.IncidentCategory == "INFORMATIVA")
                .ThenBy(x => x.SortOrder)
                .FirstOrDefaultAsync();

            foreach (var employee in employees)
            {
                if (incidentType is not null && !await dbContext.EmployeeIncidents.AnyAsync(x => x.EmployeeId == employee.Id && x.PayrollPeriodId == period.Id && x.IncidentType == incidentType.Code))
                {
                    dbContext.EmployeeIncidents.Add(new EmployeeIncident
                    {
                        TenantId = company.TenantId,
                        CompanyId = company.Id,
                        EmployeeId = employee.Id,
                        PayrollPeriodId = period.Id,
                        IncidentDate = now.Date,
                        PayrollIncidentTypeId = incidentType.Id,
                        IncidentType = incidentType.Code,
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
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS formula character varying(2000) NOT NULL DEFAULT '';
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS taxable_formula character varying(2000) NOT NULL DEFAULT '';
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS exempt_formula character varying(2000) NOT NULL DEFAULT '';
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS imss_taxable_formula character varying(2000) NOT NULL DEFAULT '';
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS sat_tipo_percepcion_code character varying(20) NOT NULL DEFAULT '';
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS sat_tipo_deduccion_code character varying(20) NOT NULL DEFAULT '';
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS sat_tipo_otro_pago_code character varying(20) NOT NULL DEFAULT '';
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS sat_agrupador character varying(40) NOT NULL DEFAULT '';
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS automatic_on_global_run boolean NOT NULL DEFAULT false;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS automatic_on_termination boolean NOT NULL DEFAULT false;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS is_in_kind boolean NOT NULL DEFAULT false;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS affects_seventh_day boolean NOT NULL DEFAULT false;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS affects_holiday_pay boolean NOT NULL DEFAULT false;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS affects_imss boolean NOT NULL DEFAULT true;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS affects_isr boolean NOT NULL DEFAULT true;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS affects_accumulators boolean NOT NULL DEFAULT true;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS requires_sat_stamping boolean NOT NULL DEFAULT true;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS min_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE payroll.payroll_concepts ADD COLUMN IF NOT EXISTS max_amount numeric(18,2) NOT NULL DEFAULT 0;
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS hr.hr_attendance_punches (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    branch_id uuid NULL,
    employee_id uuid NOT NULL,
    work_date timestamp with time zone NOT NULL,
    punch_date_time timestamp with time zone NOT NULL,
    punch_type character varying(20) NOT NULL,
    source character varying(30) NOT NULL,
    device_name character varying(120) NOT NULL,
    device_serial character varying(120) NOT NULL,
    external_reference character varying(120) NOT NULL,
    status character varying(30) NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE INDEX IF NOT EXISTS ix_hr_attendance_punches_company_employee_datetime ON hr.hr_attendance_punches (company_id, employee_id, punch_date_time);
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS payroll.payroll_recurring_movements (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    payroll_concept_id uuid NOT NULL,
    movement_code character varying(40) NOT NULL,
    movement_name character varying(160) NOT NULL,
    movement_type character varying(30) NOT NULL,
    calculation_mode character varying(40) NOT NULL,
    quantity numeric(18,4) NOT NULL,
    amount numeric(18,2) NOT NULL,
    percentage numeric(18,4) NOT NULL,
    effective_start_date timestamp with time zone NOT NULL,
    effective_end_date timestamp with time zone NULL,
    apply_every_run boolean NOT NULL,
    day_of_period integer NULL,
    is_prorated boolean NOT NULL,
    status character varying(30) NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_recurring_movements_company_employee_code ON payroll.payroll_recurring_movements (company_id, employee_id, movement_code);
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS payroll.employee_loans (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    payroll_concept_id uuid NOT NULL,
    loan_number character varying(40) NOT NULL,
    loan_date timestamp with time zone NOT NULL,
    start_date timestamp with time zone NOT NULL,
    end_date timestamp with time zone NULL,
    principal_amount numeric(18,2) NOT NULL,
    balance_amount numeric(18,2) NOT NULL,
    installment_amount numeric(18,2) NOT NULL,
    installments integer NOT NULL,
    installments_paid integer NOT NULL,
    status character varying(30) NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_employee_loans_company_number ON payroll.employee_loans (company_id, loan_number);
");

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS payroll.employee_loan_deductions (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    employee_loan_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    payroll_period_id uuid NULL,
    payroll_run_id uuid NULL,
    payroll_run_line_id uuid NULL,
    deduction_date timestamp with time zone NOT NULL,
    installment_number integer NOT NULL,
    amount numeric(18,2) NOT NULL,
    principal_applied numeric(18,2) NOT NULL,
    interest_applied numeric(18,2) NOT NULL,
    remaining_balance numeric(18,2) NOT NULL,
    status character varying(30) NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_employee_loan_deductions_loan_installment ON payroll.employee_loan_deductions (employee_loan_id, installment_number);
");
    }
}
