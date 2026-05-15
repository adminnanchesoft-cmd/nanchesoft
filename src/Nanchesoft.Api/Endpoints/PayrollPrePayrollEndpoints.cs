using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollPrePayrollEndpoints
{
    public static IEndpointRouteBuilder MapPayrollPrePayrollEndpoints(this IEndpointRouteBuilder app)
    {
        var summaries = app.MapGroup("/api/payroll/attendance-daily-summaries").WithTags("PayrollAttendanceDailySummaries");
        summaries.MapGet("/", GetAttendanceDailySummariesAsync);
        summaries.MapPost("/", CreateAttendanceDailySummaryAsync);
        summaries.MapPut("/{id:guid}", UpdateAttendanceDailySummaryAsync);
        summaries.MapDelete("/{id:guid}", DeleteAttendanceDailySummaryAsync);

        var adjustments = app.MapGroup("/api/payroll/prepayroll-adjustments").WithTags("PrePayrollAdjustments");
        adjustments.MapGet("/", GetPrePayrollAdjustmentsAsync);
        adjustments.MapPost("/", CreatePrePayrollAdjustmentAsync);
        adjustments.MapPut("/{id:guid}", UpdatePrePayrollAdjustmentAsync);
        adjustments.MapDelete("/{id:guid}", DeletePrePayrollAdjustmentAsync);

        var cutoffs = app.MapGroup("/api/payroll/prepayroll-cutoffs").WithTags("PrePayrollCutoffs");
        cutoffs.MapGet("/", GetPrePayrollCutoffsAsync);
        cutoffs.MapPost("/", CreatePrePayrollCutoffAsync);
        cutoffs.MapPut("/{id:guid}", UpdatePrePayrollCutoffAsync);
        cutoffs.MapDelete("/{id:guid}", DeletePrePayrollCutoffAsync);

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId, Guid? BranchId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        if (company is null)
            return (null, null, null);

        var branchId = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        return (company.TenantId, company.Id, branchId);
    }

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeUpper(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string NormalizeLower(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static DateTime NormalizeUtc(DateTime? value, DateTime fallback)
    {
        var source = value ?? fallback;
        return source.Kind == DateTimeKind.Utc ? source : DateTime.SpecifyKind(source, DateTimeKind.Utc);
    }

    private static async Task<IResult> GetAttendanceDailySummariesAsync(NanchesoftDbContext db)
    {
        var rows = await db.AttendanceDailySummaries.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Employee)
            .Include(x => x.PayrollPeriod)
            .OrderByDescending(x => x.WorkDate)
            .ThenBy(x => x.Employee!.LastName)
            .Select(x => new AttendanceDailySummaryDto
            {
                AttendanceDailySummaryId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                EmployeeNumber = x.Employee != null ? x.Employee.EmployeeNumber : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                WorkDate = x.WorkDate,
                ScheduledEntryTime = x.ScheduledEntryTime,
                ScheduledExitTime = x.ScheduledExitTime,
                FirstPunchDateTime = x.FirstPunchDateTime,
                LastPunchDateTime = x.LastPunchDateTime,
                WorkedHours = x.WorkedHours,
                DelayMinutes = x.DelayMinutes,
                EarlyLeaveMinutes = x.EarlyLeaveMinutes,
                OvertimeHours = x.OvertimeHours,
                AbsenceUnits = x.AbsenceUnits,
                DayType = x.DayType,
                Status = x.Status,
                Source = x.Source,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateAttendanceDailySummaryAsync(AttendanceDailySummaryRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue)
            return Results.BadRequest(new { message = "Empresa y colaborador son obligatorios." });

        var workDate = NormalizeUtc(request.WorkDate, DateTime.UtcNow.Date).Date;
        if (await db.AttendanceDailySummaries.AnyAsync(x => x.CompanyId == companyId.Value && x.EmployeeId == request.EmployeeId.Value && x.WorkDate == workDate))
            return Results.BadRequest(new { message = "Ya existe un resumen diario para el colaborador en esa fecha." });

        var entity = new AttendanceDailySummary
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = request.BranchId ?? context.BranchId,
            EmployeeId = request.EmployeeId.Value,
            PayrollPeriodId = request.PayrollPeriodId,
            WorkDate = workDate,
            ScheduledEntryTime = request.ScheduledEntryTime,
            ScheduledExitTime = request.ScheduledExitTime,
            FirstPunchDateTime = request.FirstPunchDateTime,
            LastPunchDateTime = request.LastPunchDateTime,
            WorkedHours = request.WorkedHours,
            DelayMinutes = request.DelayMinutes,
            EarlyLeaveMinutes = request.EarlyLeaveMinutes,
            OvertimeHours = request.OvertimeHours,
            AbsenceUnits = request.AbsenceUnits,
            DayType = NormalizeLower(request.DayType, "workday"),
            Status = NormalizeLower(request.Status, "calculated"),
            Source = NormalizeLower(request.Source, "manual"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.AttendanceDailySummaries.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateAttendanceDailySummaryAsync(Guid id, AttendanceDailySummaryRequest request, NanchesoftDbContext db)
    {
        var entity = await db.AttendanceDailySummaries.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el resumen diario." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.PayrollPeriodId = request.PayrollPeriodId ?? entity.PayrollPeriodId;
        entity.WorkDate = request.WorkDate.HasValue ? NormalizeUtc(request.WorkDate, entity.WorkDate).Date : entity.WorkDate;
        entity.ScheduledEntryTime = request.ScheduledEntryTime ?? entity.ScheduledEntryTime;
        entity.ScheduledExitTime = request.ScheduledExitTime ?? entity.ScheduledExitTime;
        entity.FirstPunchDateTime = request.FirstPunchDateTime ?? entity.FirstPunchDateTime;
        entity.LastPunchDateTime = request.LastPunchDateTime ?? entity.LastPunchDateTime;
        entity.WorkedHours = request.WorkedHours;
        entity.DelayMinutes = request.DelayMinutes;
        entity.EarlyLeaveMinutes = request.EarlyLeaveMinutes;
        entity.OvertimeHours = request.OvertimeHours;
        entity.AbsenceUnits = request.AbsenceUnits;
        entity.DayType = NormalizeLower(request.DayType, entity.DayType);
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Source = NormalizeLower(request.Source, entity.Source);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteAttendanceDailySummaryAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.AttendanceDailySummaries.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el resumen diario." });

        db.AttendanceDailySummaries.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPrePayrollAdjustmentsAsync(NanchesoftDbContext db)
    {
        var rows = await db.PrePayrollAdjustments.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Employee)
            .Include(x => x.PayrollPeriod)
            .Include(x => x.PayrollConcept)
            .OrderByDescending(x => x.ReferenceDate)
            .ThenBy(x => x.AdjustmentCode)
            .Select(x => new PrePayrollAdjustmentDto
            {
                PrePayrollAdjustmentId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                EmployeeNumber = x.Employee != null ? x.Employee.EmployeeNumber : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                PayrollConceptId = x.PayrollConceptId,
                PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : string.Empty,
                AdjustmentCode = x.AdjustmentCode,
                AdjustmentName = x.AdjustmentName,
                AdjustmentType = x.AdjustmentType,
                CaptureSource = x.CaptureSource,
                ReferenceDate = x.ReferenceDate,
                Quantity = x.Quantity,
                Amount = x.Amount,
                TaxableAmount = x.TaxableAmount,
                ExemptAmount = x.ExemptAmount,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePrePayrollAdjustmentAsync(PrePayrollAdjustmentRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.EmployeeId.HasValue || !request.PayrollPeriodId.HasValue)
            return Results.BadRequest(new { message = "Empresa, colaborador y periodo son obligatorios." });

        var code = NormalizeUpper(request.AdjustmentCode);
        if (string.IsNullOrWhiteSpace(code))
            return Results.BadRequest(new { message = "El código del ajuste es obligatorio." });

        if (await db.PrePayrollAdjustments.AnyAsync(x => x.CompanyId == companyId.Value && x.EmployeeId == request.EmployeeId.Value && x.PayrollPeriodId == request.PayrollPeriodId.Value && x.AdjustmentCode == code))
            return Results.BadRequest(new { message = "Ya existe un ajuste con ese código para el colaborador y periodo." });

        var entity = new PrePayrollAdjustment
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            EmployeeId = request.EmployeeId.Value,
            PayrollPeriodId = request.PayrollPeriodId.Value,
            PayrollConceptId = request.PayrollConceptId,
            AdjustmentCode = code,
            AdjustmentName = NormalizeText(request.AdjustmentName),
            AdjustmentType = NormalizeLower(request.AdjustmentType, "perception"),
            CaptureSource = NormalizeLower(request.CaptureSource, "manual"),
            ReferenceDate = NormalizeUtc(request.ReferenceDate, DateTime.UtcNow),
            Quantity = request.Quantity,
            Amount = request.Amount,
            TaxableAmount = request.TaxableAmount,
            ExemptAmount = request.ExemptAmount,
            Status = NormalizeLower(request.Status, "captured"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PrePayrollAdjustments.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePrePayrollAdjustmentAsync(Guid id, PrePayrollAdjustmentRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PrePayrollAdjustments.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el ajuste de prenómina." });

        var code = NormalizeUpper(request.AdjustmentCode, entity.AdjustmentCode);
        if (await db.PrePayrollAdjustments.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.EmployeeId == (request.EmployeeId ?? entity.EmployeeId) && x.PayrollPeriodId == (request.PayrollPeriodId ?? entity.PayrollPeriodId) && x.AdjustmentCode == code))
            return Results.BadRequest(new { message = "Ya existe otro ajuste con ese código para el colaborador y periodo." });

        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.PayrollPeriodId = request.PayrollPeriodId ?? entity.PayrollPeriodId;
        entity.PayrollConceptId = request.PayrollConceptId ?? entity.PayrollConceptId;
        entity.AdjustmentCode = code;
        entity.AdjustmentName = NormalizeText(request.AdjustmentName, entity.AdjustmentName);
        entity.AdjustmentType = NormalizeLower(request.AdjustmentType, entity.AdjustmentType);
        entity.CaptureSource = NormalizeLower(request.CaptureSource, entity.CaptureSource);
        entity.ReferenceDate = request.ReferenceDate.HasValue ? NormalizeUtc(request.ReferenceDate, entity.ReferenceDate) : entity.ReferenceDate;
        entity.Quantity = request.Quantity;
        entity.Amount = request.Amount;
        entity.TaxableAmount = request.TaxableAmount;
        entity.ExemptAmount = request.ExemptAmount;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePrePayrollAdjustmentAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PrePayrollAdjustments.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el ajuste de prenómina." });

        db.PrePayrollAdjustments.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPrePayrollCutoffsAsync(NanchesoftDbContext db)
    {
        var rows = await db.PrePayrollCutoffs.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.PayrollPeriod)
            .OrderByDescending(x => x.EndDate)
            .ThenBy(x => x.CutoffCode)
            .Select(x => new PrePayrollCutoffDto
            {
                PrePayrollCutoffId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                PayrollPeriodId = x.PayrollPeriodId,
                PayrollPeriodName = x.PayrollPeriod != null ? x.PayrollPeriod.Name : string.Empty,
                CutoffCode = x.CutoffCode,
                CutoffName = x.CutoffName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                EmployeesReviewed = x.EmployeesReviewed,
                IncidentsDetected = x.IncidentsDetected,
                WorkedDaysTotal = x.WorkedDaysTotal,
                OvertimeHoursTotal = x.OvertimeHoursTotal,
                Status = x.Status,
                IsClosed = x.IsClosed,
                ClosedAt = x.ClosedAt,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePrePayrollCutoffAsync(PrePayrollCutoffRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;
        if (!tenantId.HasValue || !companyId.HasValue || !request.PayrollPeriodId.HasValue)
            return Results.BadRequest(new { message = "Empresa y periodo son obligatorios." });

        var code = NormalizeUpper(request.CutoffCode);
        if (string.IsNullOrWhiteSpace(code))
            return Results.BadRequest(new { message = "El código del corte es obligatorio." });

        if (await db.PrePayrollCutoffs.AnyAsync(x => x.CompanyId == companyId.Value && x.PayrollPeriodId == request.PayrollPeriodId.Value && x.CutoffCode == code))
            return Results.BadRequest(new { message = "Ya existe un corte con ese código para el periodo." });

        var entity = new PrePayrollCutoff
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            BranchId = request.BranchId ?? context.BranchId,
            PayrollPeriodId = request.PayrollPeriodId.Value,
            CutoffCode = code,
            CutoffName = NormalizeText(request.CutoffName),
            StartDate = NormalizeUtc(request.StartDate, DateTime.UtcNow.Date),
            EndDate = NormalizeUtc(request.EndDate, DateTime.UtcNow.Date),
            EmployeesReviewed = request.EmployeesReviewed,
            IncidentsDetected = request.IncidentsDetected,
            WorkedDaysTotal = request.WorkedDaysTotal,
            OvertimeHoursTotal = request.OvertimeHoursTotal,
            Status = NormalizeLower(request.Status, "draft"),
            IsClosed = request.IsClosed,
            ClosedAt = request.ClosedAt,
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PrePayrollCutoffs.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePrePayrollCutoffAsync(Guid id, PrePayrollCutoffRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PrePayrollCutoffs.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el corte de prenómina." });

        var code = NormalizeUpper(request.CutoffCode, entity.CutoffCode);
        if (await db.PrePayrollCutoffs.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.PayrollPeriodId == (request.PayrollPeriodId ?? entity.PayrollPeriodId) && x.CutoffCode == code))
            return Results.BadRequest(new { message = "Ya existe otro corte con ese código para el periodo." });

        entity.BranchId = request.BranchId ?? entity.BranchId;
        entity.PayrollPeriodId = request.PayrollPeriodId ?? entity.PayrollPeriodId;
        entity.CutoffCode = code;
        entity.CutoffName = NormalizeText(request.CutoffName, entity.CutoffName);
        entity.StartDate = request.StartDate.HasValue ? NormalizeUtc(request.StartDate, entity.StartDate) : entity.StartDate;
        entity.EndDate = request.EndDate.HasValue ? NormalizeUtc(request.EndDate, entity.EndDate) : entity.EndDate;
        entity.EmployeesReviewed = request.EmployeesReviewed;
        entity.IncidentsDetected = request.IncidentsDetected;
        entity.WorkedDaysTotal = request.WorkedDaysTotal;
        entity.OvertimeHoursTotal = request.OvertimeHoursTotal;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.IsClosed = request.IsClosed;
        entity.ClosedAt = request.ClosedAt ?? entity.ClosedAt;
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePrePayrollCutoffAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PrePayrollCutoffs.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el corte de prenómina." });

        db.PrePayrollCutoffs.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class AttendanceDailySummaryRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public DateTime? WorkDate { get; set; }
    public DateTime? ScheduledEntryTime { get; set; }
    public DateTime? ScheduledExitTime { get; set; }
    public DateTime? FirstPunchDateTime { get; set; }
    public DateTime? LastPunchDateTime { get; set; }
    public decimal WorkedHours { get; set; }
    public int DelayMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal AbsenceUnits { get; set; }
    public string? DayType { get; set; }
    public string? Status { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AttendanceDailySummaryDto
{
    public Guid AttendanceDailySummaryId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public Guid? PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime? ScheduledEntryTime { get; set; }
    public DateTime? ScheduledExitTime { get; set; }
    public DateTime? FirstPunchDateTime { get; set; }
    public DateTime? LastPunchDateTime { get; set; }
    public decimal WorkedHours { get; set; }
    public int DelayMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal AbsenceUnits { get; set; }
    public string DayType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PrePayrollAdjustmentRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string? AdjustmentCode { get; set; }
    public string? AdjustmentName { get; set; }
    public string? AdjustmentType { get; set; }
    public string? CaptureSource { get; set; }
    public DateTime? ReferenceDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PrePayrollAdjustmentDto
{
    public Guid PrePayrollAdjustmentId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public Guid? PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public Guid? PayrollConceptId { get; set; }
    public string PayrollConceptName { get; set; } = string.Empty;
    public string AdjustmentCode { get; set; } = string.Empty;
    public string AdjustmentName { get; set; } = string.Empty;
    public string AdjustmentType { get; set; } = string.Empty;
    public string CaptureSource { get; set; } = string.Empty;
    public DateTime ReferenceDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PrePayrollCutoffRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public string? CutoffCode { get; set; }
    public string? CutoffName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int EmployeesReviewed { get; set; }
    public int IncidentsDetected { get; set; }
    public decimal WorkedDaysTotal { get; set; }
    public decimal OvertimeHoursTotal { get; set; }
    public string? Status { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PrePayrollCutoffDto
{
    public Guid PrePayrollCutoffId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public string CutoffCode { get; set; } = string.Empty;
    public string CutoffName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int EmployeesReviewed { get; set; }
    public int IncidentsDetected { get; set; }
    public decimal WorkedDaysTotal { get; set; }
    public decimal OvertimeHoursTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
