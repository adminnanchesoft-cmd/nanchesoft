using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductionScheduleEndpoints
{
    public static void MapProductionScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/production/schedules").WithTags("ProductionSchedules");

        // ─── List schedules ──────────────────────────────────────────────────
        g.MapGet("/", async (Guid? companyId, Guid? branchId, string? status, int year, NanchesoftDbContext db) =>
        {
            var query = db.ProductionSchedules.AsNoTracking().Include(x => x.Lines).AsQueryable();

            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToLower());
            if (year > 0) query = query.Where(x => x.WeekStartDate.Year == year);

            var items = await query.OrderByDescending(x => x.WeekStartDate)
                .Select(x => new ProductionScheduleSummaryDto
                {
                    ProductionScheduleId = x.Id,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    WeekCode = x.WeekCode,
                    WeekStartDate = x.WeekStartDate,
                    WeekEndDate = x.WeekEndDate,
                    Status = x.Status,
                    TotalCapacityUnits = x.TotalCapacityUnits,
                    TotalScheduledUnits = x.TotalScheduledUnits,
                    TotalProducedUnits = x.TotalProducedUnits,
                    LoadPercentage = x.LoadPercentage,
                    LineCount = x.Lines.Count,
                    LockedAt = x.LockedAt,
                    LockedBy = x.LockedBy
                })
                .ToListAsync();

            return Results.Ok(items);
        });

        // ─── Get schedule with lines ─────────────────────────────────────────
        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var schedule = await db.ProductionSchedules.AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (schedule is null) return Results.NotFound(new { message = "Programa semanal no encontrado." });

            var orderIds = schedule.Lines.Select(l => l.ProductionOrderId).Distinct().ToList();
            var orders = await db.ProductionOrders.AsNoTracking()
                .Where(x => orderIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Folio, x.TotalUnitsPlanned })
                .ToDictionaryAsync(x => x.Id);

            var phases = await db.ProductionPhases.AsNoTracking()
                .Select(x => new { x.Id, x.Code, x.Name })
                .ToDictionaryAsync(x => x.Id);

            var cells = await db.ProductionCells.AsNoTracking()
                .Where(x => x.CompanyId == schedule.CompanyId)
                .Select(x => new { x.Id, x.Code, x.Name })
                .ToDictionaryAsync(x => x.Id);

            return Results.Ok(new ProductionScheduleDetailDto
            {
                ProductionScheduleId = schedule.Id,
                CompanyId = schedule.CompanyId,
                BranchId = schedule.BranchId,
                WeekCode = schedule.WeekCode,
                WeekStartDate = schedule.WeekStartDate,
                WeekEndDate = schedule.WeekEndDate,
                Status = schedule.Status,
                TotalCapacityUnits = schedule.TotalCapacityUnits,
                TotalScheduledUnits = schedule.TotalScheduledUnits,
                TotalProducedUnits = schedule.TotalProducedUnits,
                LoadPercentage = schedule.LoadPercentage,
                Notes = schedule.Notes,
                LockedAt = schedule.LockedAt,
                LockedBy = schedule.LockedBy,
                Lines = schedule.Lines.Select(l =>
                {
                    orders.TryGetValue(l.ProductionOrderId, out var op);
                    phases.TryGetValue(l.ProductionPhaseId, out var ph);
                    var cellName = l.ProductionCellId.HasValue && cells.TryGetValue(l.ProductionCellId.Value, out var c) ? c.Name : string.Empty;
                    return new ProductionScheduleLineDto
                    {
                        ProductionScheduleLineId = l.Id,
                        ProductionOrderId = l.ProductionOrderId,
                        OrderFolio = op?.Folio ?? string.Empty,
                        ProductionOrderLineId = l.ProductionOrderLineId,
                        ProductionPhaseId = l.ProductionPhaseId,
                        PhaseName = ph?.Name ?? string.Empty,
                        ProductionCellId = l.ProductionCellId,
                        CellName = cellName,
                        ScheduledDate = l.ScheduledDate,
                        Shift = l.Shift,
                        UnitsScheduled = l.UnitsScheduled,
                        UnitsProduced = l.UnitsProduced
                    };
                }).OrderBy(l => l.ScheduledDate).ThenBy(l => l.PhaseName).ToList()
            });
        });

        // ─── Get or create schedule for a week ──────────────────────────────
        g.MapPost("/week", async (WeekScheduleRequest request, NanchesoftDbContext db) =>
        {
            if (request.CompanyId == Guid.Empty)
                return Results.BadRequest(new { message = "CompanyId es obligatorio." });
            if (request.BranchId == Guid.Empty)
                return Results.BadRequest(new { message = "BranchId es obligatorio." });
            if (string.IsNullOrWhiteSpace(request.WeekCode))
                return Results.BadRequest(new { message = "WeekCode es obligatorio (ej. 2026-W20)." });

            var weekCode = request.WeekCode.Trim().ToUpper();
            var existing = await db.ProductionSchedules
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.CompanyId == request.CompanyId
                    && x.BranchId == request.BranchId
                    && x.WeekCode == weekCode);

            if (existing is not null)
                return Results.Ok(new { productionScheduleId = existing.Id, weekCode = existing.WeekCode, created = false });

            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId);
            if (company is null) return Results.BadRequest(new { message = "Empresa no encontrada." });

            var (startDate, endDate) = ParseWeekCode(weekCode);
            if (startDate == default)
                return Results.BadRequest(new { message = "Formato de WeekCode inválido. Use 'YYYY-Www' (ej. 2026-W20)." });

            // Calculate capacity from production cells for this branch
            var totalCapacity = await db.ProductionCells.AsNoTracking()
                .Where(x => x.CompanyId == request.CompanyId && x.BranchId == request.BranchId && x.IsActive)
                .SumAsync(x => x.CapacityPerWeek);

            var schedule = new ProductionSchedule
            {
                TenantId = company.TenantId,
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                WeekCode = weekCode,
                WeekStartDate = startDate,
                WeekEndDate = endDate,
                Status = "open",
                TotalCapacityUnits = totalCapacity,
                Notes = request.Notes,
                CreatedBy = request.UserId ?? "api"
            };

            db.ProductionSchedules.Add(schedule);
            await db.SaveChangesAsync();

            return Results.Created($"/api/production/schedules/{schedule.Id}", new
            {
                productionScheduleId = schedule.Id,
                weekCode = schedule.WeekCode,
                created = true,
                totalCapacityUnits = totalCapacity
            });
        });

        // ─── Add production order to schedule ────────────────────────────────
        g.MapPost("/{id:guid}/lines", async (Guid id, ScheduleLineRequest request, NanchesoftDbContext db) =>
        {
            var schedule = await db.ProductionSchedules.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (schedule is null) return Results.NotFound(new { message = "Programa semanal no encontrado." });
            if (schedule.Status == "locked" || schedule.Status == "closed")
                return Results.BadRequest(new { message = $"No se puede modificar un programa en estado '{schedule.Status}'." });

            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.ProductionOrderId);
            if (order is null) return Results.BadRequest(new { message = "Orden de producción no encontrada." });

            var by = request.UserId ?? "api";

            var line = new ProductionScheduleLine
            {
                ProductionScheduleId = schedule.Id,
                ProductionOrderId = request.ProductionOrderId,
                ProductionOrderLineId = request.ProductionOrderLineId,
                ProductionPhaseId = request.ProductionPhaseId,
                ProductionCellId = request.ProductionCellId,
                ScheduledDate = request.ScheduledDate,
                Shift = string.IsNullOrWhiteSpace(request.Shift) ? "morning" : request.Shift.ToLower(),
                UnitsScheduled = request.UnitsScheduled,
                CreatedBy = by
            };

            schedule.Lines.Add(line);
            schedule.TotalScheduledUnits += request.UnitsScheduled;
            schedule.LoadPercentage = schedule.TotalCapacityUnits > 0
                ? Math.Round((decimal)schedule.TotalScheduledUnits / schedule.TotalCapacityUnits * 100, 2)
                : 0;
            schedule.UpdatedAt = DateTime.UtcNow;
            schedule.UpdatedBy = by;

            await db.SaveChangesAsync();
            return Results.Created($"/api/production/schedules/{id}/lines/{line.Id}", new
            {
                productionScheduleLineId = line.Id,
                loadPercentage = schedule.LoadPercentage
            });
        });

        // ─── Remove line from schedule ───────────────────────────────────────
        g.MapDelete("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, NanchesoftDbContext db) =>
        {
            var schedule = await db.ProductionSchedules.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (schedule is null) return Results.NotFound(new { message = "Programa no encontrado." });
            if (schedule.Status == "locked" || schedule.Status == "closed")
                return Results.BadRequest(new { message = "No se puede modificar un programa bloqueado o cerrado." });

            var line = schedule.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null) return Results.NotFound(new { message = "Línea no encontrada." });

            schedule.TotalScheduledUnits = Math.Max(0, schedule.TotalScheduledUnits - line.UnitsScheduled);
            schedule.LoadPercentage = schedule.TotalCapacityUnits > 0
                ? Math.Round((decimal)schedule.TotalScheduledUnits / schedule.TotalCapacityUnits * 100, 2)
                : 0;
            schedule.Lines.Remove(line);
            db.ProductionScheduleLines.Remove(line);
            schedule.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Línea eliminada.", loadPercentage = schedule.LoadPercentage });
        });

        // ─── Lock schedule ───────────────────────────────────────────────────
        g.MapPost("/{id:guid}/lock", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var schedule = await db.ProductionSchedules.FirstOrDefaultAsync(x => x.Id == id);
            if (schedule is null) return Results.NotFound(new { message = "Programa no encontrado." });
            if (schedule.Status != "open" && schedule.Status != "planning")
                return Results.BadRequest(new { message = $"Solo se puede bloquear un programa en estado 'open' o 'planning'." });

            schedule.Status = "locked";
            schedule.LockedAt = DateTime.UtcNow;
            schedule.LockedBy = request.UserId ?? "api";
            schedule.UpdatedAt = DateTime.UtcNow;
            schedule.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Programa bloqueado.", status = schedule.Status });
        });

        // ─── Close schedule ──────────────────────────────────────────────────
        g.MapPost("/{id:guid}/close", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var schedule = await db.ProductionSchedules.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (schedule is null) return Results.NotFound(new { message = "Programa no encontrado." });
            if (schedule.Status != "locked")
                return Results.BadRequest(new { message = "Solo se puede cerrar un programa bloqueado." });

            schedule.TotalProducedUnits = schedule.Lines.Sum(l => l.UnitsProduced);
            schedule.Status = "closed";
            schedule.ClosedAt = DateTime.UtcNow;
            schedule.ClosedBy = request.UserId ?? "api";
            schedule.UpdatedAt = DateTime.UtcNow;
            schedule.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Programa cerrado.", status = schedule.Status });
        });

        // ─── Capacity board: available capacity by phase/cell for a week ────
        app.MapGet("/api/production/capacity/{weekCode}", async (string weekCode, Guid? companyId, Guid? branchId, NanchesoftDbContext db) =>
        {
            var wc = weekCode.Trim().ToUpper();

            var cellQuery = db.ProductionCells.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .Where(x => x.IsActive);

            if (companyId.HasValue) cellQuery = cellQuery.Where(x => x.CompanyId == companyId.Value);
            if (branchId.HasValue) cellQuery = cellQuery.Where(x => x.BranchId == branchId.Value);

            var cells = await cellQuery.ToListAsync();

            // Load already-scheduled units for this week
            var scheduleQuery = db.ProductionScheduleLines.AsNoTracking()
                .Include(x => x.ProductionSchedule)
                .Where(x => x.ProductionSchedule.WeekCode == wc);

            if (companyId.HasValue)
                scheduleQuery = scheduleQuery.Where(x => x.ProductionSchedule.CompanyId == companyId.Value);

            var scheduledByCell = await scheduleQuery
                .Where(x => x.ProductionCellId.HasValue)
                .GroupBy(x => x.ProductionCellId!.Value)
                .Select(g => new { CellId = g.Key, Scheduled = g.Sum(l => l.UnitsScheduled) })
                .ToDictionaryAsync(x => x.CellId, x => x.Scheduled);

            var capacityBoard = cells.Select(c => new
            {
                CellId = c.Id,
                c.Code,
                c.Name,
                PhaseCode = c.ProductionPhase?.Code ?? string.Empty,
                PhaseName = c.ProductionPhase?.Name ?? string.Empty,
                c.CapacityPerWeek,
                Scheduled = scheduledByCell.TryGetValue(c.Id, out var s) ? s : 0,
                Available = c.CapacityPerWeek - (scheduledByCell.TryGetValue(c.Id, out var s2) ? s2 : 0),
                LoadPercent = c.CapacityPerWeek > 0
                    ? Math.Round((decimal)(scheduledByCell.TryGetValue(c.Id, out var s3) ? s3 : 0) / c.CapacityPerWeek * 100, 1)
                    : 0m
            }).OrderBy(x => x.PhaseCode).ThenBy(x => x.Code).ToList();

            return Results.Ok(new { weekCode = wc, cells = capacityBoard });
        }).WithTags("ProductionSchedules");

        // ─── Orders ready to schedule (planned/reserved) ─────────────────────
        app.MapGet("/api/production/orders-to-schedule", async (Guid? companyId, NanchesoftDbContext db) =>
        {
            var query = db.ProductionOrders.AsNoTracking()
                .Include(x => x.Lines)
                .Where(x => x.Status == "planned" || x.Status == "exploded" || x.Status == "reserved");

            if (companyId.HasValue)
                query = query.Where(x => x.CompanyId == companyId.Value);

            var items = await query.OrderBy(x => x.DeliveryDate).ThenBy(x => x.Priority)
                .Select(x => new
                {
                    x.Id,
                    x.Folio,
                    x.WeekCode,
                    x.Status,
                    x.Priority,
                    x.DeliveryDate,
                    x.TotalUnitsPlanned,
                    x.TotalUnitsProduced,
                    LineCount = x.Lines.Count
                })
                .ToListAsync();

            return Results.Ok(items);
        }).WithTags("ProductionSchedules");
    }

    private static (DateOnly start, DateOnly end) ParseWeekCode(string weekCode)
    {
        try
        {
            // Format: YYYY-Www  (ISO 8601 week numbering)
            var parts = weekCode.Split('-');
            if (parts.Length != 2) return (default, default);
            if (!int.TryParse(parts[0], out var year)) return (default, default);
            var weekPart = parts[1];
            if (!weekPart.StartsWith('W')) return (default, default);
            if (!int.TryParse(weekPart[1..], out var week)) return (default, default);

            // Jan 4 is always in ISO week 1; treat Sunday as 7 (not 0) for correct Monday anchor
            var jan4 = new DateOnly(year, 1, 4);
            var dow = jan4.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)jan4.DayOfWeek;
            var startOfYear = jan4.AddDays(1 - dow);   // Monday of W01
            var start = startOfYear.AddDays((week - 1) * 7);
            var end = start.AddDays(6);
            return (start, end);
        }
        catch
        {
            return (default, default);
        }
    }
}

