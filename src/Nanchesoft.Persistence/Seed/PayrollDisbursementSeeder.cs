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
CREATE TABLE IF NOT EXISTS payroll_dispersion_batches (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "BatchCode" character varying(40) NOT NULL,
    "DispersionDate" timestamp with time zone NOT NULL,
    "LayoutFormat" character varying(30) NOT NULL,
    "BankName" character varying(120) NOT NULL,
    "FundingAccount" character varying(60) NOT NULL,
    "BeneficiariesCount" integer NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ApprovedAt" timestamp with time zone NULL,
    "ExportedAt" timestamp with time zone NULL,
    "ConfirmedAt" timestamp with time zone NULL,
    "FileReference" character varying(240) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_dispersion_batches_run_code ON payroll_dispersion_batches ("PayrollRunId", "BatchCode");
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_dispersion_lines (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollDispersionBatchId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "PayrollRunLineId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "Sequence" integer NOT NULL,
    "EmployeeNumber" character varying(40) NOT NULL,
    "BeneficiaryName" character varying(180) NOT NULL,
    "BankName" character varying(120) NOT NULL,
    "BankAccount" character varying(60) NOT NULL,
    "Clabe" character varying(40) NOT NULL,
    "NetAmount" numeric(18,2) NOT NULL,
    "PaymentReference" character varying(80) NOT NULL,
    "ValidationStatus" character varying(30) NOT NULL,
    "IsRejected" boolean NOT NULL,
    "PaidAt" timestamp with time zone NULL,
    "Status" character varying(30) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_dispersion_lines_batch_sequence ON payroll_dispersion_lines ("PayrollDispersionBatchId", "Sequence");
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_dispersion_lines_runline ON payroll_dispersion_lines ("PayrollRunLineId");
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_accounting_postings (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "PostingCode" character varying(40) NOT NULL,
    "PostingDate" timestamp with time zone NOT NULL,
    "LedgerBook" character varying(40) NOT NULL,
    "JournalNumber" character varying(40) NOT NULL,
    "DebitAmount" numeric(18,2) NOT NULL,
    "CreditAmount" numeric(18,2) NOT NULL,
    "LinesCount" integer NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ExportedAt" timestamp with time zone NULL,
    "PostedAt" timestamp with time zone NULL,
    "LockedAt" timestamp with time zone NULL,
    "ExportReference" character varying(180) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_accounting_postings_run_code ON payroll_accounting_postings ("PayrollRunId", "PostingCode");
""");
    }
}
