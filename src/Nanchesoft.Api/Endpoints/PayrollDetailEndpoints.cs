using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PayrollDetailEndpoints
{
    public static IEndpointRouteBuilder MapPayrollDetailEndpoints(this IEndpointRouteBuilder app)
    {
        var details = app.MapGroup("/api/payroll/run-line-details").WithTags("PayrollRunLineDetails");
        details.MapGet("/", GetPayrollRunLineDetailsAsync);
        details.MapPost("/", CreatePayrollRunLineDetailAsync);
        details.MapPut("/{id:guid}", UpdatePayrollRunLineDetailAsync);
        details.MapDelete("/{id:guid}", DeletePayrollRunLineDetailAsync);

        var runs = app.MapGroup("/api/payroll/runs").WithTags("PayrollRunBreakdown");
        runs.MapPost("/{runId:guid}/generate-details", GeneratePayrollRunBreakdownAsync);
        runs.MapGet("/{runId:guid}/breakdown", GetPayrollRunBreakdownAsync);

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

    private static string NormalizeUpper(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string NormalizeLower(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static bool IsPayrollRunEditable(string? status)
    {
        var normalized = NormalizeLower(status, "draft");
        return normalized is "draft" or "borrador" or "open" or "abierto" or "captura" or "pending" or "pendiente";
    }

    private static async Task<IResult?> ValidateEditablePayrollRunAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró la corrida de nómina." });
        if (!IsPayrollRunEditable(run.Status))
            return Results.BadRequest(new { message = "La corrida ya fue calculada/autorizada/cerrada. No se pueden modificar sus conceptos." });
        return null;
    }

    private static (decimal Taxable, decimal Exempt) SplitTaxableAmount(string taxableType, decimal amount)
    {
        var normalized = NormalizeLower(taxableType, "taxable");
        if (amount <= 0m)
            return (0m, 0m);

        return normalized switch
        {
            "exempt" => (0m, amount),
            "mixed" => (Math.Round(amount * 0.60m, 2), Math.Round(amount * 0.40m, 2)),
            _ => (amount, 0m)
        };
    }

    private static async Task<IResult> GetPayrollRunLineDetailsAsync(NanchesoftDbContext db)
    {
        var rows = await db.PayrollRunLineDetails.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.PayrollRun)
            .Include(x => x.Employee)
            .Include(x => x.PayrollConcept)
            .OrderBy(x => x.PayrollRun!.Folio)
            .ThenBy(x => x.Employee!.LastName)
            .ThenBy(x => x.SortOrder)
            .Select(x => new PayrollRunLineDetailDto
            {
                PayrollRunLineDetailId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                PayrollRunId = x.PayrollRunId,
                PayrollRunFolio = x.PayrollRun != null ? x.PayrollRun.Folio : string.Empty,
                PayrollRunLineId = x.PayrollRunLineId,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? (x.Employee.FirstName + " " + x.Employee.LastName).Trim() : string.Empty,
                PayrollConceptId = x.PayrollConceptId,
                PayrollConceptName = x.PayrollConcept != null ? x.PayrollConcept.Name : x.ConceptName,
                ConceptCode = x.ConceptCode,
                ConceptName = x.ConceptName,
                ConceptType = x.ConceptType,
                SatCode = x.SatCode,
                TaxableType = x.TaxableType,
                Quantity = x.Quantity,
                Amount = x.Amount,
                TaxableAmount = x.TaxableAmount,
                ExemptAmount = x.ExemptAmount,
                SortOrder = x.SortOrder,
                IsGenerated = x.IsGenerated,
                Status = x.Status,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePayrollRunLineDetailAsync(HttpContext httpContext, PayrollRunLineDetailRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveDefaultContextAsync(httpContext, db);
        var tenantId = request.TenantId ?? context.TenantId;
        var companyId = request.CompanyId ?? context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el detalle de nómina." });

        if (!request.PayrollRunId.HasValue || !request.PayrollRunLineId.HasValue || !request.EmployeeId.HasValue || !request.PayrollConceptId.HasValue)
            return Results.BadRequest(new { message = "Proceso, línea, colaborador y concepto son obligatorios." });

        var locked = await ValidateEditablePayrollRunAsync(request.PayrollRunId.Value, db);
        if (locked is not null) return locked;

        var concept = await db.PayrollConcepts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollConceptId.Value);
        if (concept is null)
            return Results.BadRequest(new { message = "No se encontró el concepto de nómina enviado." });

        var amount = request.Amount;
        var taxableAmount = request.TaxableAmount;
        var exemptAmount = request.ExemptAmount;
        if (taxableAmount == 0m && exemptAmount == 0m && amount > 0m && NormalizeLower(request.ConceptType, concept.ConceptType) != "deduction")
        {
            var split = SplitTaxableAmount(request.TaxableType ?? concept.TaxableType, amount);
            taxableAmount = split.Taxable;
            exemptAmount = split.Exempt;
        }

        var entity = new PayrollRunLineDetail
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            PayrollRunId = request.PayrollRunId.Value,
            PayrollRunLineId = request.PayrollRunLineId.Value,
            EmployeeId = request.EmployeeId.Value,
            PayrollConceptId = request.PayrollConceptId.Value,
            ConceptCode = NormalizeUpper(request.ConceptCode, concept.Code),
            ConceptName = NormalizeText(request.ConceptName, concept.Name),
            ConceptType = NormalizeLower(request.ConceptType, concept.ConceptType),
            SatCode = NormalizeText(request.SatCode, concept.SatCode),
            TaxableType = NormalizeLower(request.TaxableType, concept.TaxableType),
            Quantity = request.Quantity <= 0m ? 1m : request.Quantity,
            Amount = amount,
            TaxableAmount = taxableAmount,
            ExemptAmount = exemptAmount,
            SortOrder = request.SortOrder,
            IsGenerated = request.IsGenerated,
            Status = NormalizeLower(request.Status, "active"),
            Notes = NormalizeText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.PayrollRunLineDetails.Add(entity);
        await db.SaveChangesAsync();
        await RecalculatePayrollRunAsync(entity.PayrollRunId, db);
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePayrollRunLineDetailAsync(Guid id, PayrollRunLineDetailRequest request, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRunLineDetails.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el detalle de nómina." });

        var locked = await ValidateEditablePayrollRunAsync(entity.PayrollRunId, db);
        if (locked is not null) return locked;

        var conceptId = request.PayrollConceptId ?? entity.PayrollConceptId;
        var concept = await db.PayrollConcepts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == conceptId);

        entity.PayrollRunId = request.PayrollRunId ?? entity.PayrollRunId;
        entity.PayrollRunLineId = request.PayrollRunLineId ?? entity.PayrollRunLineId;
        entity.EmployeeId = request.EmployeeId ?? entity.EmployeeId;
        entity.PayrollConceptId = conceptId;
        entity.ConceptCode = NormalizeUpper(request.ConceptCode, concept?.Code ?? entity.ConceptCode);
        entity.ConceptName = NormalizeText(request.ConceptName, concept?.Name ?? entity.ConceptName);
        entity.ConceptType = NormalizeLower(request.ConceptType, concept?.ConceptType ?? entity.ConceptType);
        entity.SatCode = NormalizeText(request.SatCode, concept?.SatCode ?? entity.SatCode);
        entity.TaxableType = NormalizeLower(request.TaxableType, concept?.TaxableType ?? entity.TaxableType);
        entity.Quantity = request.Quantity <= 0m ? entity.Quantity : request.Quantity;
        entity.Amount = request.Amount;

        var taxableAmount = request.TaxableAmount;
        var exemptAmount = request.ExemptAmount;
        if (taxableAmount == 0m && exemptAmount == 0m && entity.Amount > 0m && entity.ConceptType != "deduction")
        {
            var split = SplitTaxableAmount(entity.TaxableType, entity.Amount);
            taxableAmount = split.Taxable;
            exemptAmount = split.Exempt;
        }

        entity.TaxableAmount = taxableAmount;
        entity.ExemptAmount = exemptAmount;
        entity.SortOrder = request.SortOrder;
        entity.IsGenerated = request.IsGenerated;
        entity.Status = NormalizeLower(request.Status, entity.Status);
        entity.Notes = NormalizeText(request.Notes, entity.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        await RecalculatePayrollRunAsync(entity.PayrollRunId, db);
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePayrollRunLineDetailAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.PayrollRunLineDetails.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el detalle de nómina." });

        var locked = await ValidateEditablePayrollRunAsync(entity.PayrollRunId, db);
        if (locked is not null) return locked;

        var runId = entity.PayrollRunId;
        db.PayrollRunLineDetails.Remove(entity);
        await db.SaveChangesAsync();
        await RecalculatePayrollRunAsync(runId, db);
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GeneratePayrollRunBreakdownAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return Results.NotFound(new { message = "No se encontró la corrida de nómina." });
        if (!IsPayrollRunEditable(run.Status))
            return Results.BadRequest(new { message = "La corrida ya fue calculada/autorizada/cerrada. No se pueden regenerar sus conceptos." });

        var lines = await db.PayrollRunLines.Where(x => x.PayrollRunId == runId).OrderBy(x => x.CreatedAt).ToListAsync();
        if (lines.Count == 0)
            return Results.BadRequest(new { message = "La corrida no tiene colaboradores calculados." });

        var concepts = await db.PayrollConcepts.Where(x => x.CompanyId == run.CompanyId && x.IsActive).ToListAsync();
        var perceptionConcept = concepts.FirstOrDefault(x => x.Code == "SAL") ?? concepts.FirstOrDefault(x => x.ConceptType == "perception");
        var bonusConcept = concepts.FirstOrDefault(x => x.Code == "BON") ?? concepts.FirstOrDefault(x => x.ConceptType == "perception" && x.Code != (perceptionConcept?.Code ?? string.Empty));
        var deductionConcept = concepts.FirstOrDefault(x => x.Code == "ISR") ?? concepts.FirstOrDefault(x => x.ConceptType == "deduction");

        if (perceptionConcept is null || deductionConcept is null)
            return Results.BadRequest(new { message = "No existen conceptos suficientes para generar el desglose (SAL/ISR)." });

        var currentGenerated = await db.PayrollRunLineDetails.Where(x => x.PayrollRunId == runId && x.IsGenerated).ToListAsync();
        if (currentGenerated.Count > 0)
            db.PayrollRunLineDetails.RemoveRange(currentGenerated);

        var created = 0;

        foreach (var line in lines)
        {
            var sortOrder = 10;
            var baseAmount = Math.Max(0m, line.GrossAmount - line.IncidentsAmount);
            if (baseAmount > 0m)
            {
                var split = SplitTaxableAmount(perceptionConcept.TaxableType, baseAmount);
                db.PayrollRunLineDetails.Add(new PayrollRunLineDetail
                {
                    TenantId = line.TenantId,
                    CompanyId = line.CompanyId,
                    PayrollRunId = line.PayrollRunId,
                    PayrollRunLineId = line.Id,
                    EmployeeId = line.EmployeeId,
                    PayrollConceptId = perceptionConcept.Id,
                    ConceptCode = perceptionConcept.Code,
                    ConceptName = perceptionConcept.Name,
                    ConceptType = NormalizeLower(perceptionConcept.ConceptType, "perception"),
                    SatCode = perceptionConcept.SatCode,
                    TaxableType = perceptionConcept.TaxableType,
                    Quantity = line.DaysPaid <= 0m ? 1m : line.DaysPaid,
                    Amount = baseAmount,
                    TaxableAmount = split.Taxable,
                    ExemptAmount = split.Exempt,
                    SortOrder = sortOrder,
                    IsGenerated = true,
                    Status = "applied",
                    Notes = "Generado automáticamente desde la corrida.",
                    CreatedBy = "web-api"
                });
                created++;
                sortOrder += 10;
            }

            if (line.IncidentsAmount > 0m && bonusConcept is not null)
            {
                var split = SplitTaxableAmount(bonusConcept.TaxableType, line.IncidentsAmount);
                db.PayrollRunLineDetails.Add(new PayrollRunLineDetail
                {
                    TenantId = line.TenantId,
                    CompanyId = line.CompanyId,
                    PayrollRunId = line.PayrollRunId,
                    PayrollRunLineId = line.Id,
                    EmployeeId = line.EmployeeId,
                    PayrollConceptId = bonusConcept.Id,
                    ConceptCode = bonusConcept.Code,
                    ConceptName = bonusConcept.Name,
                    ConceptType = NormalizeLower(bonusConcept.ConceptType, "perception"),
                    SatCode = bonusConcept.SatCode,
                    TaxableType = bonusConcept.TaxableType,
                    Quantity = 1m,
                    Amount = line.IncidentsAmount,
                    TaxableAmount = split.Taxable,
                    ExemptAmount = split.Exempt,
                    SortOrder = sortOrder,
                    IsGenerated = true,
                    Status = "applied",
                    Notes = "Generado a partir de incidencias del periodo.",
                    CreatedBy = "web-api"
                });
                created++;
            }

            if (line.DeductionsAmount > 0m)
            {
                db.PayrollRunLineDetails.Add(new PayrollRunLineDetail
                {
                    TenantId = line.TenantId,
                    CompanyId = line.CompanyId,
                    PayrollRunId = line.PayrollRunId,
                    PayrollRunLineId = line.Id,
                    EmployeeId = line.EmployeeId,
                    PayrollConceptId = deductionConcept.Id,
                    ConceptCode = deductionConcept.Code,
                    ConceptName = deductionConcept.Name,
                    ConceptType = NormalizeLower(deductionConcept.ConceptType, "deduction"),
                    SatCode = deductionConcept.SatCode,
                    TaxableType = deductionConcept.TaxableType,
                    Quantity = 1m,
                    Amount = line.DeductionsAmount,
                    TaxableAmount = 0m,
                    ExemptAmount = 0m,
                    SortOrder = 90,
                    IsGenerated = true,
                    Status = "applied",
                    Notes = "Generado automáticamente desde la corrida.",
                    CreatedBy = "web-api"
                });
                created++;
            }
        }

        await db.SaveChangesAsync();
        await RecalculatePayrollRunAsync(runId, db);
        return Results.Ok(new { success = true, created });
    }

    private static async Task<IResult> GetPayrollRunBreakdownAsync(Guid runId, NanchesoftDbContext db)
    {
        if (!await db.PayrollRuns.AnyAsync(x => x.Id == runId))
            return Results.NotFound(new { message = "No se encontró la corrida de nómina." });

        var rows = await db.PayrollRunLineDetails.AsNoTracking()
            .Where(x => x.PayrollRunId == runId)
            .OrderBy(x => x.EmployeeId)
            .ThenBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.PayrollRunLineId,
                x.EmployeeId,
                x.ConceptCode,
                x.ConceptName,
                x.ConceptType,
                x.Amount,
                x.TaxableAmount,
                x.ExemptAmount,
                x.Status,
                x.SortOrder
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task RecalculatePayrollRunAsync(Guid runId, NanchesoftDbContext db)
    {
        var run = await db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null)
            return;
        if (!IsPayrollRunEditable(run.Status))
            return;

        var lines = await db.PayrollRunLines.Where(x => x.PayrollRunId == runId).ToListAsync();
        if (lines.Count == 0)
            return;

        var detailGroups = await db.PayrollRunLineDetails.AsNoTracking()
            .Where(x => x.PayrollRunId == runId && x.IsActive)
            .GroupBy(x => x.PayrollRunLineId)
            .Select(g => new
            {
                PayrollRunLineId = g.Key,
                GrossAmount = g.Where(x => x.ConceptType != "deduction").Sum(x => x.Amount),
                DeductionsAmount = g.Where(x => x.ConceptType == "deduction").Sum(x => x.Amount),
                IncidentsAmount = g.Where(x => x.ConceptCode == "BON").Sum(x => x.Amount)
            })
            .ToListAsync();

        foreach (var line in lines)
        {
            var detail = detailGroups.FirstOrDefault(x => x.PayrollRunLineId == line.Id);
            if (detail is null)
                continue;

            line.GrossAmount = detail.GrossAmount;
            line.DeductionsAmount = detail.DeductionsAmount;
            line.NetAmount = detail.GrossAmount - detail.DeductionsAmount;
            line.IncidentsAmount = detail.IncidentsAmount;
            line.UpdatedAt = DateTime.UtcNow;
            line.UpdatedBy = "web-api";
        }

        run.EmployeeCount = lines.Count;
        run.GrossAmount = lines.Sum(x => x.GrossAmount);
        run.DeductionsAmount = lines.Sum(x => x.DeductionsAmount);
        run.NetAmount = lines.Sum(x => x.NetAmount);
        run.UpdatedAt = DateTime.UtcNow;
        run.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
    }
}

public class PayrollRunLineDetailRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollRunId { get; set; }
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string? ConceptCode { get; set; }
    public string? ConceptName { get; set; }
    public string? ConceptType { get; set; }
    public string? SatCode { get; set; }
    public string? TaxableType { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public int SortOrder { get; set; }
    public bool IsGenerated { get; set; } = true;
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollRunLineDetailDto : PayrollRunLineDetailRequest
{
    public Guid PayrollRunLineDetailId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string PayrollConceptName { get; set; } = string.Empty;
}
