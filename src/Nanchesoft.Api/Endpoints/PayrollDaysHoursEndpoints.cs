using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollDaysHoursEndpoints
{
    public static IEndpointRouteBuilder MapPayrollDaysHoursEndpoints(this IEndpointRouteBuilder app)
    {
        var mnemonics = app.MapGroup("/api/payroll/day-mnemonics").WithTags("PayrollDayMnemonics");
        mnemonics.MapGet("/", ListMnemonicsAsync);
        mnemonics.MapPost("/", CreateMnemonicAsync);
        mnemonics.MapPut("/{id:guid}", UpdateMnemonicAsync);
        mnemonics.MapDelete("/{id:guid}", DeleteMnemonicAsync);

        var grid = app.MapGroup("/api/payroll/periods").WithTags("PayrollDaysHoursGrid");
        grid.MapGet("/{periodId:guid}/days-hours-grid", GetGridAsync);
        grid.MapPost("/{periodId:guid}/days-hours-save", SaveCellAsync);
        grid.MapDelete("/{periodId:guid}/days-hours-entry/{entryId:guid}", DeleteEntryAsync);
        grid.MapPost("/{periodId:guid}/days-hours-consolidate", ConsolidateAsync);

        return app;
    }

    private static string Trim(string? value, int max = 0, string fallback = "")
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (max > 0 && text.Length > max) text = text.Substring(0, max);
        return text;
    }

    private static string Upper(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string Lower(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static DateTime ToUtcDate(DateTime value)
    {
        var date = value.Date;
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveContextAsync(HttpContext httpContext, NanchesoftDbContext db)
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
            if (comp is not null) return (comp.TenantId, comp.Id);
        }

        var fallback = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return fallback is null ? (null, null) : (fallback.TenantId, fallback.Id);
    }

    private static async Task<IResult> ListMnemonicsAsync(NanchesoftDbContext db)
    {
        var rows = await db.PayrollDayMnemonics.AsNoTracking()
            .Include(x => x.PayrollConcept)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .Select(x => new PayrollDayMnemonicDto
            {
                PayrollDayMnemonicId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                PayrollConceptId = x.PayrollConceptId,
                PayrollConceptCode = x.PayrollConcept != null ? x.PayrollConcept.Code : string.Empty,
                Code = x.Code,
                Name = x.Name,
                Kind = x.Kind,
                UnitType = x.UnitType,
                DefaultUnits = x.DefaultUnits,
                Multiplier = x.Multiplier,
                ColorCode = x.ColorCode,
                ShortLabel = x.ShortLabel,
                AffectsAttendance = x.AffectsAttendance,
                AffectsPayroll = x.AffectsPayroll,
                SortOrder = x.SortOrder,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateMnemonicAsync(HttpContext httpContext, PayrollDayMnemonicRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveContextAsync(httpContext, db);
        if (!context.TenantId.HasValue || !context.CompanyId.HasValue)
            return Results.BadRequest(new { message = "No se pudo resolver empresa." });

        var code = Upper(request.Code, string.Empty);
        if (string.IsNullOrWhiteSpace(code)) return Results.BadRequest(new { message = "El código es obligatorio." });
        if (await db.PayrollDayMnemonics.AnyAsync(x => x.CompanyId == context.CompanyId.Value && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe una clave con ese código." });

        var entity = new PayrollDayMnemonic
        {
            TenantId = context.TenantId.Value,
            CompanyId = context.CompanyId.Value,
            PayrollConceptId = request.PayrollConceptId,
            Code = code,
            Name = Trim(request.Name, 120, code),
            Kind = Lower(request.Kind, "worked"),
            UnitType = Lower(request.UnitType, "hours"),
            DefaultUnits = request.DefaultUnits <= 0m ? 1m : request.DefaultUnits,
            Multiplier = request.Multiplier <= 0m ? 1m : request.Multiplier,
            ColorCode = Trim(request.ColorCode, 16, "#0d6efd"),
            ShortLabel = Trim(request.ShortLabel, 12, code),
            AffectsAttendance = request.AffectsAttendance,
            AffectsPayroll = request.AffectsPayroll,
            SortOrder = request.SortOrder,
            Notes = Trim(request.Notes, 400),
            IsActive = request.IsActive,
            CreatedBy = "days-hours"
        };
        db.PayrollDayMnemonics.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateMnemonicAsync(Guid id, PayrollDayMnemonicRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollDayMnemonics.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "No se encontró la clave." });

        var code = Upper(request.Code, entity.Code);
        if (code != entity.Code && await db.PayrollDayMnemonics.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otra clave con ese código." });

        entity.PayrollConceptId = request.PayrollConceptId ?? entity.PayrollConceptId;
        entity.Code = code;
        entity.Name = Trim(request.Name, 120, entity.Name);
        entity.Kind = Lower(request.Kind, entity.Kind);
        entity.UnitType = Lower(request.UnitType, entity.UnitType);
        entity.DefaultUnits = request.DefaultUnits <= 0m ? entity.DefaultUnits : request.DefaultUnits;
        entity.Multiplier = request.Multiplier <= 0m ? entity.Multiplier : request.Multiplier;
        entity.ColorCode = Trim(request.ColorCode, 16, entity.ColorCode);
        entity.ShortLabel = Trim(request.ShortLabel, 12, entity.ShortLabel);
        entity.AffectsAttendance = request.AffectsAttendance;
        entity.AffectsPayroll = request.AffectsPayroll;
        entity.SortOrder = request.SortOrder;
        entity.Notes = Trim(request.Notes, 400, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "days-hours";
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteMnemonicAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollDayMnemonics.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Results.NotFound(new { message = "No se encontró la clave." });

        var hasEntries = await db.PayrollDailyEntries.AnyAsync(x => x.PayrollDayMnemonicId == id);
        if (hasEntries)
            return Results.BadRequest(new { message = "No se puede eliminar una clave con capturas. Desactívela." });

        db.PayrollDayMnemonics.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetGridAsync(Guid periodId, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null) return Results.NotFound(new { message = "No se encontró el periodo." });

        var mnemonics = await db.PayrollDayMnemonics.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .Select(x => new PayrollDayMnemonicGridDto
            {
                PayrollDayMnemonicId = x.Id,
                Code = x.Code,
                Name = x.Name,
                Kind = x.Kind,
                UnitType = x.UnitType,
                DefaultUnits = x.DefaultUnits,
                Multiplier = x.Multiplier,
                ColorCode = x.ColorCode,
                ShortLabel = x.ShortLabel,
                AffectsPayroll = x.AffectsPayroll
            })
            .ToListAsync();

        var employees = await db.Employees.AsNoTracking()
            .Include(x => x.Department)
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && x.Status == "active")
            .OrderBy(x => x.EmployeeNumber)
            .Select(x => new PayrollDailyGridEmployeeDto
            {
                EmployeeId = x.Id,
                EmployeeNumber = x.EmployeeNumber,
                EmployeeName = (x.FirstName + " " + x.LastName).Trim(),
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                Entries = new()
            })
            .ToListAsync();

        var employeeIds = employees.Select(x => x.EmployeeId).ToHashSet();

        var entries = await db.PayrollDailyEntries.AsNoTracking()
            .Where(x => x.PayrollPeriodId == periodId && x.IsActive)
            .Select(x => new PayrollDailyEntryDto
            {
                PayrollDailyEntryId = x.Id,
                EmployeeId = x.EmployeeId,
                PayrollDayMnemonicId = x.PayrollDayMnemonicId,
                WorkDate = x.WorkDate,
                Units = x.Units,
                Amount = x.Amount,
                Notes = x.Notes,
                Status = x.Status,
                ResultingAdjustmentId = x.ResultingAdjustmentId
            })
            .ToListAsync();

        var entriesByEmployee = entries
            .Where(x => employeeIds.Contains(x.EmployeeId))
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.WorkDate).ToList());

        foreach (var emp in employees)
        {
            if (entriesByEmployee.TryGetValue(emp.EmployeeId, out var list))
                emp.Entries = list;
        }

        var days = new List<DateTime>();
        var startUtc = ToUtcDate(period.StartDate);
        var endUtc = ToUtcDate(period.EndDate);
        for (var d = startUtc; d <= endUtc; d = d.AddDays(1))
            days.Add(d);

        return Results.Ok(new PayrollDailyGridDto
        {
            PayrollPeriodId = period.Id,
            PeriodName = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Days = days,
            Mnemonics = mnemonics,
            Employees = employees
        });
    }

    private static async Task<IResult> SaveCellAsync(Guid periodId, PayrollDailyEntrySaveRequest request, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null) return Results.NotFound(new { message = "No se encontró el periodo." });

        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == period.CompanyId);
        if (employee is null) return Results.BadRequest(new { message = "Colaborador no válido." });

        var mnemonic = await db.PayrollDayMnemonics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollDayMnemonicId && x.CompanyId == period.CompanyId);
        if (mnemonic is null) return Results.BadRequest(new { message = "Clave no válida." });

        var workDate = ToUtcDate(request.WorkDate);
        if (workDate < ToUtcDate(period.StartDate) || workDate > ToUtcDate(period.EndDate))
            return Results.BadRequest(new { message = "La fecha está fuera del periodo." });

        var existing = request.PayrollDailyEntryId.HasValue
            ? await db.PayrollDailyEntries.FirstOrDefaultAsync(x => x.Id == request.PayrollDailyEntryId.Value)
            : null;

        if (request.Units <= 0m)
        {
            if (existing is not null)
            {
                db.PayrollDailyEntries.Remove(existing);
                await db.SaveChangesAsync();
                return Results.Ok(new { success = true, deleted = true });
            }
            return Results.Ok(new { success = true, deleted = false });
        }

        if (existing is null)
        {
            existing = new PayrollDailyEntry
            {
                TenantId = period.TenantId,
                CompanyId = period.CompanyId,
                EmployeeId = employee.Id,
                PayrollPeriodId = period.Id,
                PayrollDayMnemonicId = mnemonic.Id,
                WorkDate = workDate,
                Units = Math.Round(request.Units, 4),
                Amount = Math.Round(request.Amount, 2),
                Notes = Trim(request.Notes, 400),
                Status = "captured",
                IsActive = true,
                CreatedBy = "days-hours"
            };
            db.PayrollDailyEntries.Add(existing);
        }
        else
        {
            existing.PayrollDayMnemonicId = mnemonic.Id;
            existing.WorkDate = workDate;
            existing.Units = Math.Round(request.Units, 4);
            existing.Amount = Math.Round(request.Amount, 2);
            existing.Notes = Trim(request.Notes, 400, existing.Notes);
            existing.Status = "captured";
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = "days-hours";
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = existing.Id });
    }

    private static async Task<IResult> DeleteEntryAsync(Guid periodId, Guid entryId, NanchesoftDbContext db)
    {
        var entry = await db.PayrollDailyEntries.FirstOrDefaultAsync(x => x.Id == entryId && x.PayrollPeriodId == periodId);
        if (entry is null) return Results.NotFound(new { message = "No se encontró la captura." });
        db.PayrollDailyEntries.Remove(entry);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static (decimal Taxable, decimal Exempt) SplitConceptAmount(PayrollConcept concept, decimal amount)
    {
        if (amount <= 0m) return (0m, 0m);
        var taxableType = (concept.TaxableType ?? "taxable").Trim().ToLowerInvariant();
        return taxableType switch
        {
            "exempt" => (0m, amount),
            "mixed" => (Math.Round(amount * (concept.TaxablePercent / 100m), 2), Math.Round(amount * (concept.ExemptPercent / 100m), 2)),
            _ => (amount, 0m)
        };
    }

    private static async Task<IResult> ConsolidateAsync(Guid periodId, NanchesoftDbContext db)
    {
        var period = await db.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == periodId);
        if (period is null) return Results.NotFound(new { message = "No se encontró el periodo." });

        var entries = await db.PayrollDailyEntries
            .Include(x => x.PayrollDayMnemonic).ThenInclude(m => m!.PayrollConcept)
            .Where(x => x.PayrollPeriodId == periodId && x.IsActive && x.Units > 0m)
            .ToListAsync();

        var employees = await db.Employees.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId)
            .ToDictionaryAsync(x => x.Id);

        var concepts = await db.PayrollConcepts.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId)
            .ToDictionaryAsync(x => x.Id);

        var existingAdjustments = await db.PrePayrollAdjustments
            .Where(x => x.CompanyId == period.CompanyId && x.PayrollPeriodId == periodId && x.CaptureSource == "days-hours")
            .ToListAsync();
        var existingMap = existingAdjustments
            .Where(x => x.PayrollConceptId.HasValue)
            .GroupBy(x => (x.EmployeeId, x.PayrollConceptId!.Value))
            .ToDictionary(g => g.Key, g => g.First());

        var consolidated = entries
            .Where(x => x.PayrollDayMnemonic is not null
                && x.PayrollDayMnemonic.AffectsPayroll
                && x.PayrollDayMnemonic.PayrollConceptId.HasValue)
            .GroupBy(x => new
            {
                x.EmployeeId,
                ConceptId = x.PayrollDayMnemonic!.PayrollConceptId!.Value
            })
            .Select(g => new
            {
                g.Key.EmployeeId,
                g.Key.ConceptId,
                TotalUnits = g.Sum(x => x.Units),
                TotalAmount = g.Sum(x => x.Amount),
                MnemonicCode = g.First().PayrollDayMnemonic!.Code,
                MnemonicName = g.First().PayrollDayMnemonic!.Name,
                Multiplier = g.First().PayrollDayMnemonic!.Multiplier
            })
            .ToList();

        var generated = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var item in consolidated)
        {
            if (!employees.TryGetValue(item.EmployeeId, out var emp))
            {
                skipped++;
                continue;
            }
            if (!concepts.TryGetValue(item.ConceptId, out var concept))
            {
                skipped++;
                continue;
            }

            var baseSalary = emp.PeriodSalary > 0 ? emp.PeriodSalary : emp.DailySalary;
            decimal computedAmount = item.TotalAmount > 0m
                ? item.TotalAmount
                : item.MnemonicCode switch
                {
                    var code when code.StartsWith("HE") => Math.Round((baseSalary / 30m / 8m) * item.TotalUnits * item.Multiplier, 2),
                    "FINJ" => Math.Round((baseSalary / 30m) * item.TotalUnits * item.Multiplier, 2),
                    "VAC"  => Math.Round((baseSalary / 30m) * item.TotalUnits, 2),
                    _ => Math.Round(baseSalary / 30m * item.TotalUnits * item.Multiplier, 2)
                };

            var split = SplitConceptAmount(concept, computedAmount);
            var key = (item.EmployeeId, item.ConceptId);

            if (existingMap.TryGetValue(key, out var existing))
            {
                existing.AdjustmentCode = (concept.Code ?? string.Empty).ToUpperInvariant();
                existing.AdjustmentName = concept.Name ?? string.Empty;
                existing.AdjustmentType = (concept.ConceptType ?? "perception").ToLowerInvariant();
                existing.CaptureSource = "days-hours";
                existing.ReferenceDate = period.EndDate;
                existing.Quantity = item.TotalUnits;
                existing.Amount = computedAmount;
                existing.TaxableAmount = split.Taxable;
                existing.ExemptAmount = split.Exempt;
                existing.Status = "captured";
                existing.Notes = $"Consolidado: {item.MnemonicCode} · {item.MnemonicName}";
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = "days-hours";
                updated++;
            }
            else
            {
                db.PrePayrollAdjustments.Add(new PrePayrollAdjustment
                {
                    TenantId = period.TenantId,
                    CompanyId = period.CompanyId,
                    EmployeeId = item.EmployeeId,
                    PayrollPeriodId = period.Id,
                    PayrollConceptId = concept.Id,
                    AdjustmentCode = (concept.Code ?? string.Empty).ToUpperInvariant(),
                    AdjustmentName = concept.Name ?? string.Empty,
                    AdjustmentType = (concept.ConceptType ?? "perception").ToLowerInvariant(),
                    CaptureSource = "days-hours",
                    ReferenceDate = period.EndDate,
                    Quantity = item.TotalUnits,
                    Amount = computedAmount,
                    TaxableAmount = split.Taxable,
                    ExemptAmount = split.Exempt,
                    Status = "captured",
                    Notes = $"Consolidado: {item.MnemonicCode} · {item.MnemonicName}",
                    IsActive = true,
                    CreatedBy = "days-hours"
                });
                generated++;
            }
        }

        foreach (var entry in entries)
        {
            entry.Status = "consolidated";
            entry.UpdatedAt = DateTime.UtcNow;
            entry.UpdatedBy = "days-hours";
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            generated,
            updated,
            skipped,
            entriesProcessed = entries.Count
        });
    }
}

