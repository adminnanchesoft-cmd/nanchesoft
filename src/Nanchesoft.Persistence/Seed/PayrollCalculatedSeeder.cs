using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class PayrollCalculatedSeeder
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

        var lines = await dbContext.PayrollRunLines
            .Where(x => x.PayrollRunId == run.Id)
            .OrderBy(x => x.CreatedAt)
            .Take(3)
            .ToListAsync();

        if (lines.Count == 0)
            return;

        var details = await dbContext.PayrollRunLineDetails
            .Where(x => x.PayrollRunId == run.Id)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        foreach (var line in lines)
        {
            var employee = await dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == line.EmployeeId);
            if (employee is null)
                continue;

            var hasReceipt = await dbContext.PayrollReceiptControls.AnyAsync(x => x.PayrollRunLineId == line.Id);
            if (!hasReceipt)
            {
                dbContext.PayrollReceiptControls.Add(new PayrollReceiptControl
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    PayrollRunId = run.Id,
                    PayrollRunLineId = line.Id,
                    EmployeeId = line.EmployeeId,
                    ReceiptNumber = $"REC-{run.Folio}-{employee.EmployeeNumber}",
                    ReceiptStatus = "generated",
                    GeneratedAt = DateTime.UtcNow,
                    DeliveryChannel = "portal",
                    DeliveryReference = $"PORTAL-{employee.EmployeeNumber}",
                    AckBy = string.Empty,
                    NetAmount = line.NetAmount,
                    Notes = "Recibo generado desde seed enterprise de nómina calculada.",
                    CreatedBy = "seed"
                });
            }

            var lineDetails = details.Where(x => x.PayrollRunLineId == line.Id).ToList();
            foreach (var detail in lineDetails)
            {
                var hasApplication = await dbContext.PayrollSourceApplications.AnyAsync(x =>
                    x.PayrollRunId == run.Id &&
                    x.PayrollRunLineId == line.Id &&
                    x.PayrollConceptId == detail.PayrollConceptId &&
                    x.ApplicationCode == detail.ConceptCode &&
                    x.SourceType == (detail.IsGenerated ? "generated_detail" : "manual_detail"));

                if (hasApplication)
                    continue;

                dbContext.PayrollSourceApplications.Add(new PayrollSourceApplication
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    PayrollRunId = run.Id,
                    PayrollRunLineId = line.Id,
                    EmployeeId = line.EmployeeId,
                    PayrollPeriodId = run.PayrollPeriodId,
                    PayrollConceptId = detail.PayrollConceptId,
                    SourceId = detail.Id,
                    SourceType = detail.IsGenerated ? "generated_detail" : "manual_detail",
                    ApplicationCode = detail.ConceptCode,
                    ApplicationName = detail.ConceptName,
                    MovementType = detail.ConceptType,
                    Quantity = detail.Quantity,
                    Amount = detail.Amount,
                    TaxableAmount = detail.TaxableAmount,
                    ExemptAmount = detail.ExemptAmount,
                    AppliedAt = DateTime.UtcNow,
                    Status = "applied",
                    Notes = "Aplicación consolidada desde el desglose de recibo para trazabilidad enterprise.",
                    CreatedBy = "seed"
                });
            }
        }

        var hasClosing = await dbContext.PayrollRunClosings.AnyAsync(x => x.PayrollRunId == run.Id && x.ClosingCode == $"CLOSE-{run.Folio}");
        if (!hasClosing)
        {
            var sourceCount = await dbContext.PayrollSourceApplications.CountAsync(x => x.PayrollRunId == run.Id);
            var receiptCount = await dbContext.PayrollReceiptControls.CountAsync(x => x.PayrollRunId == run.Id);
            dbContext.PayrollRunClosings.Add(new PayrollRunClosing
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollRunId = run.Id,
                ClosingCode = $"CLOSE-{run.Folio}",
                ClosingDate = DateTime.UtcNow,
                EmployeesIncluded = run.EmployeeCount,
                GrossAmount = run.GrossAmount,
                DeductionsAmount = run.DeductionsAmount,
                NetAmount = run.NetAmount,
                SourceApplicationsCount = sourceCount,
                ReceiptsGeneratedCount = receiptCount,
                IssuesDetected = 0,
                Status = "review",
                IsLocked = false,
                LockedAt = null,
                ClosedBy = string.Empty,
                Notes = "Cierre preliminar seed para validación de recibos, aplicaciones y control del proceso.",
                CreatedBy = "seed"
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureSchemaAsync(NanchesoftDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_source_applications (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "PayrollRunLineId" uuid NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollPeriodId" uuid NULL,
    "PayrollConceptId" uuid NULL,
    "SourceId" uuid NULL,
    "SourceType" character varying(40) NOT NULL,
    "ApplicationCode" character varying(40) NOT NULL,
    "ApplicationName" character varying(180) NOT NULL,
    "MovementType" character varying(30) NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "TaxableAmount" numeric(18,2) NOT NULL,
    "ExemptAmount" numeric(18,2) NOT NULL,
    "AppliedAt" timestamp with time zone NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_source_applications_run_employee_code_source
ON payroll_source_applications ("PayrollRunId", "EmployeeId", "ApplicationCode", "SourceId");
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_receipt_controls (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "PayrollRunLineId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "ReceiptNumber" character varying(40) NOT NULL,
    "ReceiptStatus" character varying(30) NOT NULL,
    "GeneratedAt" timestamp with time zone NOT NULL,
    "ReviewedAt" timestamp with time zone NULL,
    "DeliveredAt" timestamp with time zone NULL,
    "StampedAt" timestamp with time zone NULL,
    "DeliveryChannel" character varying(40) NOT NULL,
    "DeliveryReference" character varying(120) NOT NULL,
    "AckBy" character varying(120) NOT NULL,
    "NetAmount" numeric(18,2) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_receipt_controls_runline ON payroll_receipt_controls ("PayrollRunLineId");
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_receipt_controls_run_receipt ON payroll_receipt_controls ("PayrollRunId", "ReceiptNumber");
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll_run_closings (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "ClosingCode" character varying(40) NOT NULL,
    "ClosingDate" timestamp with time zone NOT NULL,
    "EmployeesIncluded" integer NOT NULL,
    "GrossAmount" numeric(18,2) NOT NULL,
    "DeductionsAmount" numeric(18,2) NOT NULL,
    "NetAmount" numeric(18,2) NOT NULL,
    "SourceApplicationsCount" integer NOT NULL,
    "ReceiptsGeneratedCount" integer NOT NULL,
    "IssuesDetected" integer NOT NULL,
    "Status" character varying(30) NOT NULL,
    "IsLocked" boolean NOT NULL,
    "LockedAt" timestamp with time zone NULL,
    "ClosedBy" character varying(120) NOT NULL,
    "Notes" character varying(1200) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_run_closings_run_code ON payroll_run_closings ("PayrollRunId", "ClosingCode");
""");
    }
}
