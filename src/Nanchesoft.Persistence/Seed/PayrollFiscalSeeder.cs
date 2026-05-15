using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class PayrollFiscalSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        await EnsureSchemaAsync(dbContext);

        var run = await dbContext.PayrollRuns
            .AsNoTracking()
            .OrderByDescending(x => x.RunDate)
            .FirstOrDefaultAsync();

        if (run is null)
            return;

        var company = await dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.CompanyId);
        if (company is null)
            return;

        var period = await dbContext.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.PayrollPeriodId);
        if (period is null)
            return;

        var runLines = await dbContext.PayrollRunLines
            .AsNoTracking()
            .Where(x => x.PayrollRunId == run.Id)
            .OrderBy(x => x.CreatedAt)
            .Take(5)
            .ToListAsync();

        foreach (var line in runLines)
        {
            var accumulatorCode = $"ACU-{line.EmployeeId.ToString("N")[..8]}";
            var exists = await dbContext.PayrollTaxAccumulators.AnyAsync(x =>
                x.PayrollRunId == run.Id &&
                x.EmployeeId == line.EmployeeId &&
                x.AccumulatorCode == accumulatorCode);

            if (exists)
                continue;

            dbContext.PayrollTaxAccumulators.Add(new PayrollTaxAccumulator
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollRunId = run.Id,
                PayrollRunLineId = line.Id,
                PayrollPeriodId = period.Id,
                EmployeeId = line.EmployeeId,
                AccumulatorCode = accumulatorCode,
                AccumulatorName = "ACUMULADO ISR / CFDI",
                FiscalYear = period.StartDate.Year,
                FiscalMonth = period.StartDate.Month,
                TaxableAmount = Math.Round(line.GrossAmount * 0.82m, 2),
                ExemptAmount = Math.Round(line.GrossAmount * 0.18m, 2),
                WithheldIsr = Math.Round(line.GrossAmount * 0.07m, 2),
                SubsidyApplied = 0m,
                SocialSecurityBase = Math.Round(line.GrossAmount * 0.65m, 2),
                NetAmount = line.NetAmount,
                LastCalculatedAt = DateTime.UtcNow,
                Status = "calculated",
                Notes = "Acumulado fiscal seed enterprise para seguimiento ISR/CFDI de nómina.",
                CreatedBy = "seed"
            });
        }

        var obligations = new[]
        {
            new { Code = "IMSS", Name = "Cuotas IMSS", Type = "social-security", Rate = 0.185m, DueOffset = 17 },
            new { Code = "INFONAVIT", Name = "Aportación INFONAVIT", Type = "housing", Rate = 0.05m, DueOffset = 17 },
            new { Code = "ISN", Name = "Impuesto sobre nómina", Type = "state-tax", Rate = 0.03m, DueOffset = 20 },
        };

        foreach (var obligation in obligations)
        {
            var exists = await dbContext.PayrollEmployerObligations.AnyAsync(x =>
                x.PayrollRunId == run.Id &&
                x.ObligationCode == obligation.Code);

            if (exists)
                continue;

            var amount = Math.Round(run.GrossAmount * obligation.Rate, 2);
            dbContext.PayrollEmployerObligations.Add(new PayrollEmployerObligation
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollRunId = run.Id,
                PayrollPeriodId = period.Id,
                ObligationCode = obligation.Code,
                ObligationName = obligation.Name,
                ObligationType = obligation.Type,
                FiscalYear = period.StartDate.Year,
                FiscalMonth = period.StartDate.Month,
                BaseAmount = run.GrossAmount,
                Amount = amount,
                EmployeesCount = run.EmployeeCount,
                DueDate = CreateUtcDate(period.EndDate.Year, period.EndDate.Month, 1).AddMonths(1).AddDays(obligation.DueOffset - 1),
                Status = "pending",
                ReferenceNumber = string.Empty,
                Notes = "Obligación patronal seed enterprise a partir del cierre de nómina.",
                CreatedBy = "seed"
            });
        }

        var reconcileCode = $"FISC-{run.Folio}";
        var existingReconciliation = await dbContext.PayrollFiscalReconciliations
            .FirstOrDefaultAsync(x => x.PayrollRunId == run.Id && x.ReconciliationCode == reconcileCode);

        if (existingReconciliation is null)
        {
            var dispersionBatchId = await dbContext.PayrollDispersionBatches
                .Where(x => x.PayrollRunId == run.Id)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

            var accountingPostingId = await dbContext.PayrollAccountingPostings
                .Where(x => x.PayrollRunId == run.Id)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

            var receiptsCount = await dbContext.PayrollReceiptControls.CountAsync(x => x.PayrollRunId == run.Id);
            var taxCount = await dbContext.PayrollTaxAccumulators.CountAsync(x => x.PayrollRunId == run.Id);
            var obligationAmount = await dbContext.PayrollEmployerObligations
                .Where(x => x.PayrollRunId == run.Id)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            dbContext.PayrollFiscalReconciliations.Add(new PayrollFiscalReconciliation
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollRunId = run.Id,
                PayrollPeriodId = period.Id,
                PayrollDispersionBatchId = dispersionBatchId,
                PayrollAccountingPostingId = accountingPostingId,
                ReconciliationCode = reconcileCode,
                FiscalYear = period.StartDate.Year,
                FiscalMonth = period.StartDate.Month,
                ReceiptsStampedCount = receiptsCount,
                DispersionValidatedCount = dispersionBatchId.HasValue ? Math.Max(run.EmployeeCount, 0) : 0,
                AccountingPostedCount = accountingPostingId.HasValue ? 1 : 0,
                TaxAccumulatorsCount = taxCount,
                GrossAmount = run.GrossAmount,
                WithheldIsrAmount = Math.Round(run.GrossAmount * 0.07m, 2),
                EmployerTaxesAmount = obligationAmount,
                NetAmount = run.NetAmount,
                DifferenceAmount = 0m,
                Status = "ready",
                ReconciledAt = DateTime.UtcNow,
                ClosedBy = "seed",
                Notes = "Conciliación fiscal seed enterprise entre nómina, dispersión, CFDI y contabilidad.",
                CreatedBy = "seed"
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static DateTime CreateUtcDate(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static async Task EnsureSchemaAsync(NanchesoftDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_tax_accumulators (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "PayrollRunLineId" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "AccumulatorCode" character varying(40) NOT NULL,
    "AccumulatorName" character varying(140) NOT NULL,
    "FiscalYear" integer NOT NULL,
    "FiscalMonth" integer NOT NULL,
    "TaxableAmount" numeric(18,2) NOT NULL,
    "ExemptAmount" numeric(18,2) NOT NULL,
    "WithheldIsr" numeric(18,2) NOT NULL,
    "SubsidyApplied" numeric(18,2) NOT NULL,
    "SocialSecurityBase" numeric(18,2) NOT NULL,
    "NetAmount" numeric(18,2) NOT NULL,
    "LastCalculatedAt" timestamp with time zone NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_tax_accumulators_run_employee_code ON payroll_tax_accumulators ("PayrollRunId", "EmployeeId", "AccumulatorCode");
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_employer_obligations (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "ObligationCode" character varying(40) NOT NULL,
    "ObligationName" character varying(140) NOT NULL,
    "ObligationType" character varying(40) NOT NULL,
    "FiscalYear" integer NOT NULL,
    "FiscalMonth" integer NOT NULL,
    "BaseAmount" numeric(18,2) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "EmployeesCount" integer NOT NULL,
    "DueDate" timestamp with time zone NOT NULL,
    "Status" character varying(30) NOT NULL,
    "PaidAt" timestamp with time zone NULL,
    "ReferenceNumber" character varying(80) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_employer_obligations_run_code ON payroll_employer_obligations ("PayrollRunId", "ObligationCode");
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_fiscal_reconciliations (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "PayrollDispersionBatchId" uuid NULL,
    "PayrollAccountingPostingId" uuid NULL,
    "ReconciliationCode" character varying(40) NOT NULL,
    "FiscalYear" integer NOT NULL,
    "FiscalMonth" integer NOT NULL,
    "ReceiptsStampedCount" integer NOT NULL,
    "DispersionValidatedCount" integer NOT NULL,
    "AccountingPostedCount" integer NOT NULL,
    "TaxAccumulatorsCount" integer NOT NULL,
    "GrossAmount" numeric(18,2) NOT NULL,
    "WithheldIsrAmount" numeric(18,2) NOT NULL,
    "EmployerTaxesAmount" numeric(18,2) NOT NULL,
    "NetAmount" numeric(18,2) NOT NULL,
    "DifferenceAmount" numeric(18,2) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ReconciledAt" timestamp with time zone NULL,
    "ClosedBy" character varying(120) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_fiscal_reconciliations_run_code ON payroll_fiscal_reconciliations ("PayrollRunId", "ReconciliationCode");
""");
    }
}
