using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollDisbursementEndpoints
{
    public static IEndpointRouteBuilder MapPayrollDisbursementEndpoints(this IEndpointRouteBuilder app)
    {
        var batches = app.MapGroup("/api/payroll/dispersion-batches").WithTags("PayrollDispersionBatches");
        batches.MapGet("/", GetPayrollDispersionBatchesAsync);
        batches.MapPost("/", CreatePayrollDispersionBatchAsync);
        batches.MapPut("/{id:guid}", UpdatePayrollDispersionBatchAsync);
        batches.MapDelete("/{id:guid}", DeletePayrollDispersionBatchAsync);
        batches.MapPost("/runs/{runId:guid}/generate", GeneratePayrollDispersionBatchAsync);

        var lines = app.MapGroup("/api/payroll/dispersion-lines").WithTags("PayrollDispersionLines");
        lines.MapGet("/", GetPayrollDispersionLinesAsync);
        lines.MapPost("/", CreatePayrollDispersionLineAsync);
        lines.MapPut("/{id:guid}", UpdatePayrollDispersionLineAsync);
        lines.MapDelete("/{id:guid}", DeletePayrollDispersionLineAsync);
        lines.MapPost("/batches/{batchId:guid}/generate", GeneratePayrollDispersionLinesAsync);

        var postings = app.MapGroup("/api/payroll/accounting-postings").WithTags("PayrollAccountingPostings");
        postings.MapGet("/", GetPayrollAccountingPostingsAsync);
        postings.MapPost("/", CreatePayrollAccountingPostingAsync);
        postings.MapPut("/{id:guid}", UpdatePayrollAccountingPostingAsync);
        postings.MapDelete("/{id:guid}", DeletePayrollAccountingPostingAsync);
        postings.MapPost("/runs/{runId:guid}/generate", GeneratePayrollAccountingPostingAsync);

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);

        if (companyId.HasValue)
        {
            if (!tenantId.HasValue)
                tenantId = await db.Companies.Where(x => x.Id == companyId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefaultAsync();
            return (tenantId, companyId);
        }

        if (tenantId.HasValue)
        {
            var comp = await db.Companies.Where(x => x.TenantId == tenantId.Value).OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
            if (comp is not null)
                return (tenantId, comp.Id);
        }

        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        if (company is null) return (null, null);
        return (company.TenantId, company.Id);
    }

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeLower(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static async Task<IResult> GetPayrollDispersionBatchesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var rows = await db.PayrollDispersionBatches.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId
                     && (!scope.CompanyId.HasValue || x.CompanyId == scope.CompanyId.Value))
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .OrderByDescending(x => x.DispersionDate)
            .Select(x => new PayrollDispersionBatchDto
            {
                PayrollDispersionBatchId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                BatchCode = x.BatchCode,
                DispersionDate = x.DispersionDate,
                LayoutFormat = x.LayoutFormat,
                BankName = x.BankName,
                FundingAccount = x.FundingAccount,
                BeneficiariesCount = x.BeneficiariesCount,
                TotalAmount = x.TotalAmount,
                Status = x.Status,
                ApprovedAt = x.ApprovedAt,
                ExportedAt = x.ExportedAt,
                ConfirmedAt = x.ConfirmedAt,
                FileReference = x.FileReference,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollDispersionBatchAsync(HttpContext httpContext, PayrollDispersionBatchRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollRunId.HasValue)
            return Results.BadRequest(new { message = "Empresa y proceso de nómina son obligatorios." });

        var entity = new PayrollDispersionBatch
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            BatchCode = NormalizeText(request.BatchCode),
            DispersionDate = request.DispersionDate ?? DateTime.UtcNow,
            LayoutFormat = NormalizeLower(request.LayoutFormat, "spei"),
            BankName = NormalizeText(request.BankName),
            FundingAccount = NormalizeText(request.FundingAccount),
            BeneficiariesCount = request.BeneficiariesCount,
            TotalAmount = request.TotalAmount,
            Status = NormalizeLower(request.Status, "draft"),
            ApprovedAt = request.ApprovedAt,
            ExportedAt = request.ExportedAt,
            ConfirmedAt = request.ConfirmedAt,
            FileReference = NormalizeText(request.FileReference),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollDispersionBatches.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollDispersionBatchAsync(Guid id, PayrollDispersionBatchRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollDispersionBatches.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el lote de dispersión." });

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.BatchCode = NormalizeText(request.BatchCode, entity.BatchCode);
        entity.DispersionDate = request.DispersionDate ?? entity.DispersionDate;
        entity.LayoutFormat = NormalizeLower(request.LayoutFormat, entity.LayoutFormat);
        entity.BankName = NormalizeText(request.BankName, entity.BankName);
        entity.FundingAccount = NormalizeText(request.FundingAccount, entity.FundingAccount);
        entity.BeneficiariesCount = request.BeneficiariesCount <= 0 ? entity.BeneficiariesCount : request.BeneficiariesCount;
        entity.TotalAmount = request.TotalAmount;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.ApprovedAt = request.ApprovedAt ?? entity.ApprovedAt;
        entity.ExportedAt = request.ExportedAt ?? entity.ExportedAt;
        entity.ConfirmedAt = request.ConfirmedAt ?? entity.ConfirmedAt;
        entity.FileReference = NormalizeText(request.FileReference, entity.FileReference);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollDispersionBatchAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollDispersionBatches.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el lote de dispersión." });

        db.PayrollDispersionBatches.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePayrollDispersionBatchAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró el proceso de nómina." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.CompanyId);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa del proceso." });

        var code = $"DSP-{run.Folio}";
        var existing = await db.PayrollDispersionBatches.FirstOrDefaultAsync(x => x.PayrollRunId == runId && x.BatchCode == code);
        if (existing is not null)
            return Results.Ok(new { success = true, id = existing.Id, message = "El lote ya existía." });

        var entity = new PayrollDispersionBatch
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            PayrollRunId = run.Id,
            BatchCode = code,
            DispersionDate = DateTime.UtcNow,
            LayoutFormat = "spei",
            BankName = "BANCO PRINCIPAL",
            FundingAccount = "000123456789",
            BeneficiariesCount = run.EmployeeCount,
            TotalAmount = run.NetAmount,
            Status = "generated",
            FileReference = $"{code}.txt",
            Notes = "Lote generado desde endpoint para dispersión bancaria de nómina.",
            CreatedBy = "web-api"
        };

        db.PayrollDispersionBatches.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> GetPayrollDispersionLinesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var rows = await db.PayrollDispersionLines.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId
                     && (!scope.CompanyId.HasValue || x.CompanyId == scope.CompanyId.Value))
            .Include(x => x.Company)
            .Include(x => x.PayrollDispersionBatch)
            .Include(x => x.PayrollRun)
            .Include(x => x.Employee)
            .OrderBy(x => x.Sequence)
            .Select(x => new PayrollDispersionLineDto
            {
                PayrollDispersionLineId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollDispersionBatchId = x.PayrollDispersionBatchId,
                BatchCode = x.PayrollDispersionBatch != null ? x.PayrollDispersionBatch.BatchCode : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PayrollRunLineId = x.PayrollRunLineId,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                Sequence = x.Sequence,
                EmployeeNumber = x.EmployeeNumber,
                BeneficiaryName = x.BeneficiaryName,
                BankName = x.BankName,
                BankAccount = x.BankAccount,
                Clabe = x.Clabe,
                NetAmount = x.NetAmount,
                PaymentReference = x.PaymentReference,
                ValidationStatus = x.ValidationStatus,
                IsRejected = x.IsRejected,
                PaidAt = x.PaidAt,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollDispersionLineAsync(HttpContext httpContext, PayrollDispersionLineRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollDispersionBatchId.HasValue || !request.PayrollRunId.HasValue || !request.PayrollRunLineId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa, lote, proceso, línea y colaborador son obligatorios." });

        var entity = new PayrollDispersionLine
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollDispersionBatchId = request.PayrollDispersionBatchId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            PayrollRunLineId = request.PayrollRunLineId.Value,
            EmployeeId = request.EmployeeId.Value,
            Sequence = request.Sequence <= 0 ? 1 : request.Sequence,
            EmployeeNumber = NormalizeText(request.EmployeeNumber),
            BeneficiaryName = NormalizeText(request.BeneficiaryName),
            BankName = NormalizeText(request.BankName),
            BankAccount = NormalizeText(request.BankAccount),
            Clabe = NormalizeText(request.Clabe),
            NetAmount = request.NetAmount,
            PaymentReference = NormalizeText(request.PaymentReference),
            ValidationStatus = NormalizeLower(request.ValidationStatus, "ready"),
            IsRejected = request.IsRejected,
            PaidAt = request.PaidAt,
            Status = NormalizeLower(request.Status, "pending"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollDispersionLines.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollDispersionLineAsync(Guid id, PayrollDispersionLineRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollDispersionLines.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la línea de dispersión." });

        entity.PayrollDispersionBatchId = request.PayrollDispersionBatchId ?? entity.PayrollDispersionBatchId;
        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PayrollRunLineId = request.PayrollRunLineId ?? entity.PayrollRunLineId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.Sequence = request.Sequence <= 0 ? entity.Sequence : request.Sequence;
        entity.EmployeeNumber = NormalizeText(request.EmployeeNumber, entity.EmployeeNumber);
        entity.BeneficiaryName = NormalizeText(request.BeneficiaryName, entity.BeneficiaryName);
        entity.BankName = NormalizeText(request.BankName, entity.BankName);
        entity.BankAccount = NormalizeText(request.BankAccount, entity.BankAccount);
        entity.Clabe = NormalizeText(request.Clabe, entity.Clabe);
        entity.NetAmount = request.NetAmount;
        entity.PaymentReference = NormalizeText(request.PaymentReference, entity.PaymentReference);
        entity.ValidationStatus = NormalizeLower(request.ValidationStatus, entity.ValidationStatus);
        entity.IsRejected = request.IsRejected;
        entity.PaidAt = request.PaidAt ?? entity.PaidAt;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollDispersionLineAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollDispersionLines.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la línea de dispersión." });

        db.PayrollDispersionLines.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePayrollDispersionLinesAsync(Guid batchId, NanchesoftDbContext db)
    {
        var batch = await db.PayrollDispersionBatches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == batchId);
        if (batch is null)
            return Results.NotFound(new { message = "No se encontró el lote de dispersión." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == batch.CompanyId);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa del lote." });

        var runLines = await db.PayrollRunLines.Where(x => x.PayrollRunId == batch.PayrollRunId).OrderBy(x => x.CreatedAt).ToListAsync();
        var currentCount = await db.PayrollDispersionLines.CountAsync(x => x.PayrollDispersionBatchId == batchId);
        var created = 0;
        var sequence = currentCount + 1;

        foreach (var runLine in runLines)
        {
            var exists = await db.PayrollDispersionLines.AnyAsync(x => x.PayrollRunLineId == runLine.Id);
            if (exists)
                continue;

            var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runLine.EmployeeId);
            if (employee is null)
                continue;

            db.PayrollDispersionLines.Add(new PayrollDispersionLine
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                PayrollDispersionBatchId = batch.Id,
                PayrollRunId = batch.PayrollRunId,
                PayrollRunLineId = runLine.Id,
                EmployeeId = employee.Id,
                Sequence = sequence,
                EmployeeNumber = employee.EmployeeNumber,
                BeneficiaryName = (employee.FirstName + " " + employee.LastName).Trim(),
                BankName = "BANCO DESTINO",
                BankAccount = $"000000{sequence:0000}",
                Clabe = $"01234567890123456{sequence % 10}",
                NetAmount = runLine.NetAmount,
                PaymentReference = $"NOM-{batch.BatchCode}-{employee.EmployeeNumber}",
                ValidationStatus = "ready",
                Status = "pending",
                Notes = "Línea generada desde endpoint para dispersión bancaria.",
                CreatedBy = "web-api"
            });
            created++;
            sequence++;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created });
    }

    private static async Task<IResult> GetPayrollAccountingPostingsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var scope = ApiTenantScope.RequireScope(httpContext);
        if (!scope.IsValid) return scope.Error!;

        var rows = await db.PayrollAccountingPostings.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId
                     && (!scope.CompanyId.HasValue || x.CompanyId == scope.CompanyId.Value))
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .OrderByDescending(x => x.PostingDate)
            .Select(x => new PayrollAccountingPostingDto
            {
                PayrollAccountingPostingId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PostingCode = x.PostingCode,
                PostingDate = x.PostingDate,
                LedgerBook = x.LedgerBook,
                JournalNumber = x.JournalNumber,
                DebitAmount = x.DebitAmount,
                CreditAmount = x.CreditAmount,
                LinesCount = x.LinesCount,
                Status = x.Status,
                ExportedAt = x.ExportedAt,
                PostedAt = x.PostedAt,
                LockedAt = x.LockedAt,
                ExportReference = x.ExportReference,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollAccountingPostingAsync(HttpContext httpContext, PayrollAccountingPostingRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollRunId.HasValue)
            return Results.BadRequest(new { message = "Empresa y proceso son obligatorios." });

        var entity = new PayrollAccountingPosting
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            PostingCode = NormalizeText(request.PostingCode),
            PostingDate = request.PostingDate ?? DateTime.UtcNow,
            LedgerBook = NormalizeText(request.LedgerBook, "GENERAL"),
            JournalNumber = NormalizeText(request.JournalNumber),
            DebitAmount = request.DebitAmount,
            CreditAmount = request.CreditAmount,
            LinesCount = request.LinesCount,
            Status = NormalizeLower(request.Status, "draft"),
            ExportedAt = request.ExportedAt,
            PostedAt = request.PostedAt,
            LockedAt = request.LockedAt,
            ExportReference = NormalizeText(request.ExportReference),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollAccountingPostings.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollAccountingPostingAsync(Guid id, PayrollAccountingPostingRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollAccountingPostings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la póliza de nómina." });

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PostingCode = NormalizeText(request.PostingCode, entity.PostingCode);
        entity.PostingDate = request.PostingDate ?? entity.PostingDate;
        entity.LedgerBook = NormalizeText(request.LedgerBook, entity.LedgerBook);
        entity.JournalNumber = NormalizeText(request.JournalNumber, entity.JournalNumber);
        entity.DebitAmount = request.DebitAmount;
        entity.CreditAmount = request.CreditAmount;
        entity.LinesCount = request.LinesCount <= 0 ? entity.LinesCount : request.LinesCount;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.ExportedAt = request.ExportedAt ?? entity.ExportedAt;
        entity.PostedAt = request.PostedAt ?? entity.PostedAt;
        entity.LockedAt = request.LockedAt ?? entity.LockedAt;
        entity.ExportReference = NormalizeText(request.ExportReference, entity.ExportReference);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollAccountingPostingAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollAccountingPostings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la póliza de nómina." });

        db.PayrollAccountingPostings.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePayrollAccountingPostingAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró el proceso de nómina." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.CompanyId);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa del proceso." });

        var code = $"POL-{run.Folio}";
        var existing = await db.PayrollAccountingPostings.FirstOrDefaultAsync(x => x.PayrollRunId == runId && x.PostingCode == code);
        if (existing is not null)
            return Results.Ok(new { success = true, id = existing.Id, message = "La póliza ya existía." });

        var linesCount = await db.PayrollRunLineDetails.CountAsync(x => x.PayrollRunId == run.Id);
        var entity = new PayrollAccountingPosting
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            PayrollRunId = run.Id,
            PostingCode = code,
            PostingDate = DateTime.UtcNow,
            LedgerBook = "GENERAL",
            JournalNumber = string.Empty,
            DebitAmount = run.GrossAmount,
            CreditAmount = run.GrossAmount,
            LinesCount = linesCount,
            Status = "ready",
            ExportReference = $"{code}.json",
            Notes = "Póliza de nómina generada desde endpoint para traspaso contable enterprise.",
            CreatedBy = "web-api"
        };

        db.PayrollAccountingPostings.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }
}

public class PayrollDispersionBatchRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateTime? DispersionDate { get; set; }
    public string LayoutFormat { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string FundingAccount { get; set; } = string.Empty;
    public int BeneficiariesCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExportedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string FileReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollDispersionBatchDto : PayrollDispersionBatchRequest
{
    public Guid PayrollDispersionBatchId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
}

public class PayrollDispersionLineRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollDispersionBatchId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public int Sequence { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public bool IsRejected { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollDispersionLineDto : PayrollDispersionLineRequest
{
    public Guid PayrollDispersionLineId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class PayrollAccountingPostingRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public string PostingCode { get; set; } = string.Empty;
    public DateTime? PostingDate { get; set; }
    public string LedgerBook { get; set; } = string.Empty;
    public string JournalNumber { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public int LinesCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ExportedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public string ExportReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollAccountingPostingDto : PayrollAccountingPostingRequest
{
    public Guid PayrollAccountingPostingId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
}
