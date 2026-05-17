using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class QualityControlEndpoints
{
    public static void MapQualityControlEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/production/quality-control").WithTags("QualityControl");

        // ─── List QC records ─────────────────────────────────────────────────
        g.MapGet("/", async (Guid? companyId, string? status, int? year, NanchesoftDbContext db) =>
        {
            var query = db.QualityControlRecords.AsNoTracking()
                .Include(x => x.Defects)
                .AsQueryable();

            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToLower());
            if (year.HasValue) query = query.Where(x => x.InspectionDate.Year == year.Value);

            var items = await query.OrderByDescending(x => x.InspectionDate)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Folio,
                    x.CompanyId,
                    x.ProductionOrderId,
                    x.InspectionDate,
                    x.InspectorName,
                    x.Status,
                    x.Result,
                    x.TotalUnitsInspected,
                    x.TotalUnitsApproved,
                    x.TotalUnitsRejected,
                    DefectCount = x.Defects.Count,
                    x.Notes
                })
                .ToListAsync();

            return Results.Ok(items);
        });

        // ─── Get QC record with defects ──────────────────────────────────────
        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var record = await db.QualityControlRecords.AsNoTracking()
                .Include(x => x.Defects)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record is null) return Results.NotFound(new { message = "Registro de control de calidad no encontrado." });

            return Results.Ok(new
            {
                record.Id,
                record.Folio,
                record.CompanyId,
                record.ProductionOrderId,
                record.InspectionDate,
                record.InspectorName,
                record.Status,
                record.Result,
                record.TotalUnitsInspected,
                record.TotalUnitsApproved,
                record.TotalUnitsRejected,
                record.Notes,
                record.ClosedAt,
                record.ClosedBy,
                Defects = record.Defects.OrderBy(d => d.Severity).Select(d => new
                {
                    d.Id,
                    d.DefectCode,
                    d.DefectDescription,
                    d.Severity,
                    d.QuantityAffected,
                    d.ResolutionNotes,
                    d.IsResolved
                }).ToList()
            });
        });

        // ─── Create QC record ────────────────────────────────────────────────
        g.MapPost("/", async (QualityControlCreateRequest request, NanchesoftDbContext db) =>
        {
            if (request.CompanyId == Guid.Empty)
                return Results.BadRequest(new { message = "CompanyId es obligatorio." });
            if (request.ProductionOrderId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductionOrderId es obligatorio." });
            if (string.IsNullOrWhiteSpace(request.InspectorName))
                return Results.BadRequest(new { message = "El nombre del inspector es obligatorio." });

            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId);
            if (company is null) return Results.BadRequest(new { message = "Empresa no encontrada." });

            var order = await db.ProductionOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ProductionOrderId);
            if (order is null) return Results.BadRequest(new { message = "Orden de producción no encontrada." });

            var folio = await GenerateFolioAsync(db, request.CompanyId, request.InspectionDate.Year);

            var record = new QualityControlRecord
            {
                TenantId = company.TenantId,
                CompanyId = request.CompanyId,
                ProductionOrderId = request.ProductionOrderId,
                Folio = folio,
                InspectionDate = request.InspectionDate,
                InspectorName = request.InspectorName.Trim(),
                Status = "pending",
                TotalUnitsInspected = request.TotalUnitsInspected,
                TotalUnitsApproved = request.TotalUnitsApproved,
                TotalUnitsRejected = request.TotalUnitsRejected,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedBy = request.UserId ?? "api"
            };

            db.QualityControlRecords.Add(record);
            await db.SaveChangesAsync();

            return Results.Created($"/api/production/quality-control/{record.Id}", new
            {
                qualityControlRecordId = record.Id,
                folio = record.Folio
            });
        });

        // ─── Approve QC record ───────────────────────────────────────────────
        g.MapPost("/{id:guid}/approve", async (Guid id, QualityControlActionRequest request, NanchesoftDbContext db) =>
        {
            var record = await db.QualityControlRecords.Include(x => x.Defects).FirstOrDefaultAsync(x => x.Id == id);
            if (record is null) return Results.NotFound(new { message = "Registro no encontrado." });
            if (record.Status != "pending" && record.Status != "on_hold")
                return Results.BadRequest(new { message = $"No se puede aprobar un registro en estado '{record.Status}'." });

            var unresolvedCritical = record.Defects.Any(d => d.Severity == "critical" && !d.IsResolved);
            if (unresolvedCritical)
                return Results.BadRequest(new { message = "Existen defectos críticos sin resolver. Resuélvelos antes de aprobar." });

            record.Status = "approved";
            record.Result = "approved";
            record.ClosedAt = DateTime.UtcNow;
            record.ClosedBy = request.UserId ?? "api";
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = request.UserId ?? "api";
            if (!string.IsNullOrWhiteSpace(request.Notes))
                record.Notes = record.Notes + "\n[Aprobación] " + request.Notes.Trim();

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Registro aprobado.", status = record.Status });
        });

        // ─── Reject QC record ────────────────────────────────────────────────
        g.MapPost("/{id:guid}/reject", async (Guid id, QualityControlActionRequest request, NanchesoftDbContext db) =>
        {
            var record = await db.QualityControlRecords.FirstOrDefaultAsync(x => x.Id == id);
            if (record is null) return Results.NotFound(new { message = "Registro no encontrado." });
            if (record.Status != "pending" && record.Status != "on_hold")
                return Results.BadRequest(new { message = $"No se puede rechazar un registro en estado '{record.Status}'." });

            record.Status = "rejected";
            record.Result = "rejected";
            record.ClosedAt = DateTime.UtcNow;
            record.ClosedBy = request.UserId ?? "api";
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = request.UserId ?? "api";
            if (!string.IsNullOrWhiteSpace(request.Notes))
                record.Notes = record.Notes + "\n[Rechazo] " + request.Notes.Trim();

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Registro rechazado.", status = record.Status });
        });

        // ─── Put on hold ─────────────────────────────────────────────────────
        g.MapPost("/{id:guid}/hold", async (Guid id, QualityControlActionRequest request, NanchesoftDbContext db) =>
        {
            var record = await db.QualityControlRecords.FirstOrDefaultAsync(x => x.Id == id);
            if (record is null) return Results.NotFound(new { message = "Registro no encontrado." });
            if (record.Status != "pending")
                return Results.BadRequest(new { message = "Solo se puede poner en espera un registro pendiente." });

            record.Status = "on_hold";
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Registro en espera.", status = record.Status });
        });

        // ─── Add defect ──────────────────────────────────────────────────────
        g.MapPost("/{id:guid}/defects", async (Guid id, QualityDefectRequest request, NanchesoftDbContext db) =>
        {
            var record = await db.QualityControlRecords.Include(x => x.Defects).FirstOrDefaultAsync(x => x.Id == id);
            if (record is null) return Results.NotFound(new { message = "Registro no encontrado." });
            if (record.Status == "approved" || record.Status == "rejected")
                return Results.BadRequest(new { message = "No se pueden agregar defectos a un registro cerrado." });

            if (string.IsNullOrWhiteSpace(request.DefectCode))
                return Results.BadRequest(new { message = "El código de defecto es obligatorio." });

            var defect = new QualityDefect
            {
                QualityControlRecordId = id,
                DefectCode = request.DefectCode.Trim().ToUpper(),
                DefectDescription = request.DefectDescription?.Trim() ?? string.Empty,
                Severity = request.Severity?.ToLower() ?? "low",
                QuantityAffected = request.QuantityAffected,
                CreatedBy = request.UserId ?? "api"
            };

            record.Defects.Add(defect);
            record.TotalUnitsRejected = Math.Max(record.TotalUnitsRejected, record.Defects.Sum(d => d.QuantityAffected));
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Created($"/api/production/quality-control/{id}/defects/{defect.Id}", new { defectId = defect.Id });
        });

        // ─── Resolve defect ──────────────────────────────────────────────────
        g.MapPost("/{id:guid}/defects/{defectId:guid}/resolve", async (Guid id, Guid defectId, QualityControlActionRequest request, NanchesoftDbContext db) =>
        {
            var defect = await db.QualityDefects.FirstOrDefaultAsync(x => x.Id == defectId && x.QualityControlRecordId == id);
            if (defect is null) return Results.NotFound(new { message = "Defecto no encontrado." });

            defect.IsResolved = true;
            defect.ResolutionNotes = request.Notes?.Trim() ?? string.Empty;
            defect.UpdatedAt = DateTime.UtcNow;
            defect.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Defecto resuelto." });
        });

        // ─── Dashboard ───────────────────────────────────────────────────────
        g.MapGet("/dashboard", async (Guid? companyId, int? year, NanchesoftDbContext db) =>
        {
            var y = year ?? DateTime.UtcNow.Year;
            var query = db.QualityControlRecords.AsNoTracking().Where(x => x.InspectionDate.Year == y);
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);

            var all = await query.Select(x => new
            {
                x.Status,
                x.TotalUnitsInspected,
                x.TotalUnitsApproved,
                x.TotalUnitsRejected
            }).ToListAsync();

            return Results.Ok(new
            {
                Year = y,
                Total = all.Count,
                Approved = all.Count(x => x.Status == "approved"),
                Rejected = all.Count(x => x.Status == "rejected"),
                Pending = all.Count(x => x.Status == "pending"),
                OnHold = all.Count(x => x.Status == "on_hold"),
                TotalUnitsInspected = all.Sum(x => x.TotalUnitsInspected),
                TotalUnitsApproved = all.Sum(x => x.TotalUnitsApproved),
                TotalUnitsRejected = all.Sum(x => x.TotalUnitsRejected),
                ApprovalRate = all.Count > 0
                    ? Math.Round((double)all.Count(x => x.Status == "approved") / all.Count * 100, 1)
                    : 0.0
            });
        });
    }

    private static async Task<string> GenerateFolioAsync(NanchesoftDbContext db, Guid companyId, int year)
    {
        var count = await db.QualityControlRecords.CountAsync(x => x.CompanyId == companyId && x.InspectionDate.Year == year);
        return $"QC{year}-{(count + 1):D4}";
    }
}

public sealed class QualityControlCreateRequest
{
    public Guid CompanyId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public DateOnly InspectionDate { get; set; }
    public string InspectorName { get; set; } = string.Empty;
    public int TotalUnitsInspected { get; set; }
    public int TotalUnitsApproved { get; set; }
    public int TotalUnitsRejected { get; set; }
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}

public sealed class QualityControlActionRequest
{
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}

public sealed class QualityDefectRequest
{
    public string DefectCode { get; set; } = string.Empty;
    public string? DefectDescription { get; set; }
    public string? Severity { get; set; }
    public int QuantityAffected { get; set; }
    public string? UserId { get; set; }
}
