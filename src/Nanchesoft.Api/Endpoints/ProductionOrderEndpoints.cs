using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductionOrderEndpoints
{
    public static void MapProductionOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/production/orders").WithTags("ProductionOrders");

        // ─── List ────────────────────────────────────────────────────────────
        g.MapGet("/", async (
            Guid? companyId,
            string? status,
            string? weekCode,
            int page = 1,
            int pageSize = 20,
            NanchesoftDbContext db = default!) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var query = db.ProductionOrders
                .AsNoTracking()
                .Include(x => x.Lines)
                .AsQueryable();

            if (companyId.HasValue)
                query = query.Where(x => x.CompanyId == companyId.Value);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.Status == status.Trim().ToLower());
            if (!string.IsNullOrWhiteSpace(weekCode))
                query = query.Where(x => x.WeekCode == weekCode.Trim().ToUpper());

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductionOrderSummaryDto
                {
                    ProductionOrderId = x.Id,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    Folio = x.Folio,
                    WeekCode = x.WeekCode,
                    Status = x.Status,
                    ExplosionStatus = x.ExplosionStatus,
                    Priority = x.Priority,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    DeliveryDate = x.DeliveryDate,
                    TotalUnitsPlanned = x.TotalUnitsPlanned,
                    TotalUnitsProduced = x.TotalUnitsProduced,
                    TotalUnitsShipped = x.TotalUnitsShipped,
                    LineCount = x.Lines.Count,
                    Notes = x.Notes,
                    CreatedAt = x.CreatedAt,
                    ApprovedAt = x.ApprovedAt,
                    ApprovedBy = x.ApprovedBy
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // ─── Get by id ───────────────────────────────────────────────────────
        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order is null) return Results.NotFound(new { message = "Orden de producción no encontrada." });

            var productIds = order.Lines
                .Select(l => l.FinishedProductId)
                .Distinct()
                .ToList();

            var products = await db.FinishedProducts.AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Code, x.Name })
                .ToDictionaryAsync(x => x.Id);

            var phases = await db.ProductionPhases.AsNoTracking()
                .Select(x => new { x.Id, x.Code, x.Name, x.Sequence })
                .ToListAsync();

            var progress = await db.ProductionPhaseProgress.AsNoTracking()
                .Where(x => x.ProductionOrderId == id)
                .ToListAsync();

            return Results.Ok(new ProductionOrderDetailDto
            {
                ProductionOrderId = order.Id,
                TenantId = order.TenantId,
                CompanyId = order.CompanyId,
                BranchId = order.BranchId,
                Folio = order.Folio,
                WeekCode = order.WeekCode,
                Status = order.Status,
                ExplosionStatus = order.ExplosionStatus,
                Priority = order.Priority,
                StartDate = order.StartDate,
                EndDate = order.EndDate,
                DeliveryDate = order.DeliveryDate,
                TotalUnitsPlanned = order.TotalUnitsPlanned,
                TotalUnitsProduced = order.TotalUnitsProduced,
                TotalUnitsShipped = order.TotalUnitsShipped,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                CreatedBy = order.CreatedBy,
                ApprovedAt = order.ApprovedAt,
                ApprovedBy = order.ApprovedBy,
                ClosedAt = order.ClosedAt,
                ClosedBy = order.ClosedBy,
                Lines = order.Lines.OrderBy(l => l.LineNumber).Select(l =>
                {
                    products.TryGetValue(l.FinishedProductId, out var prod);
                    return new ProductionOrderLineDto
                    {
                        ProductionOrderLineId = l.Id,
                        LineNumber = l.LineNumber,
                        FinishedProductId = l.FinishedProductId,
                        ProductCode = prod?.Code ?? string.Empty,
                        ProductName = prod?.Name ?? string.Empty,
                        QuantitiesPerSize = l.QuantitiesPerSize,
                        TotalUnitsPlanned = l.TotalUnitsPlanned,
                        TotalUnitsProduced = l.TotalUnitsProduced,
                        TotalUnitsShipped = l.TotalUnitsShipped,
                        TotalUnitsPending = l.TotalUnitsPending,
                        Status = l.Status,
                        DeliveryDate = l.DeliveryDate,
                        Priority = l.Priority
                    };
                }).ToList(),
                PhaseProgress = progress.Select(pp => new ProductionPhaseProgressDto
                {
                    ProductionPhaseProgressId = pp.Id,
                    ProductionOrderLineId = pp.ProductionOrderLineId,
                    ProductionPhaseId = pp.ProductionPhaseId,
                    PhaseName = phases.FirstOrDefault(p => p.Id == pp.ProductionPhaseId)?.Name ?? string.Empty,
                    PhaseSequence = phases.FirstOrDefault(p => p.Id == pp.ProductionPhaseId)?.Sequence ?? 0,
                    UnitsPlanned = pp.UnitsPlanned,
                    UnitsInProgress = pp.UnitsInProgress,
                    UnitsCompleted = pp.UnitsCompleted,
                    UnitsRejected = pp.UnitsRejected,
                    UnitsPending = pp.UnitsPending,
                    Status = pp.Status,
                    StartedAt = pp.StartedAt,
                    CompletedAt = pp.CompletedAt
                }).OrderBy(p => p.PhaseSequence).ToList()
            });
        });

        // ─── Create ──────────────────────────────────────────────────────────
        g.MapPost("/", async (ProductionOrderRequest request, NanchesoftDbContext db) =>
        {
            if (request.CompanyId == Guid.Empty)
                return Results.BadRequest(new { message = "CompanyId es obligatorio." });
            if (request.BranchId == Guid.Empty)
                return Results.BadRequest(new { message = "BranchId es obligatorio." });
            if (request.Lines == null || request.Lines.Count == 0)
                return Results.BadRequest(new { message = "Debe agregar al menos una línea." });
            if (string.IsNullOrWhiteSpace(request.CustomerReference))
                return Results.BadRequest(new { message = "La referencia del cliente es obligatoria." });
            if (request.Lines.Any(l => string.IsNullOrWhiteSpace(l.CustomerPoReference)))
                return Results.BadRequest(new { message = "Todas las líneas deben tener el campo P.O. (pedido del cliente)." });

            var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId);
            if (company is null) return Results.BadRequest(new { message = "Empresa no encontrada." });

            var folio = await GenerateProductionFolioAsync(db, company.TenantId, request.CompanyId, "PRODUCTION_ORDER", "OP");

            var order = new ProductionOrder
            {
                TenantId = company.TenantId,
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                Folio = folio,
                WeekCode = (request.WeekCode ?? string.Empty).Trim().ToUpper(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                DeliveryDate = request.DeliveryDate,
                CancellationDate = request.CancellationDate,
                Priority = request.Priority < 1 ? 1 : request.Priority,
                Notes = request.Notes,
                CustomerId = request.CustomerId == Guid.Empty ? null : request.CustomerId,
                CustomerReference = request.CustomerReference.Trim(),
                WarehouseId = request.WarehouseId == Guid.Empty ? null : request.WarehouseId,
                ShipToAddressId = request.ShipToAddressId == Guid.Empty ? null : request.ShipToAddressId,
                ShipToAddressText = request.ShipToAddressText?.Trim() ?? string.Empty,
                LegalName = request.LegalName?.Trim() ?? string.Empty,
                Status = "draft",
                ExplosionStatus = "pending",
                CreatedBy = request.UserId ?? "api"
            };

            var lineNumber = 1;
            foreach (var lineReq in request.Lines)
            {
                // Sanitize quantities — skip entries with invalid (empty) size run size IDs
                var validQty = lineReq.QuantitiesPerSize
                    .Where(kv => Guid.TryParse(kv.Key, out var g) && g != Guid.Empty && kv.Value > 0)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                var totalPlanned = validQty.Values.Sum();
                var lineTotal = Math.Round(totalPlanned * lineReq.UnitPrice * (1 - lineReq.DiscountPercent / 100m), 2);

                var ncFolio = await GenerateNcFolioAsync(db, request.CompanyId);

                order.Lines.Add(new ProductionOrderLine
                {
                    LineNumber = lineNumber++,
                    NcFolio = ncFolio,
                    FinishedProductId = lineReq.FinishedProductId,
                    ProductStyleId = lineReq.ProductStyleId,
                    ProductSizeRunId = lineReq.ProductSizeRunId,
                    ProductLastId = lineReq.ProductLastId,
                    ProductColorId = lineReq.ProductColorId,
                    ProductSoleId = lineReq.ProductSoleId,
                    ProductManufacturingTypeId = lineReq.ProductManufacturingTypeId,
                    CustomerId = lineReq.CustomerId == Guid.Empty ? null : lineReq.CustomerId,
                    SalesOrderId = lineReq.SalesOrderId,
                    SalesOrderLineId = lineReq.SalesOrderLineId,
                    QuantitiesPerSize = validQty,
                    TotalUnitsPlanned = totalPlanned,
                    TotalUnitsPending = totalPlanned,
                    CustomerPoReference = (lineReq.CustomerPoReference ?? string.Empty).Trim(),
                    UnitPrice = lineReq.UnitPrice,
                    DiscountPercent = lineReq.DiscountPercent,
                    LineTotal = lineTotal,
                    DeliveryDate = lineReq.DeliveryDate,
                    Priority = lineReq.Priority < 1 ? 1 : lineReq.Priority,
                    Status = "pending",
                    Notes = lineReq.Notes ?? string.Empty,
                    WarehouseId = lineReq.WarehouseId == Guid.Empty ? null : lineReq.WarehouseId,
                    ShipToAddressId = lineReq.ShipToAddressId == Guid.Empty ? null : lineReq.ShipToAddressId,
                    ShipToAddressText = lineReq.ShipToAddressText?.Trim() ?? string.Empty,
                    LegalName = lineReq.LegalName?.Trim() ?? string.Empty,
                    CreatedBy = request.UserId ?? "api"
                });
                order.TotalUnitsPlanned += totalPlanned;
                order.Subtotal += lineTotal;
            }

            order.Total = order.Subtotal - order.DiscountAmount;

            db.ProductionOrders.Add(order);
            await db.SaveChangesAsync();

            // Return created lines with their NC folios for the print view
            var linesResult = order.Lines.OrderBy(l => l.LineNumber).Select(l => new
            {
                l.Id,
                l.LineNumber,
                l.NcFolio,
                l.FinishedProductId,
                l.CustomerPoReference,
                l.UnitPrice,
                l.DiscountPercent,
                l.TotalUnitsPlanned,
                l.LineTotal
            }).ToList();

            return Results.Created($"/api/production/orders/{order.Id}", new
            {
                productionOrderId = order.Id,
                folio = order.Folio,
                subtotal = order.Subtotal,
                total = order.Total,
                lines = linesResult
            });
        });

        // ─── Update header ───────────────────────────────────────────────────
        g.MapPut("/{id:guid}", async (Guid id, ProductionOrderUpdateRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden de producción no encontrada." });
            if (order.Status != "draft" && order.Status != "planned")
                return Results.BadRequest(new { message = $"No se puede editar una orden en estado '{order.Status}'." });

            order.WeekCode = (request.WeekCode ?? order.WeekCode).Trim().ToUpper();
            order.StartDate = request.StartDate != default ? request.StartDate : order.StartDate;
            order.EndDate = request.EndDate != default ? request.EndDate : order.EndDate;
            order.DeliveryDate = request.DeliveryDate != default ? request.DeliveryDate : order.DeliveryDate;
            order.Priority = request.Priority > 0 ? request.Priority : order.Priority;
            order.Notes = request.Notes ?? order.Notes;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Orden actualizada." });
        });

        // ─── Status transitions ──────────────────────────────────────────────

        // Confirm: draft → planned
        g.MapPost("/{id:guid}/confirm", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "draft")
                return Results.BadRequest(new { message = $"Solo se pueden confirmar órdenes en estado 'draft'. Estado actual: '{order.Status}'." });
            if (!order.Lines.Any())
                return Results.BadRequest(new { message = "La orden no tiene líneas." });

            order.Status = "planned";
            order.ApprovedAt = DateTime.UtcNow;
            order.ApprovedBy = request.UserId ?? "api";
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Orden confirmada (draft → planned).", status = order.Status });
        });

        // Explode: planned → exploded (triggers material requirements calculation)
        g.MapPost("/{id:guid}/explode", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "planned")
                return Results.BadRequest(new { message = $"Solo se pueden explotar órdenes en estado 'planned'. Estado actual: '{order.Status}'." });

            var by = request.UserId ?? "api";
            var now = DateTime.UtcNow;

            // Cancel previous requirement for this order if any
            var existing = await db.MaterialRequirements
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.ProductionOrderId == id && x.Status != "cancelled");

            if (existing is not null)
            {
                existing.Status = "cancelled";
                existing.UpdatedAt = now;
                existing.UpdatedBy = by;
            }

            // Pre-load all stock balances for this company to avoid N+1 per material
            var stockByItem = await db.StockBalances.AsNoTracking()
                .Where(x => x.CompanyId == order.CompanyId && x.IsActive)
                .GroupBy(x => x.ItemId)
                .Select(g => new { ItemId = g.Key, OnHand = g.Sum(s => s.QuantityAvailable) })
                .ToDictionaryAsync(x => x.ItemId, x => x.OnHand);

            var requirement = new MaterialRequirement
            {
                TenantId = order.TenantId,
                CompanyId = order.CompanyId,
                ProductionOrderId = order.Id,
                CalculatedAt = now,
                CalculatedBy = by,
                Status = "draft",
                CreatedBy = by
            };

            var lineCount = 0;
            var linesWithShortage = 0;
            var linesFull = 0;
            var linesWithoutTemplate = new List<object>();

            foreach (var line in order.Lines.Where(l => l.IsActive))
            {
                var template = await db.ConsumptionTemplates.AsNoTracking()
                    .Include(x => x.Details).ThenInclude(d => d.Sizes)
                    .Include(x => x.Details).ThenInclude(d => d.ProductComponent)
                    .FirstOrDefaultAsync(x => x.CompanyId == order.CompanyId
                        && x.ProductStyleId == line.ProductStyleId
                        && x.ProductSizeRunId == line.ProductSizeRunId
                        && x.IsActive && x.IsAuthorized);

                if (template is null)
                {
                    linesWithoutTemplate.Add(new
                    {
                        lineNumber = line.LineNumber,
                        productionOrderLineId = line.Id,
                        styleId = line.ProductStyleId,
                        sizeRunId = line.ProductSizeRunId,
                        warning = "Sin plantilla de consumo activa y autorizada para el estilo/corrida de esta línea."
                    });
                    continue;
                }

                var supplies = await db.FinishedProductSupplies.AsNoTracking()
                    .Include(x => x.ProductComponent)
                    .Include(x => x.Sizes).ThenInclude(s => s.MaterialItem)
                    .Where(x => x.FinishedProductId == line.FinishedProductId && x.IsActive && x.IsAuthorized)
                    .ToListAsync();

                var sizeRunSizes = await db.ProductSizeRunSizes.AsNoTracking()
                    .Where(x => x.ProductSizeRunId == line.ProductSizeRunId && x.IsActive)
                    .ToListAsync();

                foreach (var detail in template.Details.Where(d => d.IsActive))
                {
                    var component = detail.ProductComponent;
                    if (component is null) continue;

                    var supply = supplies.FirstOrDefault(s => s.ProductComponentId == detail.ProductComponentId);
                    var repSize = supply?.Sizes.FirstOrDefault(s => s.MaterialItem is not null);
                    var material = repSize?.MaterialItem;

                    decimal totalRequired = 0;
                    foreach (var srSize in sizeRunSizes)
                    {
                        if (!line.QuantitiesPerSize.TryGetValue(srSize.Id.ToString(), out var qty) || qty <= 0)
                            continue;
                        var tSize = detail.Sizes.FirstOrDefault(s => s.ProductSizeRunSizeId == srSize.Id);
                        if (tSize is null) continue;
                        totalRequired += tSize.Consumption * qty;
                    }

                    if (totalRequired <= 0) continue;

                    var onHand = material is not null && stockByItem.TryGetValue(material.Id, out var s) ? s : 0m;
                    var coverage = totalRequired <= onHand ? "covered"
                        : onHand > 0 ? "partial"
                        : "shortage";

                    if (coverage == "shortage") linesWithShortage++;
                    else if (coverage == "covered") linesFull++;

                    lineCount++;

                    requirement.Lines.Add(new MaterialRequirementLine
                    {
                        ProductionOrderLineId = line.Id,
                        ProductComponentId = detail.ProductComponentId,
                        MaterialItemId = material?.Id,
                        ComponentCode = component.Code,
                        ComponentName = component.Name,
                        MaterialName = material?.Name ?? string.Empty,
                        QuantityRequired = totalRequired,
                        QuantityOnHand = (decimal)onHand,
                        QuantityToReserve = Math.Min(totalRequired, (decimal)onHand),
                        QuantityShortage = Math.Max(0, totalRequired - (decimal)onHand),
                        UnitCost = material?.AuthorizedCost ?? 0,
                        TotalCost = totalRequired * (material?.AuthorizedCost ?? 0),
                        CoverageStatus = coverage,
                        CreatedBy = by
                    });
                }
            }

            requirement.TotalLines = lineCount;
            requirement.LinesWithShortage = linesWithShortage;
            requirement.LinesFulyCovered = linesFull;
            requirement.Status = linesWithShortage > 0 ? "with_shortages" : "confirmed";

            db.MaterialRequirements.Add(requirement);

            order.Status = "exploded";
            order.ExplosionStatus = linesWithoutTemplate.Count > 0 && lineCount == 0
                ? "no_templates"
                : linesWithShortage > 0 ? "with_shortages" : "complete";
            order.UpdatedAt = now;
            order.UpdatedBy = by;

            await db.SaveChangesAsync();

            var hasWarnings = linesWithoutTemplate.Count > 0;
            return Results.Ok(new
            {
                message = hasWarnings
                    ? $"Explosión calculada con {linesWithoutTemplate.Count} línea(s) sin plantilla de consumo."
                    : "Explosión de materiales calculada.",
                status = order.Status,
                explosionStatus = order.ExplosionStatus,
                materialRequirementId = requirement.Id,
                totalLines = lineCount,
                linesWithShortage,
                linesFull,
                linesWithoutTemplate = linesWithoutTemplate.Count,
                warnings = linesWithoutTemplate
            });
        });

        // Reserve: exploded → reserved
        g.MapPost("/{id:guid}/reserve", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "exploded")
                return Results.BadRequest(new { message = $"Solo se pueden reservar órdenes en estado 'exploded'. Estado actual: '{order.Status}'." });

            var by = request.UserId ?? "api";
            var now = DateTime.UtcNow;

            // Mark materials as reserved in the requirement
            var req = await db.MaterialRequirements
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.ProductionOrderId == id && x.Status != "cancelled");

            if (req is not null)
            {
                foreach (var rl in req.Lines.Where(l => l.CoverageStatus == "covered" || l.CoverageStatus == "partial"))
                {
                    rl.ReservedAt = now;
                    rl.ReservedBy = by;
                    rl.UpdatedAt = now;
                    rl.UpdatedBy = by;
                }
                req.Status = "reserved";
                req.UpdatedAt = now;
                req.UpdatedBy = by;
            }

            order.Status = "reserved";
            order.UpdatedAt = now;
            order.UpdatedBy = by;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Materiales reservados.", status = order.Status });
        });

        // Start: reserved/planned → in_progress
        g.MapPost("/{id:guid}/start", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "reserved" && order.Status != "planned" && order.Status != "exploded")
                return Results.BadRequest(new { message = $"No se puede iniciar una orden en estado '{order.Status}'." });

            var by = request.UserId ?? "api";
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            order.Status = "in_progress";
            order.UpdatedAt = now;
            order.UpdatedBy = by;

            // Initialise phase progress for all lines if not already present
            var phases = await db.ProductionPhases.AsNoTracking()
                .Where(x => x.TenantId == order.TenantId && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync();

            foreach (var line in order.Lines.Where(l => l.IsActive))
            {
                line.Status = "in_progress";
                foreach (var phase in phases)
                {
                    var alreadyExists = await db.ProductionPhaseProgress.AnyAsync(x =>
                        x.ProductionOrderId == order.Id &&
                        x.ProductionOrderLineId == line.Id &&
                        x.ProductionPhaseId == phase.Id);

                    if (alreadyExists) continue;

                    db.ProductionPhaseProgress.Add(new ProductionPhaseProgress
                    {
                        ProductionOrderId = order.Id,
                        ProductionOrderLineId = line.Id,
                        ProductionPhaseId = phase.Id,
                        UnitsPlanned = line.TotalUnitsPlanned,
                        UnitsPending = line.TotalUnitsPlanned,
                        Status = "pending",
                        LastUpdatedAt = now,
                        CreatedBy = by
                    });
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Orden iniciada.", status = order.Status });
        });

        // Complete: in_progress → completed
        g.MapPost("/{id:guid}/complete", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "in_progress")
                return Results.BadRequest(new { message = $"Solo se pueden completar órdenes en estado 'in_progress'. Estado actual: '{order.Status}'." });

            var by = request.UserId ?? "api";
            var now = DateTime.UtcNow;

            order.Status = "completed";
            order.UpdatedAt = now;
            order.UpdatedBy = by;

            foreach (var line in order.Lines.Where(l => l.IsActive && l.Status == "in_progress"))
                line.Status = "completed";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Orden completada.", status = order.Status });
        });

        // Close: completed → closed
        g.MapPost("/{id:guid}/close", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "completed")
                return Results.BadRequest(new { message = $"Solo se pueden cerrar órdenes en estado 'completed'. Estado actual: '{order.Status}'." });

            order.Status = "closed";
            order.ClosedAt = DateTime.UtcNow;
            order.ClosedBy = request.UserId ?? "api";
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = request.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Orden cerrada.", status = order.Status });
        });

        // Cancel
        g.MapPost("/{id:guid}/cancel", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status == "closed" || order.Status == "cancelled")
                return Results.BadRequest(new { message = $"No se puede cancelar una orden en estado '{order.Status}'." });

            var by = request.UserId ?? "api";
            var now = DateTime.UtcNow;

            order.Status = "cancelled";
            order.Notes = string.IsNullOrWhiteSpace(request.Reason)
                ? order.Notes
                : $"{order.Notes}\nCANCELACIÓN: {request.Reason}";
            order.UpdatedAt = now;
            order.UpdatedBy = by;

            foreach (var line in order.Lines.Where(l => l.IsActive))
                line.Status = "cancelled";

            // Cancel active material requirement
            var req = await db.MaterialRequirements
                .FirstOrDefaultAsync(x => x.ProductionOrderId == id && x.Status != "cancelled");
            if (req is not null)
            {
                req.Status = "cancelled";
                req.UpdatedAt = now;
                req.UpdatedBy = by;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Orden cancelada.", status = order.Status });
        });

        // Reschedule: in_progress → planned
        g.MapPost("/{id:guid}/reschedule", async (Guid id, ProductionOrderActionRequest request, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "in_progress" && order.Status != "reserved")
                return Results.BadRequest(new { message = $"Solo se pueden reprogramar órdenes en estado 'in_progress' o 'reserved'." });

            var by = request.UserId ?? "api";
            var now = DateTime.UtcNow;

            order.Status = "planned";
            if (!string.IsNullOrWhiteSpace(request.NewWeekCode))
                order.WeekCode = request.NewWeekCode.Trim().ToUpper();
            if (request.NewStartDate.HasValue) order.StartDate = request.NewStartDate.Value;
            if (request.NewEndDate.HasValue) order.EndDate = request.NewEndDate.Value;
            order.Notes = string.IsNullOrWhiteSpace(request.Reason)
                ? order.Notes
                : $"{order.Notes}\nREPROGRAMACIÓN: {request.Reason}";
            order.UpdatedAt = now;
            order.UpdatedBy = by;

            foreach (var line in order.Lines.Where(l => l.IsActive && l.Status == "in_progress"))
                line.Status = "pending";

            var progressItems = await db.ProductionPhaseProgress
                .Where(x => x.ProductionOrderId == id && x.Status == "pending")
                .ToListAsync();
            foreach (var pp in progressItems)
            {
                pp.RescheduledCount++;
                pp.LastRescheduleReason = request.Reason;
                pp.LastUpdatedAt = now;
                pp.UpdatedAt = now;
                pp.UpdatedBy = by;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Orden reprogramada.", status = order.Status });
        });

        // ─── Add line to existing order ──────────────────────────────────────
        g.MapPost("/{id:guid}/lines", async (Guid id, ProductionOrderLineRequest lineReq, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "draft" && order.Status != "planned")
                return Results.BadRequest(new { message = $"No se pueden agregar líneas a una orden en estado '{order.Status}'." });

            var nextLine = order.Lines.Any() ? order.Lines.Max(l => l.LineNumber) + 1 : 1;
            var totalPlanned = lineReq.QuantitiesPerSize.Values.Sum();

            var line = new ProductionOrderLine
            {
                ProductionOrderId = order.Id,
                LineNumber = nextLine,
                FinishedProductId = lineReq.FinishedProductId,
                ProductStyleId = lineReq.ProductStyleId,
                ProductSizeRunId = lineReq.ProductSizeRunId,
                ProductLastId = lineReq.ProductLastId,
                ProductColorId = lineReq.ProductColorId,
                ProductSoleId = lineReq.ProductSoleId,
                ProductManufacturingTypeId = lineReq.ProductManufacturingTypeId,
                CustomerId = lineReq.CustomerId,
                SalesOrderId = lineReq.SalesOrderId,
                SalesOrderLineId = lineReq.SalesOrderLineId,
                QuantitiesPerSize = lineReq.QuantitiesPerSize,
                TotalUnitsPlanned = totalPlanned,
                TotalUnitsPending = totalPlanned,
                DeliveryDate = lineReq.DeliveryDate,
                Priority = lineReq.Priority < 1 ? 1 : lineReq.Priority,
                Status = "pending",
                CreatedBy = lineReq.UserId ?? "api"
            };

            order.Lines.Add(line);
            order.TotalUnitsPlanned += totalPlanned;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = lineReq.UserId ?? "api";

            await db.SaveChangesAsync();
            return Results.Created($"/api/production/orders/{id}/lines/{line.Id}", new { productionOrderLineId = line.Id, lineNumber = line.LineNumber });
        });

        // ─── Delete line ─────────────────────────────────────────────────────
        g.MapDelete("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, NanchesoftDbContext db) =>
        {
            var order = await db.ProductionOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (order is null) return Results.NotFound(new { message = "Orden no encontrada." });
            if (order.Status != "draft" && order.Status != "planned")
                return Results.BadRequest(new { message = "Solo se pueden eliminar líneas de órdenes en estado draft o planned." });

            var line = order.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null) return Results.NotFound(new { message = "Línea no encontrada." });

            order.TotalUnitsPlanned -= line.TotalUnitsPlanned;
            order.Lines.Remove(line);
            db.ProductionOrderLines.Remove(line);
            order.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Línea eliminada." });
        });

        // ─── Catalog helpers ─────────────────────────────────────────────────
        app.MapGet("/api/production/phases", async (Guid? tenantId, NanchesoftDbContext db) =>
        {
            var query = db.ProductionPhases.AsNoTracking().Where(x => x.IsActive);
            if (tenantId.HasValue)
                query = query.Where(x => x.TenantId == tenantId.Value);
            var list = await query.OrderBy(x => x.Sequence)
                .Select(x => new { x.Id, x.Code, x.Name, x.Description, x.Sequence })
                .ToListAsync();
            return Results.Ok(list);
        }).WithTags("ProductionOrders");

        app.MapGet("/api/production/cells", async (Guid? companyId, Guid? phaseId, NanchesoftDbContext db) =>
        {
            var query = db.ProductionCells.AsNoTracking().Where(x => x.IsActive);
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            if (phaseId.HasValue) query = query.Where(x => x.ProductionPhaseId == phaseId.Value);
            var list = await query.OrderBy(x => x.Code)
                .Select(x => new { x.Id, x.Code, x.Name, x.ProductionPhaseId, x.BranchId, x.CapacityPerDay, x.CapacityPerWeek })
                .ToListAsync();
            return Results.Ok(list);
        }).WithTags("ProductionOrders");

        // ─── Price lookup: customer price list → finished product (by code match) ─
        app.MapGet("/api/production/product-price", async (Guid finishedProductId, Guid customerId, NanchesoftDbContext db) =>
        {
            var customer = await db.Customers.AsNoTracking()
                .Where(x => x.Id == customerId)
                .Select(x => new { x.PriceListId })
                .FirstOrDefaultAsync();

            if (customer?.PriceListId == null)
                return Results.Ok(new { price = 0m, found = false });

            var product = await db.FinishedProducts.AsNoTracking()
                .Where(x => x.Id == finishedProductId)
                .Select(x => new { x.Code, x.Name })
                .FirstOrDefaultAsync();

            if (product is null)
                return Results.Ok(new { price = 0m, found = false });

            // Try to find by matching item code to product code
            var priceDetail = await db.ItemPriceListDetails.AsNoTracking()
                .Include(x => x.Item)
                .Where(x => x.PriceListId == customer.PriceListId.Value
                    && x.Item != null
                    && (x.Item.Code == product.Code || x.Item.Name == product.Name))
                .Select(x => new { x.Price })
                .FirstOrDefaultAsync();

            if (priceDetail is not null)
                return Results.Ok(new { price = priceDetail.Price, found = true });

            return Results.Ok(new { price = 0m, found = false });
        }).WithTags("ProductionOrders");

        // ─── Size run sizes for a finished product ───────────────────────────
        app.MapGet("/api/production/product-sizes/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var product = await db.FinishedProducts.AsNoTracking()
                .Where(x => x.Id == finishedProductId)
                .Select(x => new { x.ProductSizeRunId, x.ProductStyleId })
                .FirstOrDefaultAsync();

            if (product?.ProductSizeRunId == null)
                return Results.Ok(new List<object>());

            var sizes = await db.ProductSizeRunSizes.AsNoTracking()
                .Where(x => x.ProductSizeRunId == product.ProductSizeRunId.Value && x.IsActive)
                .OrderBy(x => x.Sequence)
                .Select(x => new { sizeRunSizeId = x.Id, sizeCode = x.SizeCode, sequence = x.Sequence })
                .ToListAsync();

            return Results.Ok(new
            {
                productSizeRunId = product.ProductSizeRunId,
                productStyleId = product.ProductStyleId,
                sizes
            });
        }).WithTags("ProductionOrders");

        // ─── Customer shipping addresses / locations ──────────────────────────
        app.MapGet("/api/production/customer-addresses/{customerId:guid}", async (Guid customerId, NanchesoftDbContext db) =>
        {
            var rows = await db.ThirdPartyAddresses.AsNoTracking()
                .Where(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == customerId && x.IsActive)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.LocationName).ThenBy(x => x.Street)
                .Select(x => new
                {
                    AddressId = x.Id,
                    LocationName = x.LocationName,
                    AddressType = x.AddressType,
                    Street = x.Street,
                    ExteriorNumber = x.ExteriorNumber,
                    Neighborhood = x.Neighborhood,
                    ZipCode = x.ZipCode,
                    IsPrimary = x.IsPrimary
                })
                .ToListAsync();

            var result = rows.Select(x => new
            {
                x.AddressId,
                x.IsPrimary,
                FullAddress = (!string.IsNullOrWhiteSpace(x.LocationName) ? x.LocationName + " — " : "") +
                              string.Join(", ", new[] { (x.Street + " " + x.ExteriorNumber).Trim(), x.Neighborhood, x.ZipCode }
                                  .Where(s => !string.IsNullOrWhiteSpace(s)))
            }).ToList();

            return Results.Ok(result);
        }).WithTags("ProductionOrders");

        // ─── Material requirement for an order ───────────────────────────────
        app.MapGet("/api/production/orders/{id:guid}/material-requirement", async (Guid id, NanchesoftDbContext db) =>
        {
            var req = await db.MaterialRequirements.AsNoTracking()
                .Include(x => x.Lines)
                .Where(x => x.ProductionOrderId == id && x.Status != "cancelled")
                .OrderByDescending(x => x.CalculatedAt)
                .FirstOrDefaultAsync();

            if (req is null) return Results.NotFound(new { message = "No hay explosión registrada para esta orden." });

            return Results.Ok(new
            {
                materialRequirementId = req.Id,
                status = req.Status,
                calculatedAt = req.CalculatedAt,
                totalLines = req.TotalLines,
                linesWithShortage = req.LinesWithShortage,
                linesFulyCovered = req.LinesFulyCovered,
                lines = req.Lines.OrderBy(l => l.ComponentCode).Select(l => new
                {
                    l.ComponentCode,
                    l.ComponentName,
                    l.MaterialName,
                    l.QuantityRequired,
                    l.QuantityOnHand,
                    l.QuantityToReserve,
                    l.QuantityShortage,
                    l.UnitCost,
                    l.TotalCost,
                    l.CoverageStatus
                }).ToList()
            });
        }).WithTags("ProductionOrders");
    }

    private static async Task<string> GenerateProductionFolioAsync(
        NanchesoftDbContext db,
        Guid tenantId,
        Guid companyId,
        string documentType,
        string fallbackPrefix)
    {
        var series = await db.DocumentSeries
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.DocumentType == documentType && x.IsDefault && x.IsActive);

        if (series is null)
            return $"{fallbackPrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var folio = await db.DocumentFolios
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.DocumentType == documentType && x.SeriesId == series.Id);

        var number = folio?.CurrentNumber ?? series.CurrentNumber;
        var formatted = $"{series.Prefix}{number.ToString().PadLeft(series.NumberLength, '0')}";

        if (folio is not null)
        {
            folio.CurrentNumber = number + 1;
            folio.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            series.CurrentNumber = number + 1;
            series.UpdatedAt = DateTime.UtcNow;
        }

        return formatted;
    }

    // NC = Número de Control — sequential, unique per company across all order lines
    private static async Task<string> GenerateNcFolioAsync(NanchesoftDbContext db, Guid companyId)
    {
        var series = await db.DocumentSeries
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.DocumentType == "PRODUCTION_ORDER_LINE_NC" && x.IsDefault && x.IsActive);

        if (series is null)
        {
            // Auto-create a default NC series for this company
            var maxNc = await db.ProductionOrderLines
                .Where(l => l.ProductionOrder != null && l.ProductionOrder.CompanyId == companyId && l.NcFolio.StartsWith("NC-"))
                .Select(l => l.NcFolio)
                .ToListAsync();

            var nextNum = maxNc.Count == 0 ? 1
                : maxNc.Select(nc => int.TryParse(nc.Replace("NC-", ""), out var n) ? n : 0).Max() + 1;

            return $"NC-{nextNum:D6}";
        }

        var folio = await db.DocumentFolios
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.DocumentType == "PRODUCTION_ORDER_LINE_NC" && x.SeriesId == series.Id);

        var number = folio?.CurrentNumber ?? series.CurrentNumber;
        var formatted = $"{series.Prefix}{number.ToString().PadLeft(series.NumberLength, '0')}";

        if (folio is not null)
        {
            folio.CurrentNumber = number + 1;
            folio.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            series.CurrentNumber = number + 1;
            series.UpdatedAt = DateTime.UtcNow;
        }

        return formatted;
    }
}

