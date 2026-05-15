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
CREATE TABLE IF NOT EXISTS payroll_attendance_daily_summaries (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollPeriodId" uuid NULL,
    "WorkDate" timestamp with time zone NOT NULL,
    "ScheduledEntryTime" timestamp with time zone NULL,
    "ScheduledExitTime" timestamp with time zone NULL,
    "FirstPunchDateTime" timestamp with time zone NULL,
    "LastPunchDateTime" timestamp with time zone NULL,
    "WorkedHours" numeric(18,4) NOT NULL,
    "DelayMinutes" integer NOT NULL,
    "EarlyLeaveMinutes" integer NOT NULL,
    "OvertimeHours" numeric(18,4) NOT NULL,
    "AbsenceUnits" numeric(18,4) NOT NULL,
    "DayType" character varying(30) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Source" character varying(40) NOT NULL,
    "Notes" character varying(800) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_attendance_daily_summaries_company_employee_workdate ON payroll_attendance_daily_summaries ("CompanyId", "EmployeeId", "WorkDate");
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_prepayroll_adjustments (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "PayrollConceptId" uuid NULL,
    "AdjustmentCode" character varying(40) NOT NULL,
    "AdjustmentName" character varying(160) NOT NULL,
    "AdjustmentType" character varying(30) NOT NULL,
    "CaptureSource" character varying(30) NOT NULL,
    "ReferenceDate" timestamp with time zone NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "TaxableAmount" numeric(18,2) NOT NULL,
    "ExemptAmount" numeric(18,2) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Notes" character varying(800) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_prepayroll_adjustments_company_employee_period_code ON payroll_prepayroll_adjustments ("CompanyId", "EmployeeId", "PayrollPeriodId", "AdjustmentCode");
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_prepayroll_cutoffs (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "CutoffCode" character varying(40) NOT NULL,
    "CutoffName" character varying(160) NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "EmployeesReviewed" integer NOT NULL,
    "IncidentsDetected" integer NOT NULL,
    "WorkedDaysTotal" numeric(18,4) NOT NULL,
    "OvertimeHoursTotal" numeric(18,4) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "IsClosed" boolean NOT NULL,
    "ClosedAt" timestamp with time zone NULL,
    "Notes" character varying(800) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_prepayroll_cutoffs_company_period_code ON payroll_prepayroll_cutoffs ("CompanyId", "PayrollPeriodId", "CutoffCode");
""");
    }
}
