using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductionPieceWorkEndpoints
{
    public static void MapProductionPieceWorkEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/production/piecework").WithTags("ProductionPiecework");

        // ─── List records ────────────────────────────────────────────────────
        g.MapGet("/records", async (
            Guid? companyId,
            Guid? employeeId,
            Guid? orderId,
            string? status,
            DateOnly? from,
            DateOnly? to,
            int page,
            int pageSize,
            NanchesoftDbContext db) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 200);

            var query = db.PieceWorkRecords.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .AsQueryable();

            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
            if (orderId.HasValue) query = query.Where(x => x.ProductionOrderId == orderId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToLower());
            if (from.HasValue) query = query.Where(x => x.WorkDate >= from.Value);
            if (to.HasValue) query = query.Where(x => x.WorkDate <= to.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.WorkDate)
                .ThenBy(x => x.EmployeeId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PieceWorkRecordDto
                {
                    PieceWorkRecordId = x.Id,
                    EmployeeId = x.EmployeeId,
                    ProductionOrderId = x.ProductionOrderId,
                    ProductionPhaseId = x.ProductionPhaseId,
                    PhaseName = x.ProductionPhase != null ? x.ProductionPhase.Name : string.Empty,
                    ProductionVoucherId = x.ProductionVoucherId,
                    PayrollPeriodId = x.PayrollPeriodId,
                    WorkDate = x.WorkDate,
                    UnitsProduced = x.UnitsProduced,
                    UnitsRejected = x.UnitsRejected,
                    UnitPrice = x.UnitPrice,
                    GrossAmount = x.GrossAmount,
                    QualityDeduction = x.QualityDeduction,
                    NetAmount = x.NetAmount,
                    Status = x.Status,
                    ApprovedBy = x.ApprovedBy,
                    ApprovedAt = x.ApprovedAt
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // ─── Register piecework record ────────────────────────────────────────
        g.MapPost("/records", async (RegisterPieceWorkRequest request, NanchesoftDbContext db) =>
        {
            if (request.EmployeeId == Guid.Empty)
                return Results.BadRequest(new { message = "EmployeeId es obligatorio." });
            if (request.ProductionOrderId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductionOrderId es obligatorio." });
            if (request.ProductionPhaseId == Guid.Empty)
                return Results.BadRequest(new { message = "ProductionPhaseId es obligatorio." });
            if (request.UnitsProduced <= 0)
                return Results.BadRequest(new { message = "UnitsProduced debe ser mayor a cero." });

            var order = await db.ProductionOrders.AsNoTracking()
                .Select(x => new { x.Id, x.TenantId, x.CompanyId })
                .FirstOrDefaultAsync(x => x.Id == request.ProductionOrderId);
            if (order is null) return Results.BadRequest(new { message = "Orden no encontrada." });

            // Resolve effective unit price: explicit override, else latest rate for phase
            var unitPrice = request.UnitPriceOverride ?? await GetEffectivePriceAsync(
                db, order.CompanyId, request.ProductionPhaseId, request.WorkDate);

            var qualityDeduction = request.UnitsRejected > 0
                ? Math.Round(unitPrice * request.UnitsRejected * 0.5m, 4)
                : 0m;

            var grossAmount = Math.Round(unitPrice * request.UnitsProduced, 4);
            var netAmount = Math.Round(grossAmount - qualityDeduction, 4);

            var record = new PieceWorkRecord
            {
                TenantId = order.TenantId,
                CompanyId = order.CompanyId,
                EmployeeId = request.EmployeeId,
                ProductionVoucherId = request.ProductionVoucherId,
                ProductionOrderId = request.ProductionOrderId,
                ProductionPhaseId = request.ProductionPhaseId,
                WorkDate = request.WorkDate,
                UnitsProduced = request.UnitsProduced,
                UnitsRejected = request.UnitsRejected,
                UnitPrice = unitPrice,
                GrossAmount = grossAmount,
                QualityDeduction = qualityDeduction,
                NetAmount = netAmount,
                Status = "pending",
                CreatedBy = request.UserId ?? "api"
            };

            db.PieceWorkRecords.Add(record);
            await db.SaveChangesAsync();

            return Results.Created($"/api/production/piecework/records/{record.Id}", new
            {
                pieceWorkRecordId = record.Id,
                unitPrice,
                grossAmount,
                qualityDeduction,
                netAmount
            });
        });

        // ─── Approve record ──────────────────────────────────────────────────
        g.MapPost("/records/{id:guid}/approve", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var record = await db.PieceWorkRecords.FirstOrDefaultAsync(x => x.Id == id);
            if (record is null) return Results.NotFound(new { message = "Registro no encontrado." });
            if (record.Status != "pending")
                return Results.BadRequest(new { message = $"Solo se pueden aprobar registros 'pending'. Estado: '{record.Status}'." });

            var by = request.UserId ?? "api";
            record.Status = "approved";
            record.ApprovedBy = by;
            record.ApprovedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = by;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Registro aprobado.", status = record.Status });
        });

        // ─── Bulk approve (week/employee) ─────────────────────────────────────
        g.MapPost("/records/bulk-approve", async (BulkApproveRequest request, NanchesoftDbContext db) =>
        {
            if (request.CompanyId == Guid.Empty)
                return Results.BadRequest(new { message = "CompanyId es obligatorio." });

            var by = request.UserId ?? "api";
            var now = DateTime.UtcNow;

            var query = db.PieceWorkRecords.Where(x => x.CompanyId == request.CompanyId && x.Status == "pending");

            if (request.EmployeeId.HasValue) query = query.Where(x => x.EmployeeId == request.EmployeeId.Value);
            if (request.FromDate.HasValue) query = query.Where(x => x.WorkDate >= request.FromDate.Value);
            if (request.ToDate.HasValue) query = query.Where(x => x.WorkDate <= request.ToDate.Value);

            var records = await query.ToListAsync();
            foreach (var r in records)
            {
                r.Status = "approved";
                r.ApprovedBy = by;
                r.ApprovedAt = now;
                r.UpdatedAt = now;
                r.UpdatedBy = by;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"{records.Count} registros aprobados.", count = records.Count });
        });

        // ─── Summary by employee for a date range ─────────────────────────────
        g.MapGet("/summary", async (
            Guid companyId,
            DateOnly from,
            DateOnly to,
            NanchesoftDbContext db) =>
        {
            var records = await db.PieceWorkRecords.AsNoTracking()
                .Where(x => x.CompanyId == companyId
                    && x.WorkDate >= from && x.WorkDate <= to
                    && x.Status != "cancelled")
                .GroupBy(x => x.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    TotalUnitsProduced = g.Sum(r => r.UnitsProduced),
                    TotalUnitsRejected = g.Sum(r => r.UnitsRejected),
                    TotalGross = g.Sum(r => r.GrossAmount),
                    TotalDeductions = g.Sum(r => r.QualityDeduction),
                    TotalNet = g.Sum(r => r.NetAmount),
                    PendingCount = g.Count(r => r.Status == "pending"),
                    ApprovedCount = g.Count(r => r.Status == "approved"),
                    ProcessedCount = g.Count(r => r.Status == "processed")
                })
                .ToListAsync();

            var employeeIds = records.Select(r => r.EmployeeId).ToList();
            var employees = await db.Employees.AsNoTracking()
                .Where(x => employeeIds.Contains(x.Id))
                .Select(x => new { x.Id, x.EmployeeNumber, FullName = x.FirstName + " " + x.LastName })
                .ToDictionaryAsync(x => x.Id);

            var result = records.Select(r =>
            {
                employees.TryGetValue(r.EmployeeId, out var emp);
                return new
                {
                    r.EmployeeId,
                    EmployeeNumber = emp?.EmployeeNumber ?? string.Empty,
                    EmployeeName = emp?.FullName ?? string.Empty,
                    r.TotalUnitsProduced,
                    r.TotalUnitsRejected,
                    r.TotalGross,
                    r.TotalDeductions,
                    r.TotalNet,
                    r.PendingCount,
                    r.ApprovedCount,
                    r.ProcessedCount
                };
            }).OrderBy(x => x.EmployeeName).ToList();

            return Results.Ok(new
            {
                companyId,
                from,
                to,
                employeeCount = result.Count,
                grandTotalGross = result.Sum(r => r.TotalGross),
                grandTotalNet = result.Sum(r => r.TotalNet),
                employees = result
            });
        });

        // ─── Process week into payroll ────────────────────────────────────────
        g.MapPost("/process-to-payroll", async (ProcessToPayrollRequest request, NanchesoftDbContext db) =>
        {
            if (request.CompanyId == Guid.Empty)
                return Results.BadRequest(new { message = "CompanyId es obligatorio." });
            if (request.PayrollPeriodId == Guid.Empty)
                return Results.BadRequest(new { message = "PayrollPeriodId es obligatorio." });
            if (request.FromDate == default || request.ToDate == default)
                return Results.BadRequest(new { message = "FromDate y ToDate son obligatorios." });

            var period = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == request.PayrollPeriodId);
            if (period is null) return Results.BadRequest(new { message = "Período de nómina no encontrado." });
            if (period.IsClosed)
                return Results.BadRequest(new { message = "El período de nómina está cerrado." });

            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId);
            if (company is null) return Results.BadRequest(new { message = "Empresa no encontrada." });

            var by = request.UserId ?? "api";
            var now = DateTime.UtcNow;

            // Fetch approved piecework records in range
            var records = await db.PieceWorkRecords
                .Where(x => x.CompanyId == request.CompanyId
                    && x.WorkDate >= request.FromDate
                    && x.WorkDate <= request.ToDate
                    && x.Status == "approved"
                    && x.PayrollPeriodId == null)
                .ToListAsync();

            if (!records.Any())
                return Results.BadRequest(new { message = "No hay registros aprobados pendientes de procesar en el rango especificado." });

            // Find or create PayrollRun for this period
            var run = await db.PayrollRuns.FirstOrDefaultAsync(x =>
                x.CompanyId == request.CompanyId &&
                x.PayrollPeriodId == request.PayrollPeriodId &&
                x.Status == "draft");

            if (run is null)
            {
                run = new PayrollRun
                {
                    TenantId = company.TenantId,
                    CompanyId = request.CompanyId,
                    PayrollPeriodId = request.PayrollPeriodId,
                    Folio = $"NOM-DESTAJO-{request.FromDate:yyyyMMdd}",
                    RunDate = now,
                    Status = "draft",
                    Notes = $"Destajo {request.FromDate:dd/MM/yyyy}–{request.ToDate:dd/MM/yyyy}",
                    CreatedBy = by
                };
                db.PayrollRuns.Add(run);
                await db.SaveChangesAsync();
            }

            // Find "DESTAJO" concept or fallback to first perception concept
            var destajoCode = "DESTAJO";
            var destajoConcept = await db.PayrollConcepts
                .FirstOrDefaultAsync(x => x.CompanyId == request.CompanyId && x.Code == destajoCode && x.IsActive)
                ?? await db.PayrollConcepts
                    .FirstOrDefaultAsync(x => x.CompanyId == request.CompanyId && x.ConceptType == "perception" && x.IsActive);

            // Group records by employee
            var byEmployee = records.GroupBy(r => r.EmployeeId).ToList();
            var processedCount = 0;
            var totalNetInjected = 0m;

            foreach (var empGroup in byEmployee)
            {
                var empId = empGroup.Key;
                var totalNet = empGroup.Sum(r => r.NetAmount);
                var totalGross = empGroup.Sum(r => r.GrossAmount);
                var totalDeductions = empGroup.Sum(r => r.QualityDeduction);
                var totalUnits = empGroup.Sum(r => r.UnitsProduced);

                // Find or create PayrollRunLine for this employee in this run
                var runLine = await db.PayrollRunLines
                    .FirstOrDefaultAsync(x => x.PayrollRunId == run.Id && x.EmployeeId == empId);

                if (runLine is null)
                {
                    var employee = await db.Employees.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == empId);

                    runLine = new PayrollRunLine
                    {
                        TenantId = company.TenantId,
                        CompanyId = request.CompanyId,
                        PayrollRunId = run.Id,
                        EmployeeId = empId,
                        GrossAmount = totalGross,
                        DeductionsAmount = totalDeductions,
                        NetAmount = totalNet,
                        Notes = $"Destajo {totalUnits} pzas",
                        CreatedBy = by
                    };
                    db.PayrollRunLines.Add(runLine);
                    await db.SaveChangesAsync();
                }
                else
                {
                    runLine.GrossAmount += totalGross;
                    runLine.DeductionsAmount += totalDeductions;
                    runLine.NetAmount += totalNet;
                    runLine.UpdatedAt = now;
                    runLine.UpdatedBy = by;
                }

                // Add PayrollRunLineDetail (destajo concept line)
                if (destajoConcept is not null)
                {
                    db.PayrollRunLineDetails.Add(new PayrollRunLineDetail
                    {
                        TenantId = company.TenantId,
                        CompanyId = request.CompanyId,
                        PayrollRunId = run.Id,
                        PayrollRunLineId = runLine.Id,
                        EmployeeId = empId,
                        PayrollConceptId = destajoConcept.Id,
                        ConceptCode = destajoConcept.Code,
                        ConceptName = destajoConcept.Name,
                        ConceptType = "perception",
                        TaxableType = "taxable",
                        Amount = totalGross,
                        TaxableAmount = totalGross,
                        ExemptAmount = 0m,
                        CreatedBy = by
                    });
                }

                // Mark piecework records as processed and link to period
                foreach (var rec in empGroup)
                {
                    rec.Status = "processed";
                    rec.PayrollPeriodId = request.PayrollPeriodId;
                    rec.ProcessedBy = by;
                    rec.ProcessedAt = now;
                    rec.UpdatedAt = now;
                    rec.UpdatedBy = by;
                }

                processedCount++;
                totalNetInjected += totalNet;
            }

            // Update run totals
            run.EmployeeCount = await db.PayrollRunLines.CountAsync(x => x.PayrollRunId == run.Id);
            run.GrossAmount = await db.PayrollRunLines.Where(x => x.PayrollRunId == run.Id).SumAsync(x => x.GrossAmount);
            run.DeductionsAmount = await db.PayrollRunLines.Where(x => x.PayrollRunId == run.Id).SumAsync(x => x.DeductionsAmount);
            run.NetAmount = await db.PayrollRunLines.Where(x => x.PayrollRunId == run.Id).SumAsync(x => x.NetAmount);
            run.UpdatedAt = now;
            run.UpdatedBy = by;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Registros de destajo procesados a nómina.",
                payrollRunId = run.Id,
                payrollRunFolio = run.Folio,
                employeesProcessed = processedCount,
                recordsProcessed = records.Count,
                totalNetInjected
            });
        });

        // ─── Piece work rates catalog ─────────────────────────────────────────
        g.MapGet("/rates", async (Guid? companyId, Guid? phaseId, NanchesoftDbContext db) =>
        {
            var query = db.PieceWorkRates.AsNoTracking()
                .Include(x => x.ProductionPhase)
                .Where(x => x.IsActive);

            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            if (phaseId.HasValue) query = query.Where(x => x.ProductionPhaseId == phaseId.Value);

            var rates = await query.OrderBy(x => x.ProductionPhase!.Sequence).ThenByDescending(x => x.EffectiveDate)
                .Select(x => new
                {
                    PieceWorkRateId = x.Id,
                    x.CompanyId,
                    x.ProductionPhaseId,
                    PhaseName = x.ProductionPhase != null ? x.ProductionPhase.Name : string.Empty,
                    x.EffectiveDate,
                    x.PricePerUnit,
                    x.Notes
                })
                .ToListAsync();

            return Results.Ok(rates);
        });

        g.MapPost("/rates", async (PieceWorkRateRequest request, NanchesoftDbContext db) =>
        {
            if (request.CompanyId == Guid.Empty || request.ProductionPhaseId == Guid.Empty)
                return Results.BadRequest(new { message = "CompanyId y ProductionPhaseId son obligatorios." });
            if (request.PricePerUnit <= 0)
                return Results.BadRequest(new { message = "PricePerUnit debe ser mayor a cero." });

            var company = await db.Companies.AsNoTracking()
                .Select(x => new { x.Id, x.TenantId })
                .FirstOrDefaultAsync(x => x.Id == request.CompanyId);
            if (company is null) return Results.BadRequest(new { message = "Empresa no encontrada." });

            var rate = new PieceWorkRate
            {
                TenantId = company.TenantId,
                CompanyId = request.CompanyId,
                ProductionPhaseId = request.ProductionPhaseId,
                EffectiveDate = request.EffectiveDate,
                PricePerUnit = request.PricePerUnit,
                Notes = request.Notes ?? string.Empty,
                CreatedBy = request.UserId ?? "api"
            };

            db.PieceWorkRates.Add(rate);
            await db.SaveChangesAsync();

            return Results.Created($"/api/production/piecework/rates/{rate.Id}", new { pieceWorkRateId = rate.Id });
        });
    }

    private static async Task<decimal> GetEffectivePriceAsync(
        NanchesoftDbContext db,
        Guid companyId,
        Guid phaseId,
        DateOnly workDate)
    {
        var rate = await db.PieceWorkRates.AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && x.ProductionPhaseId == phaseId
                && x.EffectiveDate <= workDate
                && x.IsActive)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync();

        return rate?.PricePerUnit ?? 0m;
    }
}

public sealed class PieceWorkRecordDto
{
    public Guid PieceWorkRecordId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionPhaseId { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public Guid? ProductionVoucherId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public DateOnly WorkDate { get; set; }
    public int UnitsProduced { get; set; }
    public int UnitsRejected { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal QualityDeduction { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public sealed class RegisterPieceWorkRequest
{
    public Guid EmployeeId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionPhaseId { get; set; }
    public Guid? ProductionVoucherId { get; set; }
    public DateOnly WorkDate { get; set; }
    public int UnitsProduced { get; set; }
    public int UnitsRejected { get; set; }
    public decimal? UnitPriceOverride { get; set; }
    public string? UserId { get; set; }
}

public sealed class BulkApproveRequest
{
    public Guid CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? UserId { get; set; }
}

public sealed class ProcessToPayrollRequest
{
    public Guid CompanyId { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? UserId { get; set; }
}

public sealed class PieceWorkRateRequest
{
    public Guid CompanyId { get; set; }
    public Guid ProductionPhaseId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public decimal PricePerUnit { get; set; }
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}
