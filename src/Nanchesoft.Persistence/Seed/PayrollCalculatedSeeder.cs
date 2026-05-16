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
CREATE TABLE IF NOT EXISTS payroll.payroll_source_applications (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    payroll_run_line_id uuid NULL,
    employee_id uuid NOT NULL,
    payroll_period_id uuid NULL,
    payroll_concept_id uuid NULL,
    source_id uuid NULL,
    source_type character varying(40) NOT NULL,
    application_code character varying(40) NOT NULL,
    application_name character varying(180) NOT NULL,
    movement_type character varying(30) NOT NULL,
    quantity numeric(18,4) NOT NULL,
    amount numeric(18,2) NOT NULL,
    taxable_amount numeric(18,2) NOT NULL,
    exempt_amount numeric(18,2) NOT NULL,
    applied_at timestamp with time zone NOT NULL,
    status character varying(30) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_source_applications_run_employee_code_source
ON payroll.payroll_source_applications (payroll_run_id, employee_id, application_code, source_id);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_receipt_controls (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    payroll_run_line_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    receipt_number character varying(40) NOT NULL,
    receipt_status character varying(30) NOT NULL,
    generated_at timestamp with time zone NOT NULL,
    reviewed_at timestamp with time zone NULL,
    delivered_at timestamp with time zone NULL,
    stamped_at timestamp with time zone NULL,
    delivery_channel character varying(40) NOT NULL,
    delivery_reference character varying(120) NOT NULL,
    ack_by character varying(120) NOT NULL,
    net_amount numeric(18,2) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_receipt_controls_runline ON payroll.payroll_receipt_controls (payroll_run_line_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_receipt_controls_run_receipt ON payroll.payroll_receipt_controls (payroll_run_id, receipt_number);
""");

        await dbContext.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS payroll.payroll_run_closings (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    payroll_run_id uuid NOT NULL,
    closing_code character varying(40) NOT NULL,
    closing_date timestamp with time zone NOT NULL,
    employees_included integer NOT NULL,
    gross_amount numeric(18,2) NOT NULL,
    deductions_amount numeric(18,2) NOT NULL,
    net_amount numeric(18,2) NOT NULL,
    source_applications_count integer NOT NULL,
    receipts_generated_count integer NOT NULL,
    issues_detected integer NOT NULL,
    status character varying(30) NOT NULL,
    is_locked boolean NOT NULL,
    locked_at timestamp with time zone NULL,
    closed_by character varying(120) NOT NULL,
    notes character varying(1200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    created_by text NULL,
    updated_at timestamp with time zone NULL,
    updated_by text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_run_closings_run_code ON payroll.payroll_run_closings (payroll_run_id, closing_code);
""");
    }
}
