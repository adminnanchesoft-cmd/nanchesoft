using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollCalculatedEndpoints
{
    public static IEndpointRouteBuilder MapPayrollCalculatedEndpoints(this IEndpointRouteBuilder app)
    {
        var sourceApps = app.MapGroup("/api/payroll/source-applications").WithTags("PayrollSourceApplications");
        sourceApps.MapGet("/", GetPayrollSourceApplicationsAsync);
        sourceApps.MapPost("/", CreatePayrollSourceApplicationAsync);
        sourceApps.MapPut("/{id:guid}", UpdatePayrollSourceApplicationAsync);
        sourceApps.MapDelete("/{id:guid}", DeletePayrollSourceApplicationAsync);

        var receiptControl = app.MapGroup("/api/payroll/receipt-control").WithTags("PayrollReceiptControl");
        receiptControl.MapGet("/", GetPayrollReceiptControlsAsync);
        receiptControl.MapPost("/", CreatePayrollReceiptControlAsync);
        receiptControl.MapPut("/{id:guid}", UpdatePayrollReceiptControlAsync);
        receiptControl.MapDelete("/{id:guid}", DeletePayrollReceiptControlAsync);
        receiptControl.MapPost("/runs/{runId:guid}/generate", GenerateReceiptControlsAsync);

        var runClosings = app.MapGroup("/api/payroll/run-closings").WithTags("PayrollRunClosings");
        runClosings.MapGet("/", GetPayrollRunClosingsAsync);
        runClosings.MapPost("/", CreatePayrollRunClosingAsync);
        runClosings.MapPut("/{id:guid}", UpdatePayrollRunClosingAsync);
        runClosings.MapDelete("/{id:guid}", DeletePayrollRunClosingAsync);
        runClosings.MapPost("/runs/{runId:guid}/generate", GeneratePayrollRunClosingAsync);

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return company is null ? (null, null) : (company.TenantId, company.Id);
    }

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeLower(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static async Task<IResult> GetPayrollSourceApplicationsAsync(NanchesoftDbContext db)
    {
        var rows = await db.PayrollSourceApplications.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .Include(x => x.PayrollRunLine)
            .Include(x => x.Employee)
            .Include(x => x.PayrollPeriod)
            .Include(x => x.PayrollConcept)
            .OrderByDescending(x => x.AppliedAt)
            .Select(x => new PayrollSourceApplicationDto
            {
                PayrollSourceApplicationId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PayrollRunLineId = x.PayrollRunLineId,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                PayrollConceptId = x.PayrollConceptId,
                PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : string.Empty,
                SourceId = x.SourceId,
                SourceType = x.SourceType,
                ApplicationCode = x.ApplicationCode,
                ApplicationName = x.ApplicationName,
                MovementType = x.MovementType,
                Quantity = x.Quantity,
                Amount = x.Amount,
                TaxableAmount = x.TaxableAmount,
                ExemptAmount = x.ExemptAmount,
                AppliedAt = x.AppliedAt,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollSourceApplicationAsync(PayrollSourceApplicationRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollRunId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa, proceso y colaborador son obligatorios." });

        var entity = new PayrollSourceApplication
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            PayrollRunLineId = request.PayrollRunLineId,
            EmployeeId = request.EmployeeId.Value,
            PayrollPeriodId = request.PayrollPeriodId,
            PayrollConceptId = request.PayrollConceptId,
            SourceId = request.SourceId,
            SourceType = NormalizeLower(request.SourceType, "manual"),
            ApplicationCode = NormalizeText(request.ApplicationCode),
            ApplicationName = NormalizeText(request.ApplicationName),
            MovementType = NormalizeLower(request.MovementType, "perception"),
            Quantity = request.Quantity <= 0m ? 1m : request.Quantity,
            Amount = request.Amount,
            TaxableAmount = request.TaxableAmount,
            ExemptAmount = request.ExemptAmount,
            AppliedAt = request.AppliedAt ?? DateTime.UtcNow,
            Status = NormalizeLower(request.Status, "applied"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollSourceApplications.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollSourceApplicationAsync(Guid id, PayrollSourceApplicationRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollSourceApplications.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la aplicación de fuente." });

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PayrollRunLineId = request.PayrollRunLineId ?? entity.PayrollRunLineId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.PayrollPeriodId = request.PayrollPeriodId ?? entity.PayrollPeriodId;
        entity.PayrollConceptId = request.PayrollConceptId ?? entity.PayrollConceptId;
        entity.SourceId = request.SourceId ?? entity.SourceId;
        entity.SourceType = NormalizeLower(request.SourceType, entity.SourceType);
        entity.ApplicationCode = NormalizeText(request.ApplicationCode, entity.ApplicationCode);
        entity.ApplicationName = NormalizeText(request.ApplicationName, entity.ApplicationName);
        entity.MovementType = NormalizeLower(request.MovementType, entity.MovementType);
        entity.Quantity = request.Quantity <= 0m ? entity.Quantity : request.Quantity;
        entity.Amount = request.Amount;
        entity.TaxableAmount = request.TaxableAmount;
        entity.ExemptAmount = request.ExemptAmount;
        entity.AppliedAt = request.AppliedAt ?? entity.AppliedAt;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollSourceApplicationAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollSourceApplications.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la aplicación de fuente." });

        db.PayrollSourceApplications.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPayrollReceiptControlsAsync(NanchesoftDbContext db)
    {
        var rows = await db.PayrollReceiptControls.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .Include(x => x.Employee)
            .OrderByDescending(x => x.GeneratedAt)
            .Select(x => new PayrollReceiptControlDto
            {
                PayrollReceiptControlId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PayrollRunLineId = x.PayrollRunLineId,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                ReceiptNumber = x.ReceiptNumber,
                ReceiptStatus = x.ReceiptStatus,
                GeneratedAt = x.GeneratedAt,
                ReviewedAt = x.ReviewedAt,
                DeliveredAt = x.DeliveredAt,
                StampedAt = x.StampedAt,
                DeliveryChannel = x.DeliveryChannel,
                DeliveryReference = x.DeliveryReference,
                AckBy = x.AckBy,
                NetAmount = x.NetAmount,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollReceiptControlAsync(PayrollReceiptControlRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollRunId.HasValue || !request.PayrollRunLineId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa, proceso, línea y colaborador son obligatorios." });

        var entity = new PayrollReceiptControl
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            PayrollRunLineId = request.PayrollRunLineId.Value,
            EmployeeId = request.EmployeeId.Value,
            ReceiptNumber = NormalizeText(request.ReceiptNumber),
            ReceiptStatus = NormalizeLower(request.ReceiptStatus, "generated"),
            GeneratedAt = request.GeneratedAt ?? DateTime.UtcNow,
            ReviewedAt = request.ReviewedAt,
            DeliveredAt = request.DeliveredAt,
            StampedAt = request.StampedAt,
            DeliveryChannel = NormalizeLower(request.DeliveryChannel),
            DeliveryReference = NormalizeText(request.DeliveryReference),
            AckBy = NormalizeText(request.AckBy),
            NetAmount = request.NetAmount,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollReceiptControls.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollReceiptControlAsync(Guid id, PayrollReceiptControlRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollReceiptControls.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el control de recibo." });

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PayrollRunLineId = request.PayrollRunLineId ?? entity.PayrollRunLineId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.ReceiptNumber = NormalizeText(request.ReceiptNumber, entity.ReceiptNumber);
        entity.ReceiptStatus = NormalizeLower(request.ReceiptStatus, entity.ReceiptStatus);
        entity.GeneratedAt = request.GeneratedAt ?? entity.GeneratedAt;
        entity.ReviewedAt = request.ReviewedAt ?? entity.ReviewedAt;
        entity.DeliveredAt = request.DeliveredAt ?? entity.DeliveredAt;
        entity.StampedAt = request.StampedAt ?? entity.StampedAt;
        entity.DeliveryChannel = NormalizeLower(request.DeliveryChannel, entity.DeliveryChannel);
        entity.DeliveryReference = NormalizeText(request.DeliveryReference, entity.DeliveryReference);
        entity.AckBy = NormalizeText(request.AckBy, entity.AckBy);
        entity.NetAmount = request.NetAmount;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollReceiptControlAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollReceiptControls.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el control de recibo." });

        db.PayrollReceiptControls.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GenerateReceiptControlsAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró la corrida de nómina." });

        var lines = await db.PayrollRunLines.Where(x => x.PayrollRunId == runId).ToListAsync();
        var employees = await db.Employees.Where(x => lines.Select(l => l.EmployeeId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x);
        var created = 0;

        foreach (var line in lines)
        {
            if (await db.PayrollReceiptControls.AnyAsync(x => x.PayrollRunLineId == line.Id))
                continue;

            employees.TryGetValue(line.EmployeeId, out var employee);
            db.PayrollReceiptControls.Add(new PayrollReceiptControl
            {
                TenantId = run.TenantId,
                CompanyId = run.CompanyId,
                PayrollRunId = run.Id,
                PayrollRunLineId = line.Id,
                EmployeeId = line.EmployeeId,
                ReceiptNumber = $"REC-{run.Folio}-{employee?.EmployeeNumber ?? line.EmployeeId.ToString()[..6]}",
                ReceiptStatus = "generated",
                GeneratedAt = DateTime.UtcNow,
                DeliveryChannel = "portal",
                DeliveryReference = string.Empty,
                AckBy = string.Empty,
                NetAmount = line.NetAmount,
                Notes = "Generado automáticamente desde el control de recibos enterprise.",
                CreatedBy = "web-api"
            });
            created++;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, created });
    }

    private static async Task<IResult> GetPayrollRunClosingsAsync(NanchesoftDbContext db)
    {
        var rows = await db.PayrollRunClosings.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .OrderByDescending(x => x.ClosingDate)
            .Select(x => new PayrollRunClosingDto
            {
                PayrollRunClosingId = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                ClosingCode = x.ClosingCode,
                ClosingDate = x.ClosingDate,
                EmployeesIncluded = x.EmployeesIncluded,
                GrossAmount = x.GrossAmount,
                DeductionsAmount = x.DeductionsAmount,
                NetAmount = x.NetAmount,
                SourceApplicationsCount = x.SourceApplicationsCount,
                ReceiptsGeneratedCount = x.ReceiptsGeneratedCount,
                IssuesDetected = x.IssuesDetected,
                Status = x.Status,
                IsLocked = x.IsLocked,
                LockedAt = x.LockedAt,
                ClosedBy = x.ClosedBy,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollRunClosingAsync(PayrollRunClosingRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollRunId.HasValue)
            return Results.BadRequest(new { message = "Empresa y proceso de nómina son obligatorios." });

        var entity = new PayrollRunClosing
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            ClosingCode = NormalizeText(request.ClosingCode),
            ClosingDate = request.ClosingDate ?? DateTime.UtcNow,
            EmployeesIncluded = request.EmployeesIncluded,
            GrossAmount = request.GrossAmount,
            DeductionsAmount = request.DeductionsAmount,
            NetAmount = request.NetAmount,
            SourceApplicationsCount = request.SourceApplicationsCount,
            ReceiptsGeneratedCount = request.ReceiptsGeneratedCount,
            IssuesDetected = request.IssuesDetected,
            Status = NormalizeLower(request.Status, "draft"),
            IsLocked = request.IsLocked,
            LockedAt = request.LockedAt,
            ClosedBy = NormalizeText(request.ClosedBy),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollRunClosings.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollRunClosingAsync(Guid id, PayrollRunClosingRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRunClosings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el cierre de nómina." });

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.ClosingCode = NormalizeText(request.ClosingCode, entity.ClosingCode);
        entity.ClosingDate = request.ClosingDate ?? entity.ClosingDate;
        entity.EmployeesIncluded = request.EmployeesIncluded;
        entity.GrossAmount = request.GrossAmount;
        entity.DeductionsAmount = request.DeductionsAmount;
        entity.NetAmount = request.NetAmount;
        entity.SourceApplicationsCount = request.SourceApplicationsCount;
        entity.ReceiptsGeneratedCount = request.ReceiptsGeneratedCount;
        entity.IssuesDetected = request.IssuesDetected;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.IsLocked = request.IsLocked;
        entity.LockedAt = request.LockedAt ?? entity.LockedAt;
        entity.ClosedBy = NormalizeText(request.ClosedBy, entity.ClosedBy);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollRunClosingAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRunClosings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el cierre de nómina." });

        db.PayrollRunClosings.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePayrollRunClosingAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró la corrida de nómina." });

        var existing = await db.PayrollRunClosings.FirstOrDefaultAsync(x => x.PayrollRunId == runId && x.ClosingCode == $"CLOSE-{run.Folio}");
        var sourceCount = await db.PayrollSourceApplications.CountAsync(x => x.PayrollRunId == runId);
        var receiptCount = await db.PayrollReceiptControls.CountAsync(x => x.PayrollRunId == runId);

        if (existing is null)
        {
            existing = new PayrollRunClosing
            {
                TenantId = run.TenantId,
                CompanyId = run.CompanyId,
                PayrollRunId = run.Id,
                ClosingCode = $"CLOSE-{run.Folio}",
                CreatedBy = "web-api"
            };
            db.PayrollRunClosings.Add(existing);
        }

        existing.ClosingDate = DateTime.UtcNow;
        existing.EmployeesIncluded = run.EmployeeCount;
        existing.GrossAmount = run.GrossAmount;
        existing.DeductionsAmount = run.DeductionsAmount;
        existing.NetAmount = run.NetAmount;
        existing.SourceApplicationsCount = sourceCount;
        existing.ReceiptsGeneratedCount = receiptCount;
        existing.IssuesDetected = Math.Max(0, run.EmployeeCount - receiptCount);
        existing.Status = receiptCount >= run.EmployeeCount ? "ready_to_close" : "review";
        existing.IsLocked = false;
        existing.LockedAt = null;
        existing.ClosedBy = string.Empty;
        existing.Notes = "Cierre recalculado automáticamente desde la corrida y su control de recibos.";
        existing.IsActive = true;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = existing.Id });
    }
}

public class PayrollSourceApplicationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public Guid? SourceId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string ApplicationCode { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollSourceApplicationDto : PayrollSourceApplicationRequest
{
    public Guid PayrollSourceApplicationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PayrollPeriodName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string PayrollConceptName { get; set; } = string.Empty;
}

public class PayrollReceiptControlRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ReceiptStatus { get; set; } = string.Empty;
    public DateTime? GeneratedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? StampedAt { get; set; }
    public string DeliveryChannel { get; set; } = string.Empty;
    public string DeliveryReference { get; set; } = string.Empty;
    public string AckBy { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollReceiptControlDto : PayrollReceiptControlRequest
{
    public Guid PayrollReceiptControlId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class PayrollRunClosingRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public string ClosingCode { get; set; } = string.Empty;
    public DateTime? ClosingDate { get; set; }
    public int EmployeesIncluded { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int SourceApplicationsCount { get; set; }
    public int ReceiptsGeneratedCount { get; set; }
    public int IssuesDetected { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRunClosingDto : PayrollRunClosingRequest
{
    public Guid PayrollRunClosingId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
}