public sealed class ProductionScheduleSummaryDto
{
    public Guid ProductionScheduleId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string WeekCode { get; set; } = string.Empty;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalCapacityUnits { get; set; }
    public int TotalScheduledUnits { get; set; }
    public int TotalProducedUnits { get; set; }
    public decimal LoadPercentage { get; set; }
    public int LineCount { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
}

public sealed class ProductionScheduleDetailDto
{
    public Guid ProductionScheduleId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string WeekCode { get; set; } = string.Empty;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalCapacityUnits { get; set; }
    public int TotalScheduledUnits { get; set; }
    public int TotalProducedUnits { get; set; }
    public decimal LoadPercentage { get; set; }
    public int LineCount { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public string? Notes { get; set; }
    public List<ProductionScheduleLineDto> Lines { get; set; } = new();
}

public sealed class ProductionScheduleLineDto
{
    public Guid ProductionScheduleLineId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public string OrderFolio { get; set; } = string.Empty;
    public Guid ProductionOrderLineId { get; set; }
    public Guid ProductionPhaseId { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public Guid? ProductionCellId { get; set; }
    public string CellName { get; set; } = string.Empty;
    public DateOnly ScheduledDate { get; set; }
    public string Shift { get; set; } = string.Empty;
    public int UnitsScheduled { get; set; }
    public int UnitsProduced { get; set; }
}

public sealed class WeekScheduleRequest
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string WeekCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}

public sealed class ScheduleLineRequest
{
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionOrderLineId { get; set; }
    public Guid ProductionPhaseId { get; set; }
    public Guid? ProductionCellId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public string Shift { get; set; } = "morning";
    public int UnitsScheduled { get; set; }
    public string? UserId { get; set; }
}