// ─── DTOs ────────────────────────────────────────────────────────────────────

public sealed class ProductionOrderRequest
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public string WeekCode { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public DateOnly? CancellationDate { get; set; }
    public int Priority { get; set; } = 1;
    public string? Notes { get; set; }
    public string? UserId { get; set; }
    // Shipping & identity
    public Guid? ShipToAddressId { get; set; }
    public string ShipToAddressText { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public List<ProductionOrderLineRequest> Lines { get; set; } = new();
}

public sealed class ProductionOrderUpdateRequest
{
    public string? WeekCode { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public int Priority { get; set; }
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}

public sealed class ProductionOrderLineRequest
{
    public Guid FinishedProductId { get; set; }
    public Guid? ProductStyleId { get; set; }
    public Guid? ProductSizeRunId { get; set; }
    public Guid? ProductLastId { get; set; }
    public Guid? ProductColorId { get; set; }
    public Guid? ProductSoleId { get; set; }
    public Guid? ProductManufacturingTypeId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? SalesOrderLineId { get; set; }
    public Dictionary<string, int> QuantitiesPerSize { get; set; } = new();
    public DateOnly? DeliveryDate { get; set; }
    public int Priority { get; set; } = 1;
    public string? UserId { get; set; }
    public string? CustomerPoReference { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? Notes { get; set; }
    // Per-line shipping & identity overrides
    public Guid? WarehouseId { get; set; }
    public Guid? ShipToAddressId { get; set; }
    public string ShipToAddressText { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
}

public sealed class ProductionOrderActionRequest
{
    public string? UserId { get; set; }
    public string? Reason { get; set; }
    public string? NewWeekCode { get; set; }
    public DateOnly? NewStartDate { get; set; }
    public DateOnly? NewEndDate { get; set; }
}

public sealed class ProductionOrderSummaryDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string WeekCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ExplosionStatus { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public int TotalUnitsPlanned { get; set; }
    public int TotalUnitsProduced { get; set; }
    public int TotalUnitsShipped { get; set; }
    public int LineCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
}

public sealed class ProductionOrderDetailDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string WeekCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ExplosionStatus { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public int TotalUnitsPlanned { get; set; }
    public int TotalUnitsProduced { get; set; }
    public int TotalUnitsShipped { get; set; }
    public int LineCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public List<ProductionOrderLineDto> Lines { get; set; } = new();
    public List<ProductionPhaseProgressDto> PhaseProgress { get; set; } = new();
}

public sealed class ProductionOrderLineDto
{
    public Guid ProductionOrderLineId { get; set; }
    public int LineNumber { get; set; }
    public Guid FinishedProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Dictionary<string, int> QuantitiesPerSize { get; set; } = new();
    public int TotalUnitsPlanned { get; set; }
    public int TotalUnitsProduced { get; set; }
    public int TotalUnitsShipped { get; set; }
    public int TotalUnitsPending { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? DeliveryDate { get; set; }
    public int Priority { get; set; }
}

public sealed class ProductionPhaseProgressDto
{
    public Guid ProductionPhaseProgressId { get; set; }
    public Guid ProductionOrderLineId { get; set; }
    public Guid ProductionPhaseId { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public int PhaseSequence { get; set; }
    public int UnitsPlanned { get; set; }
    public int UnitsInProgress { get; set; }
    public int UnitsCompleted { get; set; }
    public int UnitsRejected { get; set; }
    public int UnitsPending { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
