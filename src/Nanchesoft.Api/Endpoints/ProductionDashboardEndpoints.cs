using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductionDashboardEndpoints
{
    public static void MapProductionDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── KPI summary ─────────────────────────────────────────────────────
        app.MapGet("/api/production/dashboard/kpis", async (
            Guid companyId,
            string? weekCode,
            NanchesoftDbContext db) =>
        {
            if (companyId == Guid.Empty)
                return Results.BadRequest(new { message = "companyId es obligatorio." });

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            // Active orders
            var orderQuery = db.ProductionOrders.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive);

            if (!string.IsNullOrWhiteSpace(weekCode))
                orderQuery = orderQuery.Where(x => x.WeekCode == weekCode.Trim().ToUpper());

            var orderStats = await orderQuery
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Units = g.Sum(x => x.TotalUnitsPlanned) })
                .ToListAsync();

            var totalOrders = orderStats.Sum(x => x.Count);
            var inProgressOrders = orderStats.Where(x => x.Status == "in_progress").Sum(x => x.Count);
            var completedOrders = orderStats.Where(x => x.Status == "completed" || x.Status == "closed").Sum(x => x.Count);
            var plannedOrders = orderStats.Where(x => x.Status is "draft" or "planned" or "exploded" or "reserved").Sum(x => x.Count);
            var totalPlannedUnits = orderStats.Sum(x => x.Units);

            // Production produced this week
            var weekStart = string.IsNullOrWhiteSpace(weekCode)
                ? today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday)
                : await GetWeekStartFromCodeAsync(db, companyId, weekCode.Trim().ToUpper(), today);

            var producedThisWeek = await db.ProductionVouchers.AsNoTracking()
                .Where(x => x.CompanyId == companyId
                    && x.Status == "completed"
                    && x.CompletedDate >= weekStart
                    && x.CompletedDate <= weekStart.AddDays(6))
                .SumAsync(x => (int?)x.BatchSize) ?? 0;

            // Piecework this week
            var pieceWorkThisWeek = await db.PieceWorkRecords.AsNoTracking()
                .Where(x => x.CompanyId == companyId
                    && x.WorkDate >= weekStart
                    && x.WorkDate <= weekStart.AddDays(6)
                    && x.Status != "cancelled")
                .SumAsync(x => (decimal?)x.NetAmount) ?? 0m;

            // Pending material shortages
            var shortages = await db.MaterialRequirements.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Status == "with_shortages")
                .CountAsync();

            // Vouchers issued today
            var vouchersToday = await db.ProductionVouchers.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IssuedDate == today)
                .CountAsync();

            // Units in process right now (by phase)
            var inProcessByPhase = await db.ProductionInProcess.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .Where(x => x.CompanyId == companyId && x.UnitsCurrent > 0)
                .GroupBy(x => new { x.ProductionPhaseId, PhaseName = x.ProductionPhase!.Name, Seq = x.ProductionPhase.Sequence })
                .Select(g => new
                {
                    g.Key.ProductionPhaseId,
                    g.Key.PhaseName,
                    g.Key.Seq,
                    UnitsCurrent = g.Sum(x => x.UnitsCurrent)
                })
                .OrderBy(x => x.Seq)
                .ToListAsync();

            // Weekly schedule load
            var scheduleLoad = string.IsNullOrWhiteSpace(weekCode)
                ? null
                : await db.ProductionSchedules.AsNoTracking()
                    .Where(x => x.CompanyId == companyId && x.WeekCode == weekCode.Trim().ToUpper())
                    .Select(x => new { x.TotalCapacityUnits, x.TotalScheduledUnits, x.LoadPercentage, x.Status })
                    .FirstOrDefaultAsync();

            return Results.Ok(new
            {
                companyId,
                weekCode = weekCode?.Trim().ToUpper() ?? string.Empty,
                generatedAt = now,
                orders = new
                {
                    total = totalOrders,
                    inProgress = inProgressOrders,
                    completed = completedOrders,
                    planned = plannedOrders,
                    totalPlannedUnits
                },
                production = new
                {
                    producedThisWeek,
                    vouchersIssuedToday = vouchersToday,
                    pieceWorkNetThisWeek = pieceWorkThisWeek
                },
                alerts = new
                {
                    materialShortages = shortages
                },
                inProcessByPhase,
                schedule = scheduleLoad is null ? null : new
                {
                    scheduleLoad.TotalCapacityUnits,
                    scheduleLoad.TotalScheduledUnits,
                    scheduleLoad.LoadPercentage,
                    scheduleLoad.Status
                }
            });
        }).WithTags("ProductionDashboard");

        // ─── Orders board (kanban-style by status) ────────────────────────────
        app.MapGet("/api/production/dashboard/orders-board", async (
            Guid companyId,
            string? weekCode,
            NanchesoftDbContext db) =>
        {
            var query = db.ProductionOrders.AsNoTracking()
                .Include(x => x.Lines)
                .Where(x => x.CompanyId == companyId && x.IsActive);

            if (!string.IsNullOrWhiteSpace(weekCode))
                query = query.Where(x => x.WeekCode == weekCode.Trim().ToUpper());

            var orders = await query
                .OrderBy(x => x.Priority).ThenBy(x => x.DeliveryDate)
                .Select(x => new OrderBoardItemDto
                {
                    ProductionOrderId = x.Id,
                    Folio = x.Folio,
                    WeekCode = x.WeekCode,
                    Status = x.Status,
                    ExplosionStatus = x.ExplosionStatus,
                    Priority = x.Priority,
                    DeliveryDate = x.DeliveryDate,
                    TotalUnitsPlanned = x.TotalUnitsPlanned,
                    TotalUnitsProduced = x.TotalUnitsProduced,
                    ProgressPercent = x.TotalUnitsPlanned > 0
                        ? Math.Round((decimal)x.TotalUnitsProduced / x.TotalUnitsPlanned * 100, 1)
                        : 0,
                    LineCount = x.Lines.Count,
                    IsOverdue = x.DeliveryDate < DateOnly.FromDateTime(DateTime.UtcNow)
                        && x.Status != "completed" && x.Status != "closed" && x.Status != "cancelled"
                })
                .ToListAsync();

            var board = new Dictionary<string, List<OrderBoardItemDto>>
            {
                ["draft"] = orders.Where(o => o.Status == "draft").ToList(),
                ["planned"] = orders.Where(o => o.Status is "planned" or "exploded" or "reserved").ToList(),
                ["in_progress"] = orders.Where(o => o.Status == "in_progress").ToList(),
                ["completed"] = orders.Where(o => o.Status is "completed" or "closed").ToList(),
                ["cancelled"] = orders.Where(o => o.Status == "cancelled").ToList()
            };

            return Results.Ok(new { companyId, weekCode, board });
        }).WithTags("ProductionDashboard");

        // ─── Phase throughput (units completed per phase today/week) ──────────
        app.MapGet("/api/production/dashboard/phase-throughput", async (
            Guid companyId,
            string period,
            NanchesoftDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly from = period == "today" ? today : today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            DateOnly to = period == "today" ? today : from.AddDays(6);

            var throughput = await db.PieceWorkRecords.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .Where(x => x.CompanyId == companyId
                    && x.WorkDate >= from && x.WorkDate <= to
                    && x.Status != "cancelled")
                .GroupBy(x => new { x.ProductionPhaseId, PhaseName = x.ProductionPhase!.Name, Seq = x.ProductionPhase.Sequence })
                .Select(g => new
                {
                    g.Key.ProductionPhaseId,
                    g.Key.PhaseName,
                    g.Key.Seq,
                    TotalProduced = g.Sum(r => r.UnitsProduced),
                    TotalRejected = g.Sum(r => r.UnitsRejected),
                    TotalGross = g.Sum(r => r.GrossAmount),
                    TotalNet = g.Sum(r => r.NetAmount),
                    EmployeeCount = g.Select(r => r.EmployeeId).Distinct().Count()
                })
                .OrderBy(x => x.Seq)
                .ToListAsync();

            // Daily breakdown (only for week view)
            List<object> dailyBreakdown = new();
            if (period == "week")
            {
                for (var d = from; d <= to; d = d.AddDays(1))
                {
                    var dayDate = d;
                    var dayTotals = await db.PieceWorkRecords.AsNoTracking()
                        .Where(x => x.CompanyId == companyId && x.WorkDate == dayDate && x.Status != "cancelled")
                        .SumAsync(x => (int?)x.UnitsProduced) ?? 0;
                    dailyBreakdown.Add(new { date = dayDate, unitsProduced = dayTotals });
                }
            }

            return Results.Ok(new { companyId, period, from, to, phases = throughput, dailyBreakdown });
        }).WithTags("ProductionDashboard");

        // ─── In-process units: record entry/exit ─────────────────────────────
        app.MapPost("/api/production/in-process", async (InProcessEntryRequest request, NanchesoftDbContext db) =>
        {
            if (request.ProductionOrderId == Guid.Empty || request.ProductionPhaseId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductionOrderId y ProductionPhaseId son obligatorios." });

            var order = await db.ProductionOrders.AsNoTracking()
                .Select(x => new { x.Id, x.TenantId, x.CompanyId })
                .FirstOrDefaultAsync(x => x.Id == request.ProductionOrderId);
            if (order is null) return Results.BadRequest(new { message = "Orden no encontrada." });

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var by = request.UserId ?? "api";

            // Find or create today's in-process record for this order/line/phase/cell
            var existing = await db.ProductionInProcess
                .FirstOrDefaultAsync(x =>
                    x.ProductionOrderId == request.ProductionOrderId &&
                    x.ProductionOrderLineId == request.ProductionOrderLineId &&
                    x.ProductionPhaseId == request.ProductionPhaseId &&
                    x.ProductionCellId == request.ProductionCellId &&
                    x.EntryDate == today);

            if (existing is null)
            {
                existing = new ProductionInProcess
                {
                    TenantId = order.TenantId,
                    CompanyId = order.CompanyId,
                    ProductionOrderId = request.ProductionOrderId,
                    ProductionOrderLineId = request.ProductionOrderLineId,
                    ProductionPhaseId = request.ProductionPhaseId,
                    ProductionCellId = request.ProductionCellId,
                    EntryDate = today,
                    EnteredBy = by,
                    CreatedBy = by
                };
                db.ProductionInProcess.Add(existing);
            }

            existing.UnitsEntered += request.UnitsEntered;
            existing.UnitsExited += request.UnitsExited;
            existing.UnitsRejected += request.UnitsRejected;
            existing.UnitsCurrent = Math.Max(0, existing.UnitsEntered - existing.UnitsExited - existing.UnitsRejected);
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = by;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                productionInProcessId = existing.Id,
                existing.UnitsEntered,
                existing.UnitsExited,
                existing.UnitsRejected,
                existing.UnitsCurrent
            });
        }).WithTags("ProductionDashboard");

        // ─── In-process current snapshot ─────────────────────────────────────
        app.MapGet("/api/production/in-process", async (
            Guid companyId,
            Guid? orderId,
            Guid? phaseId,
            NanchesoftDbContext db) =>
        {
            var query = db.ProductionInProcess.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .Include(x => x.ProductionCell)
                .Where(x => x.CompanyId == companyId && x.UnitsCurrent > 0);

            if (orderId.HasValue) query = query.Where(x => x.ProductionOrderId == orderId.Value);
            if (phaseId.HasValue) query = query.Where(x => x.ProductionPhaseId == phaseId.Value);

            var items = await query.OrderBy(x => x.ProductionPhase!.Sequence).ThenBy(x => x.EntryDate)
                .Select(x => new
                {
                    InProcessId = x.Id,
                    x.ProductionOrderId,
                    x.ProductionOrderLineId,
                    x.ProductionPhaseId,
                    PhaseName = x.ProductionPhase != null ? x.ProductionPhase.Name : string.Empty,
                    PhaseSequence = x.ProductionPhase != null ? x.ProductionPhase.Sequence : 0,
                    CellName = x.ProductionCell != null ? x.ProductionCell.Name : string.Empty,
                    x.EntryDate,
                    x.UnitsEntered,
                    x.UnitsExited,
                    x.UnitsRejected,
                    x.UnitsCurrent
                })
                .ToListAsync();

            return Results.Ok(new { companyId, snapshot = DateTime.UtcNow, items });
        }).WithTags("ProductionDashboard");

        // ─── Efficiency report by employee for a week ─────────────────────────
        app.MapGet("/api/production/dashboard/efficiency", async (
            Guid companyId,
            DateOnly from,
            DateOnly to,
            NanchesoftDbContext db) =>
        {
            var records = await db.PieceWorkRecords.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .Where(x => x.CompanyId == companyId
                    && x.WorkDate >= from && x.WorkDate <= to
                    && x.Status != "cancelled")
                .ToListAsync();

            var employeeIds = records.Select(r => r.EmployeeId).Distinct().ToList();
            var employees = await db.Employees.AsNoTracking()
                .Where(x => employeeIds.Contains(x.Id))
                .Select(x => new { x.Id, x.EmployeeNumber, FullName = x.FirstName + " " + x.LastName, x.DailySalary })
                .ToDictionaryAsync(x => x.Id);

            var phaseCapacities = await db.PieceWorkRates.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive)
                .ToListAsync();

            var report = records
                .GroupBy(r => r.EmployeeId)
                .Select(g =>
                {
                    employees.TryGetValue(g.Key, out var emp);
                    var daysWorked = g.Select(r => r.WorkDate).Distinct().Count();
                    var totalProduced = g.Sum(r => r.UnitsProduced);
                    var totalRejected = g.Sum(r => r.UnitsRejected);
                    var totalNet = g.Sum(r => r.NetAmount);
                    var rejectionRate = totalProduced > 0
                        ? Math.Round((decimal)totalRejected / (totalProduced + totalRejected) * 100, 2)
                        : 0m;

                    // Phase breakdown
                    var byPhase = g.GroupBy(r => new { r.ProductionPhaseId, PhaseName = r.ProductionPhase?.Name ?? string.Empty })
                        .Select(pg => new
                        {
                            pg.Key.ProductionPhaseId,
                            pg.Key.PhaseName,
                            Units = pg.Sum(r => r.UnitsProduced),
                            Net = pg.Sum(r => r.NetAmount)
                        }).OrderByDescending(x => x.Units).ToList();

                    return new
                    {
                        EmployeeId = g.Key,
                        EmployeeNumber = emp?.EmployeeNumber ?? string.Empty,
                        EmployeeName = emp?.FullName ?? string.Empty,
                        DaysWorked = daysWorked,
                        TotalProduced = totalProduced,
                        TotalRejected = totalRejected,
                        RejectionRate = rejectionRate,
                        TotalNetEarned = totalNet,
                        AvgUnitsPerDay = daysWorked > 0 ? Math.Round((decimal)totalProduced / daysWorked, 1) : 0,
                        ByPhase = byPhase
                    };
                })
                .OrderByDescending(x => x.TotalProduced)
                .ToList();

            return Results.Ok(new
            {
                companyId,
                from,
                to,
                employeeCount = report.Count,
                grandTotalProduced = report.Sum(r => r.TotalProduced),
                grandTotalNet = report.Sum(r => r.TotalNetEarned),
                avgRejectionRate = report.Count > 0 ? Math.Round(report.Average(r => r.RejectionRate), 2) : 0,
                employees = report
            });
        }).WithTags("ProductionDashboard");

        // ─── Surplus records ──────────────────────────────────────────────────
        app.MapGet("/api/production/surplus", async (Guid? companyId, string? disposition, NanchesoftDbContext db) =>
        {
            var query = db.SurplusRecords.AsNoTracking()
                .Include(x => x.FinishedProduct)
                .Where(x => x.IsActive);

            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            if (!string.IsNullOrWhiteSpace(disposition))
                query = query.Where(x => x.Disposition == disposition.Trim().ToLower());

            var items = await query.OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    SurplusId = x.Id,
                    x.CompanyId,
                    x.ProductionOrderId,
                    x.FinishedProductId,
                    ProductCode = x.FinishedProduct != null ? x.FinishedProduct.Code : string.Empty,
                    ProductName = x.FinishedProduct != null ? x.FinishedProduct.Name : string.Empty,
                    x.UnitsPlanned,
                    x.UnitsProduced,
                    x.UnitsSurplus,
                    x.Disposition,
                    x.AssignedOrderId,
                    x.Notes,
                    x.ResolvedBy,
                    x.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(items);
        }).WithTags("ProductionDashboard");

        app.MapPost("/api/production/surplus", async (RegisterSurplusRequest request, NanchesoftDbContext db) =>
        {
            if (request.ProductionOrderId == Guid.Empty || request.FinishedProductId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductionOrderId y FinishedProductId son obligatorios." });

            var order = await db.ProductionOrders.AsNoTracking()
                .Select(x => new { x.Id, x.TenantId, x.CompanyId })
                .FirstOrDefaultAsync(x => x.Id == request.ProductionOrderId);
            if (order is null) return Results.BadRequest(new { message = "Orden no encontrada." });

            var surplus = new SurplusRecord
            {
                TenantId = order.TenantId,
                CompanyId = order.CompanyId,
                ProductionOrderId = request.ProductionOrderId,
                FinishedProductId = request.FinishedProductId,
                SizeRunSizeId = request.SizeRunSizeId,
                UnitsPlanned = request.UnitsPlanned,
                UnitsProduced = request.UnitsProduced,
                UnitsSurplus = request.UnitsProduced - request.UnitsPlanned,
                Disposition = "pending",
                Notes = request.Notes ?? string.Empty,
                CreatedBy = request.UserId ?? "api"
            };

            db.SurplusRecords.Add(surplus);
            await db.SaveChangesAsync();

            return Results.Created($"/api/production/surplus/{surplus.Id}", new { surplusId = surplus.Id });
        }).WithTags("ProductionDashboard");

        app.MapPut("/api/production/surplus/{id:guid}/disposition", async (Guid id, SurplusDispositionRequest request, NanchesoftDbContext db) =>
        {
            var surplus = await db.SurplusRecords.FirstOrDefaultAsync(x => x.Id == id);
            if (surplus is null) return Results.NotFound(new { message = "Registro de sobrante no encontrado." });

            surplus.Disposition = request.Disposition?.Trim().ToLower() ?? surplus.Disposition;
            surplus.AssignedOrderId = request.AssignedOrderId;
            surplus.ResolvedBy = request.UserId ?? "api";
            surplus.Notes = string.IsNullOrWhiteSpace(request.Notes) ? surplus.Notes : surplus.Notes + "\n" + request.Notes;
            surplus.UpdatedAt = DateTime.UtcNow;
            surplus.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Disposición actualizada.", disposition = surplus.Disposition });
        }).WithTags("ProductionDashboard");
    }

    private static async Task<DateOnly> GetWeekStartFromCodeAsync(
        NanchesoftDbContext db, Guid companyId, string weekCode, DateOnly fallback)
    {
        var schedule = await db.ProductionSchedules.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.WeekCode == weekCode)
            .Select(x => x.WeekStartDate)
            .FirstOrDefaultAsync();

        return schedule == default ? fallback.AddDays(-(int)fallback.DayOfWeek + (int)DayOfWeek.Monday) : schedule;
    }
}

public sealed class OrderBoardItemDto
{
    public Guid ProductionOrderId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string WeekCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ExplosionStatus { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public int TotalUnitsPlanned { get; set; }
    public int TotalUnitsProduced { get; set; }
    public decimal ProgressPercent { get; set; }
    public int LineCount { get; set; }
    public bool IsOverdue { get; set; }
}

public sealed class InProcessEntryRequest
{
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionOrderLineId { get; set; }
    public Guid ProductionPhaseId { get; set; }
    public Guid? ProductionCellId { get; set; }
    public int UnitsEntered { get; set; }
    public int UnitsExited { get; set; }
    public int UnitsRejected { get; set; }
    public string? UserId { get; set; }
}

public sealed class RegisterSurplusRequest
{
    public Guid ProductionOrderId { get; set; }
    public Guid FinishedProductId { get; set; }
    public Guid? SizeRunSizeId { get; set; }
    public int UnitsPlanned { get; set; }
    public int UnitsProduced { get; set; }
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}

public sealed class SurplusDispositionRequest
{
    public string? Disposition { get; set; }
    public Guid? AssignedOrderId { get; set; }
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}
