using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollPrePayrollAdvancedEndpoints
{
    public static IEndpointRouteBuilder MapPayrollPrePayrollAdvancedEndpoints(this IEndpointRouteBuilder app)
    {
        var prefs = app.MapGroup("/api/payroll/prepayroll-column-preferences").WithTags("PrePayrollColumnPreferences");
        prefs.MapGet("/", GetPreferenceAsync);
        prefs.MapPut("/base", SaveBasePreferenceAsync);
        prefs.MapPut("/period/{periodId:guid}", SavePeriodPreferenceAsync);
        prefs.MapDelete("/period/{periodId:guid}", DeletePeriodPreferenceAsync);

        var periods = app.MapGroup("/api/payroll/periods").WithTags("PrePayrollWorksheetAdvanced");
        periods.MapGet("/{periodId:guid}/prepayroll-matrix-export", ExportPrePayrollMatrixAsync);

        return app;
    }

    private static string NormalizeUserKey(string? userKey, HttpContext httpContext)
    {
        if (!string.IsNullOrWhiteSpace(userKey))
            return userKey.Trim().ToLowerInvariant();

        var headerKey = httpContext.Request.Headers["X-User-Key"].ToString();
        if (!string.IsNullOrWhiteSpace(headerKey))
            return headerKey.Trim().ToLowerInvariant();

        var name = httpContext.User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim().ToLowerInvariant();

        return "default";
    }

    private static List<Guid> ParseGuidList(string raw)
        => (raw ?? string.Empty)
            .Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

    private static string SerializeGuidList(IEnumerable<Guid> ids)
        => string.Join(",", ids.Where(x => x != Guid.Empty).Distinct().Select(x => x.ToString("D")));

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
            if (comp is not null)
                return (comp.TenantId, comp.Id);
        }

        var fallback = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return fallback is null ? (null, null) : (fallback.TenantId, fallback.Id);
    }

    private static async Task<IResult> GetPreferenceAsync(HttpContext httpContext, string? userKey, Guid? periodId, NanchesoftDbContext db)
    {
        var context = await ResolveContextAsync(httpContext, db);
        if (!context.CompanyId.HasValue)
            return Results.Ok(new PrePayrollColumnPreferenceDto { Source = "default", ConceptIds = [] });

        var key = NormalizeUserKey(userKey, httpContext);
        PrePayrollColumnPreference? periodPref = null;
        if (periodId.HasValue && periodId.Value != Guid.Empty)
        {
            periodPref = await db.PrePayrollColumnPreferences.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == context.CompanyId.Value && x.UserKey == key && x.PayrollPeriodId == periodId.Value);
        }

        var basePref = await db.PrePayrollColumnPreferences.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == context.CompanyId.Value && x.UserKey == key && x.PayrollPeriodId == null);

        var selected = periodPref ?? basePref;
        return Results.Ok(new PrePayrollColumnPreferenceDto
        {
            Source = periodPref is not null ? "period" : (basePref is not null ? "base" : "default"),
            ConceptIds = selected is null ? [] : ParseGuidList(selected.ConceptIds),
            HasBase = basePref is not null,
            HasPeriodOverride = periodPref is not null
        });
    }

    private static async Task<IResult> SaveBasePreferenceAsync(HttpContext httpContext, PrePayrollColumnPreferenceRequest request, NanchesoftDbContext db)
        => await UpsertPreferenceAsync(httpContext, request, null, db);

    private static async Task<IResult> SavePeriodPreferenceAsync(HttpContext httpContext, Guid periodId, PrePayrollColumnPreferenceRequest request, NanchesoftDbContext db)
        => await UpsertPreferenceAsync(httpContext, request, periodId, db);

    private static async Task<IResult> UpsertPreferenceAsync(HttpContext httpContext, PrePayrollColumnPreferenceRequest request, Guid? periodId, NanchesoftDbContext db)
    {
        var context = await ResolveContextAsync(httpContext, db);
        if (!context.TenantId.HasValue || !context.CompanyId.HasValue)
            return Results.BadRequest(new { message = "No se pudo resolver la empresa." });

        var key = NormalizeUserKey(request.UserKey, httpContext);
        var conceptIds = (request.ConceptIds ?? []).Where(x => x != Guid.Empty).Distinct().ToList();
        var serialized = SerializeGuidList(conceptIds);

        var existing = await db.PrePayrollColumnPreferences
            .FirstOrDefaultAsync(x => x.CompanyId == context.CompanyId.Value && x.UserKey == key && x.PayrollPeriodId == periodId);

        if (existing is null)
        {
            db.PrePayrollColumnPreferences.Add(new PrePayrollColumnPreference
            {
                TenantId = context.TenantId.Value,
                CompanyId = context.CompanyId.Value,
                PayrollPeriodId = periodId,
                UserKey = key,
                ConceptIds = serialized,
                Notes = string.Empty,
                IsActive = true,
                CreatedBy = "prepayroll-prefs"
            });
        }
        else
        {
            existing.ConceptIds = serialized;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = "prepayroll-prefs";
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, scope = periodId.HasValue ? "period" : "base", conceptCount = conceptIds.Count });
    }

    private static async Task<IResult> DeletePeriodPreferenceAsync(HttpContext httpContext, Guid periodId, string? userKey, NanchesoftDbContext db)
    {
        var context = await ResolveContextAsync(httpContext, db);
        if (!context.CompanyId.HasValue)
            return Results.NotFound(new { message = "No se encontró empresa." });

        var key = NormalizeUserKey(userKey, httpContext);
        var entity = await db.PrePayrollColumnPreferences
            .FirstOrDefaultAsync(x => x.CompanyId == context.CompanyId.Value && x.UserKey == key && x.PayrollPeriodId == periodId);
        if (entity is null)
            return Results.Ok(new { success = true, removed = false });

        db.PrePayrollColumnPreferences.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, removed = true });
    }

    private static async Task<IResult> ExportPrePayrollMatrixAsync(Guid periodId, string? conceptIds, NanchesoftDbContext db)
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

        var concepts = await conceptsQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToListAsync();

        var conceptIdSet = concepts.Select(x => x.Id).ToHashSet();
        var conceptCodes = concepts.ToDictionary(x => x.Id, x => (x.Code, x.Name));

        var employees = await db.Employees.AsNoTracking()
            .Include(x => x.Department)
            .Where(x => x.CompanyId == period.CompanyId && x.IsActive && x.Status == "active")
            .OrderBy(x => x.EmployeeNumber)
            .Select(x => new
            {
                x.Id,
                x.EmployeeNumber,
                x.FirstName,
                x.LastName,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty
            })
            .ToListAsync();

        var employeeMap = employees.ToDictionary(x => x.Id);

        var adjustments = await db.PrePayrollAdjustments.AsNoTracking()
            .Where(x => x.CompanyId == period.CompanyId && x.PayrollPeriodId == periodId
                && x.PayrollConceptId.HasValue
                && (conceptIdSet.Count == 0 || conceptIdSet.Contains(x.PayrollConceptId!.Value))
                && x.IsActive)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("NoEmpleado,Nombre,Departamento,Concepto,ConceptoNombre,Importe,Cantidad,Notas");

        foreach (var adjustment in adjustments)
        {
            if (!adjustment.PayrollConceptId.HasValue) continue;
            if (!employeeMap.TryGetValue(adjustment.EmployeeId, out var emp)) continue;
            if (!conceptCodes.TryGetValue(adjustment.PayrollConceptId.Value, out var conceptInfo)) continue;

            sb.Append(CsvEscape(emp.EmployeeNumber)).Append(',');
            sb.Append(CsvEscape((emp.FirstName + " " + emp.LastName).Trim())).Append(',');
            sb.Append(CsvEscape(emp.DepartmentName)).Append(',');
            sb.Append(CsvEscape(conceptInfo.Code)).Append(',');
            sb.Append(CsvEscape(conceptInfo.Name)).Append(',');
            sb.Append(adjustment.Amount.ToString("F2", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(adjustment.Quantity.ToString("F4", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(CsvEscape(adjustment.Notes));
            sb.AppendLine();
        }

        var fileName = $"prenomina-{period.Code}-{DateTime.UtcNow:yyyyMMddHHmm}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return Results.File(bytes, "text/csv", fileName);
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}

public sealed class PrePayrollColumnPreferenceRequest
{
    public string? UserKey { get; set; }
    public List<Guid> ConceptIds { get; set; } = [];
}

public sealed class PrePayrollColumnPreferenceDto
{
    public string Source { get; set; } = "default";
    public List<Guid> ConceptIds { get; set; } = [];
    public bool HasBase { get; set; }
    public bool HasPeriodOverride { get; set; }
}