public sealed class PayrollDayMnemonicRequest
{
    public Guid? PayrollConceptId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Kind { get; set; }
    public string? UnitType { get; set; }
    public decimal DefaultUnits { get; set; } = 1m;
    public decimal Multiplier { get; set; } = 1m;
    public string? ColorCode { get; set; }
    public string? ShortLabel { get; set; }
    public bool AffectsAttendance { get; set; } = true;
    public bool AffectsPayroll { get; set; } = true;
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollDayMnemonicDto
{
    public Guid PayrollDayMnemonicId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string PayrollConceptCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public decimal DefaultUnits { get; set; }
    public decimal Multiplier { get; set; }
    public string ColorCode { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
    public bool AffectsAttendance { get; set; }
    public bool AffectsPayroll { get; set; }
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PayrollDayMnemonicGridDto
{
    public Guid PayrollDayMnemonicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public decimal DefaultUnits { get; set; }
    public decimal Multiplier { get; set; }
    public string ColorCode { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
    public bool AffectsPayroll { get; set; }
}

public sealed class PayrollDailyEntryDto
{
    public Guid PayrollDailyEntryId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid PayrollDayMnemonicId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal Units { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ResultingAdjustmentId { get; set; }
}

public sealed class PayrollDailyGridEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public List<PayrollDailyEntryDto> Entries { get; set; } = [];
}

public sealed class PayrollDailyGridDto
{
    public Guid PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DateTime> Days { get; set; } = [];
    public List<PayrollDayMnemonicGridDto> Mnemonics { get; set; } = [];
    public List<PayrollDailyGridEmployeeDto> Employees { get; set; } = [];
}

public sealed class PayrollDailyEntrySaveRequest
{
    public Guid? PayrollDailyEntryId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid PayrollDayMnemonicId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal Units { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
