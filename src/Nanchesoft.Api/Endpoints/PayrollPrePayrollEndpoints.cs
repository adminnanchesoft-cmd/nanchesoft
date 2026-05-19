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

        var periods = app.MapGroup("/api/payroll/periods").WithTags("PrePayrollWorksheet");
        periods.MapGet("/{periodId:guid}/prepayroll-matrix", GetPrePayrollMatrixAsync);
        periods.MapPost("/{periodId:guid}/prepayroll-matrix-save", SavePrePayrollMatrixAsync);
        periods.MapPost("/{periodId:guid}/prepayroll-matrix-import", ImportPrePayrollMatrixAsync).DisableAntiforgery();

        var cutoffs = app.MapGroup("/api/payroll/prepayroll-cutoffs").WithTags("PrePayrollCutoffs");
        cutoffs.MapGet("/", GetPrePayrollCutoffsAsync);
        cutoffs.MapPost("/", CreatePrePayrollCutoffAsync);
        cutoffs.MapPut("/{id:guid}", UpdatePrePayrollCutoffAsync);
        cutoffs.MapDelete("/{id:guid}", DeletePrePayrollCutoffAsync);

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId, Guid? BranchId)> ResolveDefaultContextAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var branchId = ApiTenantScope.ResolveBranchId(httpContext);

        if (companyId.HasValue)
        {
            if (!tenantId.HasValue)
                tenantId = await db.Companies.Where(x => x.Id == companyId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefaultAsync();
            if (!branchId.HasValue)
                branchId = await db.Branches.Where(x => x.CompanyId == companyId.Value).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            return (tenantId, companyId, branchId);
        }

        if (tenantId.HasValue)
        {
            var comp = await db.Companies.Where(x => x.TenantId == tenantId.Value).OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
            if (comp is not null)
            {
                if (!branchId.HasValue)
                    branchId = await db.Branches.Where(x => x.CompanyId == comp.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
                return (tenantId, comp.Id, branchId);
            }
        }

        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        if (company is null) return (null, null, null);
        var fallbackBranch = await db.Branches.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        return (company.TenantId, company.Id, fallbackBranch);
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

    private static (decimal Taxable, decimal Exempt) SplitConceptAmount(PayrollConcept concept, decimal amount)
    {
        if (amount <= 0m) return (0m, 0m);
        var taxableType = NormalizeLower(concept.TaxableType, "taxable");
        return taxableType switch
        {
            "exempt" => (0m, amount),
            "mixed" => (Math.Round(amount * (concept.TaxablePercent / 100m), 2), Math.Round(amount * (concept.ExemptPercent / 100m), 2)),
            _ => (amount, 0m)
        };
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

    private static async Task<IResult> CreateAttendanceDailySummaryAsync(HttpContext httpContext, AttendanceDailySummaryRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
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

    private static async Task<IResult> CreatePrePayrollAdjustmentAsync(HttpContext httpContext, PrePayrollAdjustmentRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
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

    private static async Task<IResult> GetPrePayrollMatrixAsync(Guid periodId, string? conceptIds, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        var selectedConceptIds = (conceptIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var conceptsQuery = db.PayrollConcepts.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && x.ConceptType != "obligation");

        if (selectedConceptIds.Count > 0)
            conceptsQuery = conceptsQuery.Where(x => selectedConceptIds.Contains(x.Id));
        else
            conceptsQuery = conceptsQuery.Where(x => !x.IsAutomatic || x.Code == "BON" || x.Code == "DESCTO");

        var concepts = await conceptsQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new PrePayrollMatrixConceptDto
            {
                ConceptId = x.Id,
                Code = x.Code,
                Name = x.Name,
                ConceptType = x.ConceptType
            })
            .ToListAsync();

        if (concepts.Count == 0)
            concepts = await db.PayrollConcepts.AsNoTracking()
                .Where(x => x.CompanyId == period.CompanyId && x.IsActive && x.ConceptType != "obligation")
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .Take(8)
                .Select(x => new PrePayrollMatrixConceptDto
                {
                    ConceptId = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    ConceptType = x.ConceptType
                })
                .ToListAsync();

        var conceptIdSet = concepts.Select(x => x.ConceptId).ToHashSet();

        var adjustments = await db.PrePayrollAdjustments.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.PayrollPeriodId == periodId && x.PayrollConceptId.HasValue && conceptIdSet.Contains(x.PayrollConceptId.Value) && x.IsActive)
            .ToListAsync();

        var adjustmentsByEmployee = adjustments
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Where(x => x.PayrollConceptId.HasValue)
                    .ToDictionary(x => x.PayrollConceptId!.Value.ToString("D"), x => new PrePayrollMatrixCellDto
                    {
                        AdjustmentId = x.Id,
                        Quantity = x.Quantity,
                        Amount = x.Amount,
                        Notes = x.Notes
                    }));

        var employees = await db.Employees.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Position)
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && x.Status == "active")
            .OrderBy(x => x.EmployeeNumber)
            .Select(x => new
            {
                x.Id,
                x.EmployeeNumber,
                x.FirstName,
                x.LastName,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                PositionName = x.Position != null ? x.Position.Name : string.Empty
            })
            .ToListAsync();

        var rows = employees.Select(x => new PrePayrollMatrixEmployeeDto
        {
            EmployeeId = x.Id,
            EmployeeNumber = x.EmployeeNumber,
            EmployeeName = (x.FirstName + " " + x.LastName).Trim(),
            DepartmentName = x.DepartmentName,
            PositionName = x.PositionName,
            ConceptAmounts = adjustmentsByEmployee.TryGetValue(x.Id, out var cells) ? cells : []
        }).ToList();

        return Results.Ok(new PrePayrollMatrixDto
        {
            PayrollPeriodId = period.Id,
            PeriodName = period.Name,
            PeriodStart = period.StartDate,
            PeriodEnd = period.EndDate,
            Concepts = concepts,
            Employees = rows
        });
    }

    private static async Task<IResult> SavePrePayrollMatrixAsync(Guid periodId, PrePayrollMatrixSaveRequest request, NanchesoftDbContext db)
    {
        var result = await SavePrePayrollCellsAsync(periodId, request, db);
        return result;
    }

    private static async Task<IResult> ImportPrePayrollMatrixAsync(Guid periodId, IFormFile file, NanchesoftDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "El archivo está vacío." });

        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return Results.NotFound(new { message = "No se encontró el periodo." });

        var employees = await db.Employees.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive)
            .ToDictionaryAsync(x => NormalizeUpper(x.EmployeeNumber), x => x.Id);
        var concepts = await db.PayrollConcepts.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && x.ConceptType != "obligation")
            .ToDictionaryAsync(x => NormalizeUpper(x.Code), x => x.Id);

        using var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            return Results.BadRequest(new { message = "El archivo no tiene encabezados." });

        var delimiter = DetectDelimiter(headerLine);
        var headers = ParseDelimitedLine(headerLine, delimiter).Select(NormalizeHeader).ToList();
        var headerMap = headers.Select((x, i) => new { x, i }).GroupBy(x => x.x).ToDictionary(g => g.Key, g => g.First().i);

        if (!headerMap.ContainsKey("noempleado") || !headerMap.ContainsKey("concepto") || !headerMap.ContainsKey("importe"))
            return Results.BadRequest(new { message = "El CSV debe incluir NoEmpleado, Concepto e Importe." });

        var cells = new List<PrePayrollMatrixSaveCell>();
        var errors = new List<string>();
        var skipped = 0;
        string? line;
        var rowNumber = 1;

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                skipped++;
                continue;
            }

            var row = ParseDelimitedLine(line, delimiter);
            var employeeNumber = NormalizeUpper(GetField(row, headerMap, "noempleado"));
            var conceptCode = NormalizeUpper(GetField(row, headerMap, "concepto"));
            var amountText = GetField(row, headerMap, "importe");
            var quantityText = GetField(row, headerMap, "cantidad");
            var notes = GetField(row, headerMap, "notas");

            if (string.IsNullOrWhiteSpace(employeeNumber) || string.IsNullOrWhiteSpace(conceptCode))
            {
                skipped++;
                continue;
            }

            if (!employees.TryGetValue(employeeNumber, out var employeeId))
            {
                errors.Add($"Fila {rowNumber}: colaborador '{employeeNumber}' no encontrado.");
                continue;
            }

            if (!concepts.TryGetValue(conceptCode, out var conceptId))
            {
                errors.Add($"Fila {rowNumber}: concepto '{conceptCode}' no encontrado.");
                continue;
            }

            if (!TryParseDecimal(amountText, out var amount))
            {
                errors.Add($"Fila {rowNumber}: importe inválido '{amountText}'.");
                continue;
            }

            var quantity = TryParseDecimal(quantityText, out var parsedQuantity) && parsedQuantity > 0m ? parsedQuantity : 1m;
            cells.Add(new PrePayrollMatrixSaveCell
            {
                EmployeeId = employeeId,
                PayrollConceptId = conceptId,
                Quantity = quantity,
                Amount = Math.Max(0m, amount),
                Notes = notes
            });
        }

        if (cells.Count == 0)
            return Results.BadRequest(new { message = "No se encontraron movimientos válidos para importar.", errors });

        var saveResult = await SavePrePayrollCellsCoreAsync(periodId, new PrePayrollMatrixSaveRequest { Cells = cells }, db);
        return Results.Ok(new
        {
            success = true,
            imported = cells.Count,
            skipped,
            errors,
            saveResult.saved,
            saveResult.deleted,
            saveSkipped = saveResult.skipped
        });
    }

    private static async Task<IResult> SavePrePayrollCellsAsync(Guid periodId, PrePayrollMatrixSaveRequest request, NanchesoftDbContext db)
    {
        var result = await SavePrePayrollCellsCoreAsync(periodId, request, db);
        if (result.notFound)
            return Results.NotFound(new { message = "No se encontró el periodo." });
        if (result.empty)
            return Results.BadRequest(new { message = "No hay movimientos para guardar." });
        return Results.Ok(new { success = true, result.saved, result.deleted, result.skipped });
    }

    private static async Task<(bool notFound, bool empty, int saved, int deleted, int skipped)> SavePrePayrollCellsCoreAsync(Guid periodId, PrePayrollMatrixSaveRequest request, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null)
            return (true, false, 0, 0, 0);

        if (request.Cells.Count == 0)
            return (false, true, 0, 0, 0);

        var employeeIds = request.Cells.Select(x => x.EmployeeId).Distinct().ToList();
        var conceptIds = request.Cells.Select(x => x.PayrollConceptId).Distinct().ToList();

        var validEmployees = await db.Employees
            .Where(x => x.CompanyId == period.CompanyId && employeeIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToHashSetAsync();

        var concepts = await db.PayrollConcepts
            .Where(x => x.CompanyId == period.CompanyId && conceptIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id);

        var existing = await db.PrePayrollAdjustments
            .Where(x => x.CompanyId == period.CompanyId && x.PayrollPeriodId == periodId && employeeIds.Contains(x.EmployeeId) && x.PayrollConceptId.HasValue && conceptIds.Contains(x.PayrollConceptId.Value))
            .ToListAsync();
        var existingMap = existing.ToDictionary(x => (x.EmployeeId, x.PayrollConceptId!.Value));

        var saved = 0;
        var deleted = 0;
        var skipped = 0;

        foreach (var cell in request.Cells)
        {
            if (!validEmployees.Contains(cell.EmployeeId) || !concepts.TryGetValue(cell.PayrollConceptId, out var concept))
            {
                skipped++;
                continue;
            }

            var key = (cell.EmployeeId, cell.PayrollConceptId);
            var amount = Math.Round(cell.Amount, 2);
            var quantity = cell.Quantity <= 0m ? 1m : cell.Quantity;

            if (amount <= 0m)
            {
                if (existingMap.TryGetValue(key, out var rowToDelete))
                {
                    db.PrePayrollAdjustments.Remove(rowToDelete);
                    deleted++;
                }
                continue;
            }

            var split = SplitConceptAmount(concept, amount);
            if (existingMap.TryGetValue(key, out var row))
            {
                row.AdjustmentCode = NormalizeUpper(concept.Code, row.AdjustmentCode);
                row.AdjustmentName = NormalizeText(concept.Name, row.AdjustmentName);
                row.AdjustmentType = NormalizeLower(concept.ConceptType, row.AdjustmentType);
                row.CaptureSource = "prepayroll-matrix";
                row.ReferenceDate = period.EndDate;
                row.Quantity = quantity;
                row.Amount = amount;
                row.TaxableAmount = split.Taxable;
                row.ExemptAmount = split.Exempt;
                row.Status = "captured";
                row.Notes = NormalizeText(cell.Notes, row.Notes);
                row.IsActive = true;
                row.UpdatedAt = DateTime.UtcNow;
                row.UpdatedBy = "prepayroll-matrix";
            }
            else
            {
                db.PrePayrollAdjustments.Add(new PrePayrollAdjustment
                {
                    TenantId = period.TenantId,
                    CompanyId = period.CompanyId,
                    EmployeeId = cell.EmployeeId,
                    PayrollPeriodId = period.Id,
                    PayrollConceptId = concept.Id,
                    AdjustmentCode = NormalizeUpper(concept.Code),
                    AdjustmentName = NormalizeText(concept.Name),
                    AdjustmentType = NormalizeLower(concept.ConceptType, "perception"),
                    CaptureSource = "prepayroll-matrix",
                    ReferenceDate = period.EndDate,
                    Quantity = quantity,
                    Amount = amount,
                    TaxableAmount = split.Taxable,
                    ExemptAmount = split.Exempt,
                    Status = "captured",
                    Notes = NormalizeText(cell.Notes),
                    IsActive = true,
                    CreatedBy = "prepayroll-matrix"
                });
            }
            saved++;
        }

        await db.SaveChangesAsync();
        return (false, false, saved, deleted, skipped);
    }

    private static char DetectDelimiter(string line)
    {
        var semis = line.Count(x => x == ';');
        var tabs = line.Count(x => x == '\t');
        var commas = line.Count(x => x == ',');
        if (tabs > 0 && tabs >= semis && tabs >= commas) return '\t';
        if (semis > commas) return ';';
        return ',';
    }

    private static List<string> ParseDelimitedLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == delimiter)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }

    private static string NormalizeHeader(string value)
    {
        var text = NormalizeLower(value)
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ñ", "n");
        var key = new string(text.Where(char.IsLetterOrDigit).ToArray());
        return key switch
        {
            "empleado" or "numeroempleado" or "claveempleado" => "noempleado",
            "conceptocode" or "codigoconcepto" or "claveconcepto" => "concepto",
            "monto" or "amount" => "importe",
            "qty" or "quantity" => "cantidad",
            "nota" or "notes" => "notas",
            _ => key
        };
    }

    private static string GetField(List<string> row, Dictionary<string, int> headerMap, string key)
        => headerMap.TryGetValue(key, out var index) && index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;

    private static bool TryParseDecimal(string value, out decimal amount)
        => decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amount)
            || decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("es-MX"), out amount);

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

    private static async Task<IResult> CreatePrePayrollCutoffAsync(HttpContext httpContext, PrePayrollCutoffRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
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

public sealed class PrePayrollMatrixDto
{
    public Guid PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<PrePayrollMatrixConceptDto> Concepts { get; set; } = [];
    public List<PrePayrollMatrixEmployeeDto> Employees { get; set; } = [];
}

public sealed class PrePayrollMatrixConceptDto
{
    public Guid ConceptId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty;
}

public sealed class PrePayrollMatrixEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public Dictionary<string, PrePayrollMatrixCellDto> ConceptAmounts { get; set; } = [];
}

public sealed class PrePayrollMatrixCellDto
{
    public Guid AdjustmentId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class PrePayrollMatrixSaveRequest
{
    public List<PrePayrollMatrixSaveCell> Cells { get; set; } = [];
}

public sealed class PrePayrollMatrixSaveCell
{
    public Guid EmployeeId { get; set; }
    public Guid PayrollConceptId { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
