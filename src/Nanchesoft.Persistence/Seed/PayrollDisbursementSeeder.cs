using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class PayrollDisbursementSeeder
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

        var batchCode = $"DSP-{run.Folio}";
        var batch = await dbContext.PayrollDispersionBatches.FirstOrDefaultAsync(x => x.PayrollRunId == run.Id && x.BatchCode == batchCode);
        if (batch is null)
        {
            batch = new PayrollDispersionBatch
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollRunId = run.Id,
                BatchCode = batchCode,
                DispersionDate = DateTime.UtcNow,
                LayoutFormat = "spei",
                BankName = "BANCO PRINCIPAL",
                FundingAccount = "000123456789",
                BeneficiariesCount = Math.Max(run.EmployeeCount, 0),
                TotalAmount = run.NetAmount,
                Status = "generated",
                FileReference = $"{batchCode}.txt",
                Notes = "Lote de dispersión seed enterprise para tesorería de nómina.",
                CreatedBy = "seed"
            };
            dbContext.PayrollDispersionBatches.Add(batch);
            await dbContext.SaveChangesAsync();
        }

        var runLines = await dbContext.PayrollRunLines
            .Where(x => x.PayrollRunId == run.Id)
            .OrderBy(x => x.CreatedAt)
            .Take(5)
            .ToListAsync();

        var sequence = 1;
        foreach (var line in runLines)
        {
            var exists = await dbContext.PayrollDispersionLines.AnyAsync(x => x.PayrollRunLineId == line.Id);
            if (exists)
            {
                sequence++;
                continue;
            }

            var employee = await dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == line.EmployeeId);
            if (employee is null)
            {
                sequence++;
                continue;
            }

            dbContext.PayrollDispersionLines.Add(new PayrollDispersionLine
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollDispersionBatchId = batch.Id,
                PayrollRunId = run.Id,
                PayrollRunLineId = line.Id,
                EmployeeId = employee.Id,
                Sequence = sequence,
                EmployeeNumber = employee.EmployeeNumber,
                BeneficiaryName = (employee.FirstName + " " + employee.LastName).Trim(),
                BankName = "BANCO DESTINO",
                BankAccount = $"000000{sequence:0000}",
                Clabe = $"01234567890123456{sequence % 10}",
                NetAmount = line.NetAmount,
                PaymentReference = $"NOM-{run.Folio}-{employee.EmployeeNumber}",
                ValidationStatus = "ready",
                IsRejected = false,
                Status = "pending",
                Notes = "Línea generada desde el proceso de nómina para dispersión bancaria.",
                CreatedBy = "seed"
            });
            sequence++;
        }

        var postingCode = $"POL-{run.Folio}";
        var hasPosting = await dbContext.PayrollAccountingPostings.AnyAsync(x => x.PayrollRunId == run.Id && x.PostingCode == postingCode);
        if (!hasPosting)
        {
            var linesCount = await dbContext.PayrollRunLineDetails.CountAsync(x => x.PayrollRunId == run.Id);
            dbContext.PayrollAccountingPostings.Add(new PayrollAccountingPosting
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollRunId = run.Id,
                PostingCode = postingCode,
                PostingDate = DateTime.UtcNow,
                LedgerBook = "GENERAL",
                JournalNumber = string.Empty,
                DebitAmount = run.GrossAmount,
                CreditAmount = run.GrossAmount,
                LinesCount = linesCount,
                Status = "ready",
                ExportReference = $"{postingCode}.json",
                Notes = "Interfaz contable seed para póliza de nómina y validación de cierre financiero.",
                CreatedBy = "seed"
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureSchemaAsync(NanchesoftDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_dispersion_batches (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    batch_code character varying(40) NOT NULL,
    dispersion_date timestamp with time zone NOT NULL,
    layout_format character varying(30) NOT NULL,
    bank_name character varying(120) NOT NULL,
    funding_account character varying(60) NOT NULL,
    beneficiaries_count integer NOT NULL,
    total_amount numeric(18,2) NOT NULL,
    status character varying(30) NOT NULL,
    approved_at timestamp with time zone NULL,
    exported_at timestamp with time zone NULL,
    confirmed_at timestamp with time zone NULL,
    file_reference character varying(240) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_dispersion_batches_run_code ON payroll.payroll_dispersion_batches (payroll_run_id, batch_code);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_dispersion_lines (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_dispersion_batch_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    payroll_run_line_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    sequence integer NOT NULL,
    employee_number character varying(40) NOT NULL,
    beneficiary_name character varying(180) NOT NULL,
    bank_name character varying(120) NOT NULL,
    bank_account character varying(60) NOT NULL,
    clabe character varying(40) NOT NULL,
    net_amount numeric(18,2) NOT NULL,
    payment_reference character varying(80) NOT NULL,
    validation_status character varying(30) NOT NULL,
    is_rejected boolean NOT NULL,
    paid_at timestamp with time zone NULL,
    status character varying(30) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_dispersion_lines_batch_sequence ON payroll.payroll_dispersion_lines (payroll_dispersion_batch_id, sequence);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_dispersion_lines_runline ON payroll.payroll_dispersion_lines (payroll_run_line_id);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_accounting_postings (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    posting_code character varying(40) NOT NULL,
    posting_date timestamp with time zone NOT NULL,
    ledger_book character varying(40) NOT NULL,
    journal_number character varying(40) NOT NULL,
    debit_amount numeric(18,2) NOT NULL,
    credit_amount numeric(18,2) NOT NULL,
    lines_count integer NOT NULL,
    status character varying(30) NOT NULL,
    exported_at timestamp with time zone NULL,
    posted_at timestamp with time zone NULL,
    locked_at timestamp with time zone NULL,
    export_reference character varying(180) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_accounting_postings_run_code ON payroll.payroll_accounting_postings (payroll_run_id, posting_code);
""");
    }
}
