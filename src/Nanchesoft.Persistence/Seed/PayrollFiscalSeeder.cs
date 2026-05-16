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
CREATE TABLE IF NOT EXISTS payroll.payroll_tax_accumulators (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    payroll_run_line_id uuid NOT NULL,
    payroll_period_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    accumulator_code character varying(40) NOT NULL,
    accumulator_name character varying(140) NOT NULL,
    fiscal_year integer NOT NULL,
    fiscal_month integer NOT NULL,
    taxable_amount numeric(18,2) NOT NULL,
    exempt_amount numeric(18,2) NOT NULL,
    withheld_isr numeric(18,2) NOT NULL,
    subsidy_applied numeric(18,2) NOT NULL,
    social_security_base numeric(18,2) NOT NULL,
    net_amount numeric(18,2) NOT NULL,
    last_calculated_at timestamp with time zone NOT NULL,
    status character varying(30) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_tax_accumulators_run_employee_code ON payroll.payroll_tax_accumulators (payroll_run_id, employee_id, accumulator_code);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_employer_obligations (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    payroll_period_id uuid NOT NULL,
    obligation_code character varying(40) NOT NULL,
    obligation_name character varying(140) NOT NULL,
    obligation_type character varying(40) NOT NULL,
    fiscal_year integer NOT NULL,
    fiscal_month integer NOT NULL,
    base_amount numeric(18,2) NOT NULL,
    amount numeric(18,2) NOT NULL,
    employees_count integer NOT NULL,
    due_date timestamp with time zone NOT NULL,
    status character varying(30) NOT NULL,
    paid_at timestamp with time zone NULL,
    reference_number character varying(80) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_employer_obligations_run_code ON payroll.payroll_employer_obligations (payroll_run_id, obligation_code);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_fiscal_reconciliations (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    payroll_period_id uuid NOT NULL,
    payroll_dispersion_batch_id uuid NULL,
    payroll_accounting_posting_id uuid NULL,
    reconciliation_code character varying(40) NOT NULL,
    fiscal_year integer NOT NULL,
    fiscal_month integer NOT NULL,
    receipts_stamped_count integer NOT NULL,
    dispersion_validated_count integer NOT NULL,
    accounting_posted_count integer NOT NULL,
    tax_accumulators_count integer NOT NULL,
    gross_amount numeric(18,2) NOT NULL,
    withheld_isr_amount numeric(18,2) NOT NULL,
    employer_taxes_amount numeric(18,2) NOT NULL,
    net_amount numeric(18,2) NOT NULL,
    difference_amount numeric(18,2) NOT NULL,
    status character varying(30) NOT NULL,
    reconciled_at timestamp with time zone NULL,
    closed_by character varying(120) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_fiscal_reconciliations_run_code ON payroll.payroll_fiscal_reconciliations (payroll_run_id, reconciliation_code);
""");
    }
}
