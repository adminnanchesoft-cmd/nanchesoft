using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class PayrollPrePayrollSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        await EnsureSchemaAsync(dbContext);

        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
            return;

        var branch = await dbContext.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var employees = await dbContext.Employees.Where(x => x.CompanyId == company.Id && x.IsActive).OrderBy(x => x.CreatedAt).Take(5).ToListAsync();
        if (employees.Count == 0)
            return;

        var period = await dbContext.PayrollPeriods.Where(x => x.CompanyId == company.Id).OrderByDescending(x => x.StartDate).FirstOrDefaultAsync();
        var concept = await dbContext.PayrollConcepts.Where(x => x.CompanyId == company.Id && x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync();

        var groupedPunches = await dbContext.AttendancePunches
            .Where(x => x.CompanyId == company.Id)
            .OrderByDescending(x => x.PunchDateTime)
            .Take(200)
            .AsNoTracking()
            .ToListAsync();

        var scheduleEntryHour = 8;
        var scheduleExitHour = 17;

        foreach (var group in groupedPunches.GroupBy(x => new { x.EmployeeId, WorkDate = x.WorkDate.Date }).Take(40))
        {
            var employee = employees.FirstOrDefault(x => x.Id == group.Key.EmployeeId);
            if (employee is null)
                continue;

            var summaryExists = await dbContext.AttendanceDailySummaries.AnyAsync(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.WorkDate == group.Key.WorkDate);
            if (summaryExists)
                continue;

            var firstPunch = group.OrderBy(x => x.PunchDateTime).First().PunchDateTime;
            var lastPunch = group.OrderByDescending(x => x.PunchDateTime).First().PunchDateTime;
            var workedHours = Math.Max(0m, (decimal)(lastPunch - firstPunch).TotalHours);
            var scheduledEntry = group.Key.WorkDate.AddHours(scheduleEntryHour);
            var scheduledExit = group.Key.WorkDate.AddHours(scheduleExitHour);
            var delayMinutes = firstPunch > scheduledEntry ? (int)Math.Round((firstPunch - scheduledEntry).TotalMinutes) : 0;
            var earlyLeaveMinutes = lastPunch < scheduledExit ? (int)Math.Round((scheduledExit - lastPunch).TotalMinutes) : 0;
            var overtimeHours = workedHours > 8m ? Math.Round(workedHours - 8m, 2) : 0m;

            dbContext.AttendanceDailySummaries.Add(new AttendanceDailySummary
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = employee.BranchId ?? branch?.Id,
                EmployeeId = employee.Id,
                PayrollPeriodId = period?.Id,
                WorkDate = group.Key.WorkDate,
                ScheduledEntryTime = scheduledEntry,
                ScheduledExitTime = scheduledExit,
                FirstPunchDateTime = firstPunch,
                LastPunchDateTime = lastPunch,
                WorkedHours = Math.Round(workedHours, 2),
                DelayMinutes = Math.Max(delayMinutes, 0),
                EarlyLeaveMinutes = Math.Max(earlyLeaveMinutes, 0),
                OvertimeHours = overtimeHours,
                AbsenceUnits = 0m,
                DayType = "workday",
                Status = "calculated",
                Source = "time-clock",
                Notes = "Resumen demo generado desde el reloj checador.",
                CreatedBy = "seed"
            });
        }

        await dbContext.SaveChangesAsync();

        if (period is not null)
        {
            var summaries = await dbContext.AttendanceDailySummaries
                .Where(x => x.CompanyId == company.Id && x.WorkDate >= period.StartDate.Date && x.WorkDate <= period.EndDate.Date)
                .ToListAsync();
            var incidents = await dbContext.EmployeeIncidents
                .Where(x => x.CompanyId == company.Id && x.PayrollPeriodId == period.Id)
                .CountAsync();

            if (!await dbContext.PrePayrollCutoffs.AnyAsync(x => x.CompanyId == company.Id && x.PayrollPeriodId == period.Id && x.CutoffCode == "CORTE-GENERAL"))
            {
                dbContext.PrePayrollCutoffs.Add(new PrePayrollCutoff
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    BranchId = branch?.Id,
                    PayrollPeriodId = period.Id,
                    CutoffCode = "CORTE-GENERAL",
                    CutoffName = $"Corte prenómina {period.Code}",
                    StartDate = period.StartDate,
                    EndDate = period.EndDate,
                    EmployeesReviewed = summaries.Select(x => x.EmployeeId).Distinct().Count(),
                    IncidentsDetected = incidents,
                    WorkedDaysTotal = summaries.Sum(x => Math.Max(0m, 1m - x.AbsenceUnits)),
                    OvertimeHoursTotal = summaries.Sum(x => x.OvertimeHours),
                    Status = "in_review",
                    IsClosed = false,
                    Notes = "Corte demo listo para revisión de prenómina.",
                    CreatedBy = "seed"
                });
            }

            var firstEmployee = employees.First();
            if (concept is not null && !await dbContext.PrePayrollAdjustments.AnyAsync(x => x.CompanyId == company.Id && x.EmployeeId == firstEmployee.Id && x.PayrollPeriodId == period.Id && x.AdjustmentCode == "AJUSTE-DEMO"))
            {
                dbContext.PrePayrollAdjustments.Add(new PrePayrollAdjustment
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    EmployeeId = firstEmployee.Id,
                    PayrollPeriodId = period.Id,
                    PayrollConceptId = concept.Id,
                    AdjustmentCode = "AJUSTE-DEMO",
                    AdjustmentName = "Ajuste manual demo prenómina",
                    AdjustmentType = concept.ConceptType,
                    CaptureSource = "manual",
                    ReferenceDate = period.EndDate,
                    Quantity = 1m,
                    Amount = 125m,
                    TaxableAmount = concept.ConceptType == "deduction" ? 125m : 87.5m,
                    ExemptAmount = concept.ConceptType == "deduction" ? 0m : 37.5m,
                    Status = "captured",
                    Notes = "Movimiento demo para validar captura de prenómina.",
                    CreatedBy = "seed"
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureSchemaAsync(NanchesoftDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_attendance_daily_summaries (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    branch_id uuid NULL,
    employee_id uuid NOT NULL,
    payroll_period_id uuid NULL,
    work_date timestamp with time zone NOT NULL,
    scheduled_entry_time timestamp with time zone NULL,
    scheduled_exit_time timestamp with time zone NULL,
    first_punch_date_time timestamp with time zone NULL,
    last_punch_date_time timestamp with time zone NULL,
    worked_hours numeric(18,4) NOT NULL,
    delay_minutes integer NOT NULL,
    early_leave_minutes integer NOT NULL,
    overtime_hours numeric(18,4) NOT NULL,
    absence_units numeric(18,4) NOT NULL,
    day_type character varying(30) NOT NULL,
    status character varying(30) NOT NULL,
    source character varying(40) NOT NULL,
    notes character varying(800) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_attendance_daily_summaries_company_employee_workdate ON payroll.payroll_attendance_daily_summaries (company_id, employee_id, work_date);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_prepayroll_adjustments (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    payroll_period_id uuid NOT NULL,
    payroll_concept_id uuid NULL,
    adjustment_code character varying(40) NOT NULL,
    adjustment_name character varying(160) NOT NULL,
    adjustment_type character varying(30) NOT NULL,
    capture_source character varying(30) NOT NULL,
    reference_date timestamp with time zone NOT NULL,
    quantity numeric(18,4) NOT NULL,
    amount numeric(18,2) NOT NULL,
    taxable_amount numeric(18,2) NOT NULL,
    exempt_amount numeric(18,2) NOT NULL,
    status character varying(30) NOT NULL,
    notes character varying(800) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_prepayroll_adjustments_company_employee_period_code ON payroll.payroll_prepayroll_adjustments (company_id, employee_id, payroll_period_id, adjustment_code);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_prepayroll_cutoffs (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    branch_id uuid NULL,
    payroll_period_id uuid NOT NULL,
    cutoff_code character varying(40) NOT NULL,
    cutoff_name character varying(160) NOT NULL,
    start_date timestamp with time zone NOT NULL,
    end_date timestamp with time zone NOT NULL,
    employees_reviewed integer NOT NULL,
    incidents_detected integer NOT NULL,
    worked_days_total numeric(18,4) NOT NULL,
    overtime_hours_total numeric(18,4) NOT NULL,
    status character varying(30) NOT NULL,
    is_closed boolean NOT NULL,
    closed_at timestamp with time zone NULL,
    notes character varying(800) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_prepayroll_cutoffs_company_period_code ON payroll.payroll_prepayroll_cutoffs (company_id, payroll_period_id, cutoff_code);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_prepayroll_column_preferences (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_period_id uuid NULL,
    user_key character varying(120) NOT NULL,
    concept_ids character varying(4000) NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_prepayroll_column_preferences_company_user_period ON payroll.payroll_prepayroll_column_preferences (company_id, user_key, payroll_period_id);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_global_movements (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_concept_id uuid NOT NULL,
    batch_code character varying(40) NOT NULL,
    batch_name character varying(160) NOT NULL,
    movement_type character varying(30) NOT NULL,
    calculation_mode character varying(40) NOT NULL,
    quantity numeric(18,4) NOT NULL,
    amount numeric(18,2) NOT NULL,
    percentage numeric(18,4) NOT NULL,
    start_date timestamp with time zone NOT NULL,
    end_date timestamp with time zone NULL,
    times_to_apply integer NOT NULL,
    times_applied integer NOT NULL,
    max_amount numeric(18,2) NOT NULL,
    accumulated_amount numeric(18,2) NOT NULL,
    control_number character varying(60) NOT NULL,
    filter_department_ids character varying(4000) NOT NULL,
    filter_position_ids character varying(4000) NOT NULL,
    filter_branch_ids character varying(4000) NOT NULL,
    filter_employer_registration_ids character varying(4000) NOT NULL,
    filter_work_shift_ids character varying(4000) NOT NULL,
    filter_employee_ids character varying(4000) NOT NULL,
    exclude_employee_ids character varying(4000) NOT NULL,
    min_salary numeric(18,2) NOT NULL,
    max_salary numeric(18,2) NOT NULL,
    make_recurring boolean NOT NULL,
    status character varying(30) NOT NULL,
    applied_at timestamp with time zone NULL,
    applied_by character varying(160) NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_global_movements_company_batch_code ON payroll.payroll_global_movements (company_id, batch_code);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_global_movement_lines (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_global_movement_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    payroll_period_id uuid NULL,
    quantity numeric(18,4) NOT NULL,
    amount numeric(18,2) NOT NULL,
    applied_at timestamp with time zone NOT NULL,
    applied_by character varying(160) NOT NULL,
    resulting_adjustment_id uuid NULL,
    resulting_recurring_movement_id uuid NULL,
    status character varying(30) NOT NULL,
    notes character varying(600) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_global_movement_lines_batch_employee_period ON payroll.payroll_global_movement_lines (payroll_global_movement_id, employee_id, payroll_period_id);
CREATE INDEX IF NOT EXISTS ix_payroll_global_movement_lines_company_employee ON payroll.payroll_global_movement_lines (company_id, employee_id);
""");
    }
}
