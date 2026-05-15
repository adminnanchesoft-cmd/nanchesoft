using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var stock = app.MapGroup("/api/inventory/stock-balances").WithTags("InventoryStock");
        stock.MapGet("/", GetStockBalancesAsync);
        stock.MapGet("/by-item/{itemId:guid}", GetStockByItemAsync);

        var kardex = app.MapGroup("/api/inventory/kardex").WithTags("InventoryKardex");
        kardex.MapGet("/", GetKardexAsync);
        kardex.MapGet("/by-item/{itemId:guid}", GetKardexByItemAsync);

        MapEntryEndpoints(app);
        MapExitEndpoints(app);
        MapTransferEndpoints(app);
        MapAdjustmentEndpoints(app);
        MapPhysicalCountEndpoints(app);

        var lots = app.MapGroup("/api/inventory/lots").WithTags("InventoryLots");
        lots.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.ItemLots.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    ItemLotId = x.Id,
                    x.CompanyId,
                    x.ItemId,
                    x.WarehouseId,
                    x.LotNumber,
                    x.Status,
                    x.QuantityOnHand,
                    x.IsActive
                })
                .ToListAsync()));

        var serials = app.MapGroup("/api/inventory/serials").WithTags("InventorySerials");
        serials.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.ItemSerials.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    ItemSerialId = x.Id,
                    x.CompanyId,
                    x.ItemId,
                    x.WarehouseId,
                    x.SerialNumber,
                    x.Status,
                    x.DocumentType,
                    x.DocumentId,
                    x.IsActive
                })
                .ToListAsync()));

        var dashboard = app.MapGroup("/api/inventory/dashboard").WithTags("InventoryDashboard");
        dashboard.MapGet("/summary", async (NanchesoftDbContext db) => Results.Ok(new
        {
            totalStockRows = await db.StockBalances.CountAsync(),
            totalOnHand = await db.StockBalances.SumAsync(x => x.QuantityOnHand),
            totalAvailable = await db.StockBalances.SumAsync(x => x.QuantityAvailable),
            totalMovements = await db.InventoryMovements.CountAsync(),
            activeLots = await db.ItemLots.CountAsync(x => x.IsActive),
            activeSerials = await db.ItemSerials.CountAsync(x => x.IsActive)
        }));

        var lookups = app.MapGroup("/api/inventory/lookups").WithTags("InventoryLookups");
        lookups.MapGet("/", GetLookupsAsync);

        return app;
    }

    private static void MapEntryEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/entries").WithTags("InventoryEntries");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.InventoryEntries.AsNoTracking()
                .OrderByDescending(x => x.EntryDate)
                .Select(x => new
                {
                    InventoryEntryId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.WarehouseId,
                    x.Folio,
                    x.EntryDate,
                    x.Status,
                    x.Reason,
                    x.Notes,
                    x.ApprovedAt,
                    x.PostedAt,
                    x.IsActive
                })
                .ToListAsync()));

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.InventoryEntries.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la entrada." });
            }

            var lineLookups = await BuildInventoryLineLookupsAsync(db, entity.Lines.Select(x => x.ItemId), entity.Lines.Select(x => x.UnitId), [], []);

            return Results.Ok(new InventoryDocumentRequest
            {
                InventoryEntryId = entity.Id,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                WarehouseId = entity.WarehouseId,
                Folio = entity.Folio,
                DocumentDate = entity.EntryDate,
                Status = entity.Status,
                Reason = entity.Reason,
                Notes = entity.Notes,
                ApprovedAt = entity.ApprovedAt,
                PostedAt = entity.PostedAt,
                IsActive = entity.IsActive,
                Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new InventoryLineRequest
                {
                    Id = x.Id,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    UnitId = x.UnitId,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitCost = x.UnitCost,
                    LineTotal = x.LineTotal,
                    ItemCode = lineLookups.Items.TryGetValue(x.ItemId, out var item) ? item.Code : string.Empty,
                    ItemName = lineLookups.Items.TryGetValue(x.ItemId, out item) ? item.Name : string.Empty,
                    UnitName = x.UnitId.HasValue && lineLookups.Units.TryGetValue(x.UnitId.Value, out var unit) ? unit.Name : string.Empty
                }).ToList()
            });
        });

        group.MapPost("/", async (InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetCompanyAsync(db, request.CompanyId);
            var branch = await GetBranchAsync(db, company.Id, request.BranchId);
            var warehouse = await GetWarehouseAsync(db, company.Id, request.WarehouseId);

            var entity = new InventoryEntry
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                Folio = NormalizeFolio(request.Folio, "ENT"),
                EntryDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                Status = NormalizeStatus(request.Status),
                Reason = (request.Reason ?? string.Empty).Trim(),
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyEntryLines(entity, request.Lines);
            db.InventoryEntries.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.InventoryEntries.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la entrada." });
            }

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.WarehouseId = request.WarehouseId ?? entity.WarehouseId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.EntryDate = request.DocumentDate?.Date ?? entity.EntryDate;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.Reason = request.Reason?.Trim() ?? entity.Reason;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyEntryLines(entity, request.Lines);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntity<InventoryEntry>(id, db, "No se encontró la entrada."));
    }

    private static void MapExitEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/exits").WithTags("InventoryExits");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.InventoryExits.AsNoTracking()
                .OrderByDescending(x => x.ExitDate)
                .Select(x => new
                {
                    InventoryExitId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.WarehouseId,
                    x.Folio,
                    x.ExitDate,
                    x.Status,
                    x.Reason,
                    x.Notes,
                    x.ApprovedAt,
                    x.PostedAt,
                    x.IsActive
                }).ToListAsync()));

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.InventoryExits.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la salida." });
            }

            var lineLookups = await BuildInventoryLineLookupsAsync(db, entity.Lines.Select(x => x.ItemId), entity.Lines.Select(x => x.UnitId), [], []);

            return Results.Ok(new InventoryDocumentRequest
            {
                InventoryExitId = entity.Id,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                WarehouseId = entity.WarehouseId,
                Folio = entity.Folio,
                DocumentDate = entity.ExitDate,
                Status = entity.Status,
                Reason = entity.Reason,
                Notes = entity.Notes,
                ApprovedAt = entity.ApprovedAt,
                PostedAt = entity.PostedAt,
                IsActive = entity.IsActive,
                Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new InventoryLineRequest
                {
                    Id = x.Id,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    UnitId = x.UnitId,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitCost = x.UnitCost,
                    LineTotal = x.LineTotal,
                    ItemCode = lineLookups.Items.TryGetValue(x.ItemId, out var item) ? item.Code : string.Empty,
                    ItemName = lineLookups.Items.TryGetValue(x.ItemId, out item) ? item.Name : string.Empty,
                    UnitName = x.UnitId.HasValue && lineLookups.Units.TryGetValue(x.UnitId.Value, out var unit) ? unit.Name : string.Empty
                }).ToList()
            });
        });

        group.MapPost("/", async (InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetCompanyAsync(db, request.CompanyId);
            var branch = await GetBranchAsync(db, company.Id, request.BranchId);
            var warehouse = await GetWarehouseAsync(db, company.Id, request.WarehouseId);

            var entity = new InventoryExit
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                Folio = NormalizeFolio(request.Folio, "SAL"),
                ExitDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                Status = NormalizeStatus(request.Status),
                Reason = (request.Reason ?? string.Empty).Trim(),
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyExitLines(entity, request.Lines);
            db.InventoryExits.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.InventoryExits.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la salida." });
            }

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.WarehouseId = request.WarehouseId ?? entity.WarehouseId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.ExitDate = request.DocumentDate?.Date ?? entity.ExitDate;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.Reason = request.Reason?.Trim() ?? entity.Reason;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyExitLines(entity, request.Lines);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntity<InventoryExit>(id, db, "No se encontró la salida."));
    }

    private static void MapTransferEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/transfers").WithTags("InventoryTransfers");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.InventoryTransfers.AsNoTracking()
                .OrderByDescending(x => x.TransferDate)
                .Select(x => new
                {
                    InventoryTransferId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.SourceWarehouseId,
                    x.TargetWarehouseId,
                    x.Folio,
                    x.TransferDate,
                    x.Status,
                    x.Reason,
                    x.Notes,
                    x.ApprovedAt,
                    x.PostedAt,
                    x.IsActive
                }).ToListAsync()));

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.InventoryTransfers.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró el traspaso." });
            }

            var lineLookups = await BuildInventoryLineLookupsAsync(db, entity.Lines.Select(x => x.ItemId), entity.Lines.Select(x => x.UnitId), [], []);

            return Results.Ok(new InventoryDocumentRequest
            {
                InventoryTransferId = entity.Id,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                SourceWarehouseId = entity.SourceWarehouseId,
                TargetWarehouseId = entity.TargetWarehouseId,
                Folio = entity.Folio,
                DocumentDate = entity.TransferDate,
                Status = entity.Status,
                Reason = entity.Reason,
                Notes = entity.Notes,
                ApprovedAt = entity.ApprovedAt,
                PostedAt = entity.PostedAt,
                IsActive = entity.IsActive,
                Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new InventoryLineRequest
                {
                    Id = x.Id,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    UnitId = x.UnitId,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitCost = x.UnitCost,
                    LineTotal = x.LineTotal,
                    ItemCode = lineLookups.Items.TryGetValue(x.ItemId, out var item) ? item.Code : string.Empty,
                    ItemName = lineLookups.Items.TryGetValue(x.ItemId, out item) ? item.Name : string.Empty,
                    UnitName = x.UnitId.HasValue && lineLookups.Units.TryGetValue(x.UnitId.Value, out var unit) ? unit.Name : string.Empty
                }).ToList()
            });
        });

        group.MapPost("/", async (InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetCompanyAsync(db, request.CompanyId);
            var branch = await GetBranchAsync(db, company.Id, request.BranchId);
            var sourceWarehouse = await GetWarehouseAsync(db, company.Id, request.SourceWarehouseId);
            var targetWarehouse = await GetWarehouseAsync(db, company.Id, request.TargetWarehouseId, sourceWarehouse.Id);

            var entity = new InventoryTransfer
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                SourceWarehouseId = sourceWarehouse.Id,
                TargetWarehouseId = targetWarehouse.Id,
                Folio = NormalizeFolio(request.Folio, "TRA"),
                TransferDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                Status = NormalizeStatus(request.Status),
                Reason = (request.Reason ?? string.Empty).Trim(),
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyTransferLines(entity, request.Lines);
            db.InventoryTransfers.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.InventoryTransfers.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró el traspaso." });
            }

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.SourceWarehouseId = request.SourceWarehouseId ?? entity.SourceWarehouseId;
            entity.TargetWarehouseId = request.TargetWarehouseId ?? entity.TargetWarehouseId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.TransferDate = request.DocumentDate?.Date ?? entity.TransferDate;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.Reason = request.Reason?.Trim() ?? entity.Reason;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyTransferLines(entity, request.Lines);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntity<InventoryTransfer>(id, db, "No se encontró el traspaso."));
    }

    private static void MapAdjustmentEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/adjustments").WithTags("InventoryAdjustments");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.InventoryAdjustments.AsNoTracking()
                .OrderByDescending(x => x.AdjustmentDate)
                .Select(x => new
                {
                    InventoryAdjustmentId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.WarehouseId,
                    x.Folio,
                    x.AdjustmentDate,
                    x.AdjustmentType,
                    x.Status,
                    x.Reason,
                    x.Notes,
                    x.ApprovedAt,
                    x.PostedAt,
                    x.IsActive
                }).ToListAsync()));

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.InventoryAdjustments.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró el ajuste." });
            }

            var lineLookups = await BuildInventoryLineLookupsAsync(db, entity.Lines.Select(x => x.ItemId), entity.Lines.Select(x => x.UnitId), [], []);

            return Results.Ok(new InventoryDocumentRequest
            {
                InventoryAdjustmentId = entity.Id,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                WarehouseId = entity.WarehouseId,
                Folio = entity.Folio,
                DocumentDate = entity.AdjustmentDate,
                Status = entity.Status,
                Reason = entity.Reason,
                Notes = entity.Notes,
                ApprovedAt = entity.ApprovedAt,
                PostedAt = entity.PostedAt,
                IsActive = entity.IsActive,
                AdjustmentType = entity.AdjustmentType,
                Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new InventoryLineRequest
                {
                    Id = x.Id,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    UnitId = x.UnitId,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitCost = x.UnitCost,
                    LineTotal = x.LineTotal,
                    ItemCode = lineLookups.Items.TryGetValue(x.ItemId, out var item) ? item.Code : string.Empty,
                    ItemName = lineLookups.Items.TryGetValue(x.ItemId, out item) ? item.Name : string.Empty,
                    UnitName = x.UnitId.HasValue && lineLookups.Units.TryGetValue(x.UnitId.Value, out var unit) ? unit.Name : string.Empty
                }).ToList()
            });
        });

        group.MapPost("/", async (InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetCompanyAsync(db, request.CompanyId);
            var branch = await GetBranchAsync(db, company.Id, request.BranchId);
            var warehouse = await GetWarehouseAsync(db, company.Id, request.WarehouseId);

            var entity = new InventoryAdjustment
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                Folio = NormalizeFolio(request.Folio, "AJU"),
                AdjustmentDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                AdjustmentType = string.IsNullOrWhiteSpace(request.AdjustmentType) ? "positive" : request.AdjustmentType.Trim().ToLowerInvariant(),
                Status = NormalizeStatus(request.Status),
                Reason = (request.Reason ?? string.Empty).Trim(),
                Notes = (request.Notes ?? string.Empty).Trim(),
                ApprovedAt = request.ApprovedAt,
                PostedAt = request.PostedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyAdjustmentLines(entity, request.Lines);
            db.InventoryAdjustments.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.InventoryAdjustments.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró el ajuste." });
            }

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.WarehouseId = request.WarehouseId ?? entity.WarehouseId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.AdjustmentDate = request.DocumentDate?.Date ?? entity.AdjustmentDate;
            entity.AdjustmentType = string.IsNullOrWhiteSpace(request.AdjustmentType) ? entity.AdjustmentType : request.AdjustmentType.Trim().ToLowerInvariant();
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.Reason = request.Reason?.Trim() ?? entity.Reason;
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ApprovedAt = request.ApprovedAt;
            entity.PostedAt = request.PostedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyAdjustmentLines(entity, request.Lines);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntity<InventoryAdjustment>(id, db, "No se encontró el ajuste."));
    }

    private static void MapPhysicalCountEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/physical-counts").WithTags("InventoryPhysicalCounts");

        group.MapGet("/", async (NanchesoftDbContext db) =>
            Results.Ok(await db.PhysicalCounts.AsNoTracking()
                .OrderByDescending(x => x.CountDate)
                .Select(x => new
                {
                    PhysicalCountId = x.Id,
                    x.CompanyId,
                    x.BranchId,
                    x.WarehouseId,
                    x.Folio,
                    x.CountDate,
                    x.Status,
                    x.Notes,
                    x.ClosedAt,
                    x.IsActive
                }).ToListAsync()));

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.PhysicalCounts.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró el conteo físico." });
            }

            var lineLookups = await BuildInventoryLineLookupsAsync(db, entity.Lines.Select(x => x.ItemId), entity.Lines.Select(x => x.UnitId), entity.Lines.Select(x => x.LotId), entity.Lines.Select(x => x.SerialId));

            return Results.Ok(new InventoryDocumentRequest
            {
                PhysicalCountId = entity.Id,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                WarehouseId = entity.WarehouseId,
                Folio = entity.Folio,
                DocumentDate = entity.CountDate,
                Status = entity.Status,
                Notes = entity.Notes,
                ClosedAt = entity.ClosedAt,
                IsActive = entity.IsActive,
                Lines = entity.Lines.OrderBy(x => x.LineNumber).Select(x => new InventoryLineRequest
                {
                    Id = x.Id,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    UnitId = x.UnitId,
                    LotId = x.LotId,
                    SerialId = x.SerialId,
                    SystemQuantity = x.SystemQuantity,
                    CountedQuantity = x.CountedQuantity,
                    DifferenceQuantity = x.DifferenceQuantity,
                    ItemCode = lineLookups.Items.TryGetValue(x.ItemId, out var item) ? item.Code : string.Empty,
                    ItemName = lineLookups.Items.TryGetValue(x.ItemId, out item) ? item.Name : string.Empty,
                    UnitName = x.UnitId.HasValue && lineLookups.Units.TryGetValue(x.UnitId.Value, out var unit) ? unit.Name : string.Empty,
                    LotNumber = x.LotId.HasValue && lineLookups.Lots.TryGetValue(x.LotId.Value, out var lot) ? lot.Name : string.Empty,
                    SerialNumber = x.SerialId.HasValue && lineLookups.Serials.TryGetValue(x.SerialId.Value, out var serial) ? serial.Name : string.Empty
                }).ToList()
            });
        });

        group.MapPost("/", async (InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var company = await GetCompanyAsync(db, request.CompanyId);
            var branch = await GetBranchAsync(db, company.Id, request.BranchId);
            var warehouse = await GetWarehouseAsync(db, company.Id, request.WarehouseId);

            var entity = new PhysicalCount
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                WarehouseId = warehouse.Id,
                Folio = NormalizeFolio(request.Folio, "CFI"),
                CountDate = request.DocumentDate?.Date ?? DateTime.UtcNow.Date,
                Status = NormalizeStatus(request.Status),
                Notes = (request.Notes ?? string.Empty).Trim(),
                ClosedAt = request.ClosedAt,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };

            ApplyPhysicalCountLines(entity, request.Lines);
            db.PhysicalCounts.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, InventoryDocumentRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.PhysicalCounts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró el conteo físico." });
            }

            entity.CompanyId = request.CompanyId ?? entity.CompanyId;
            entity.BranchId = request.BranchId ?? entity.BranchId;
            entity.WarehouseId = request.WarehouseId ?? entity.WarehouseId;
            entity.Folio = NormalizeFolio(request.Folio, entity.Folio);
            entity.CountDate = request.DocumentDate?.Date ?? entity.CountDate;
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.Notes = request.Notes?.Trim() ?? entity.Notes;
            entity.ClosedAt = request.ClosedAt;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";

            ApplyPhysicalCountLines(entity, request.Lines);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntity<PhysicalCount>(id, db, "No se encontró el conteo físico."));
    }

    private static async Task<IResult> GetStockBalancesAsync(NanchesoftDbContext db)
    {
        var rows = await (
            from s in db.StockBalances.AsNoTracking()
            join i in db.Items.AsNoTracking() on s.ItemId equals i.Id into itemJoin
            from item in itemJoin.DefaultIfEmpty()
            join w in db.Warehouses.AsNoTracking() on s.WarehouseId equals w.Id into warehouseJoin
            from warehouse in warehouseJoin.DefaultIfEmpty()
            join l in db.ItemLots.AsNoTracking() on s.LotId equals l.Id into lotJoin
            from lot in lotJoin.DefaultIfEmpty()
            join sr in db.ItemSerials.AsNoTracking() on s.SerialId equals sr.Id into serialJoin
            from serial in serialJoin.DefaultIfEmpty()
            orderby s.UpdatedAt descending
            select new
            {
                StockBalanceId = s.Id,
                s.CompanyId,
                s.BranchId,
                s.WarehouseId,
                WarehouseName = warehouse != null ? warehouse.Name : string.Empty,
                s.ItemId,
                ItemCode = item != null ? item.Code : string.Empty,
                ItemName = item != null ? item.Name : string.Empty,
                s.LotId,
                LotNumber = lot != null ? lot.LotNumber : string.Empty,
                s.SerialId,
                SerialNumber = serial != null ? serial.SerialNumber : string.Empty,
                s.QuantityOnHand,
                s.QuantityReserved,
                s.QuantityAvailable,
                s.AverageCost,
                s.LastCost,
                s.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetStockByItemAsync(Guid itemId, NanchesoftDbContext db)
    {
        var rows = await (
            from s in db.StockBalances.AsNoTracking()
            where s.ItemId == itemId
            join w in db.Warehouses.AsNoTracking() on s.WarehouseId equals w.Id into warehouseJoin
            from warehouse in warehouseJoin.DefaultIfEmpty()
            join l in db.ItemLots.AsNoTracking() on s.LotId equals l.Id into lotJoin
            from lot in lotJoin.DefaultIfEmpty()
            join sr in db.ItemSerials.AsNoTracking() on s.SerialId equals sr.Id into serialJoin
            from serial in serialJoin.DefaultIfEmpty()
            orderby warehouse!.Name, lot!.LotNumber
            select new InventoryStockDetailRow
            {
                StockBalanceId = s.Id,
                WarehouseId = s.WarehouseId,
                WarehouseName = warehouse != null ? warehouse.Name : string.Empty,
                LotId = s.LotId,
                LotNumber = lot != null ? lot.LotNumber : string.Empty,
                SerialId = s.SerialId,
                SerialNumber = serial != null ? serial.SerialNumber : string.Empty,
                QuantityOnHand = s.QuantityOnHand,
                QuantityReserved = s.QuantityReserved,
                QuantityAvailable = s.QuantityAvailable,
                AverageCost = s.AverageCost,
                LastCost = s.LastCost
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetKardexAsync(NanchesoftDbContext db)
    {
        var rows = await (
            from m in db.InventoryMovements.AsNoTracking()
            join i in db.Items.AsNoTracking() on m.ItemId equals i.Id into itemJoin
            from item in itemJoin.DefaultIfEmpty()
            join w in db.Warehouses.AsNoTracking() on m.WarehouseId equals w.Id into warehouseJoin
            from warehouse in warehouseJoin.DefaultIfEmpty()
            orderby m.MovementDate descending
            select new
            {
                InventoryMovementId = m.Id,
                m.CompanyId,
                m.BranchId,
                m.WarehouseId,
                WarehouseName = warehouse != null ? warehouse.Name : string.Empty,
                m.ItemId,
                ItemCode = item != null ? item.Code : string.Empty,
                ItemName = item != null ? item.Name : string.Empty,
                m.MovementType,
                m.DocumentType,
                m.DocumentId,
                m.MovementDate,
                m.QuantityIn,
                m.QuantityOut,
                m.BalanceAfter,
                m.UnitCost,
                m.TotalCost,
                m.Reference,
                m.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetKardexByItemAsync(Guid itemId, NanchesoftDbContext db)
    {
        var rows = await (
            from m in db.InventoryMovements.AsNoTracking()
            where m.ItemId == itemId
            join w in db.Warehouses.AsNoTracking() on m.WarehouseId equals w.Id into warehouseJoin
            from warehouse in warehouseJoin.DefaultIfEmpty()
            orderby m.MovementDate descending
            select new InventoryKardexDetailRow
            {
                InventoryMovementId = m.Id,
                WarehouseId = m.WarehouseId,
                WarehouseName = warehouse != null ? warehouse.Name : string.Empty,
                MovementType = m.MovementType,
                DocumentType = m.DocumentType,
                DocumentId = m.DocumentId,
                MovementDate = m.MovementDate,
                QuantityIn = m.QuantityIn,
                QuantityOut = m.QuantityOut,
                BalanceAfter = m.BalanceAfter,
                UnitCost = m.UnitCost,
                TotalCost = m.TotalCost,
                Reference = m.Reference
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetLookupsAsync(NanchesoftDbContext db)
    {
        var companies = await db.Companies.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name)).ToListAsync();
        var branches = await db.Branches.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name)).ToListAsync();
        var warehouses = await db.Warehouses.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name)).ToListAsync();
        var items = await db.Items.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, $"{x.Code} · {x.Name}")).ToListAsync();
        var units = await db.Units.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name)).ToListAsync();
        var lots = await db.ItemLots.AsNoTracking().OrderBy(x => x.LotNumber).Select(x => new LookupItem(x.Id, x.LotNumber)).ToListAsync();
        var serials = await db.ItemSerials.AsNoTracking().OrderBy(x => x.SerialNumber).Select(x => new LookupItem(x.Id, x.SerialNumber)).ToListAsync();

        return Results.Ok(new InventoryLookupsResponse
        {
            Companies = companies,
            Branches = branches,
            Warehouses = warehouses,
            Items = items,
            Units = units,
            Lots = lots,
            Serials = serials
        });
    }

    private static void ApplyEntryLines(InventoryEntry entity, IEnumerable<InventoryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in NormalizeLines(lines))
        {
            entity.Lines.Add(new InventoryEntryLine
            {
                Id = line.Id ?? Guid.NewGuid(),
                InventoryEntryId = entity.Id,
                LineNumber = line.LineNumber,
                ItemId = line.ItemId!.Value,
                UnitId = line.UnitId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineTotal = line.LineTotal != 0 ? line.LineTotal : line.Quantity * line.UnitCost
            });
        }
    }

    private static void ApplyExitLines(InventoryExit entity, IEnumerable<InventoryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in NormalizeLines(lines))
        {
            entity.Lines.Add(new InventoryExitLine
            {
                Id = line.Id ?? Guid.NewGuid(),
                InventoryExitId = entity.Id,
                LineNumber = line.LineNumber,
                ItemId = line.ItemId!.Value,
                UnitId = line.UnitId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineTotal = line.LineTotal != 0 ? line.LineTotal : line.Quantity * line.UnitCost
            });
        }
    }

    private static void ApplyTransferLines(InventoryTransfer entity, IEnumerable<InventoryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in NormalizeLines(lines))
        {
            entity.Lines.Add(new InventoryTransferLine
            {
                Id = line.Id ?? Guid.NewGuid(),
                InventoryTransferId = entity.Id,
                LineNumber = line.LineNumber,
                ItemId = line.ItemId!.Value,
                UnitId = line.UnitId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineTotal = line.LineTotal != 0 ? line.LineTotal : line.Quantity * line.UnitCost
            });
        }
    }

    private static void ApplyAdjustmentLines(InventoryAdjustment entity, IEnumerable<InventoryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in NormalizeLines(lines))
        {
            entity.Lines.Add(new InventoryAdjustmentLine
            {
                Id = line.Id ?? Guid.NewGuid(),
                InventoryAdjustmentId = entity.Id,
                LineNumber = line.LineNumber,
                ItemId = line.ItemId!.Value,
                UnitId = line.UnitId,
                Description = (line.Description ?? string.Empty).Trim(),
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineTotal = line.LineTotal != 0 ? line.LineTotal : line.Quantity * line.UnitCost
            });
        }
    }

    private static void ApplyPhysicalCountLines(PhysicalCount entity, IEnumerable<InventoryLineRequest> lines)
    {
        entity.Lines.Clear();
        foreach (var line in lines.Where(x => x.ItemId.HasValue).OrderBy(x => x.LineNumber))
        {
            var difference = line.DifferenceQuantity != 0
                ? line.DifferenceQuantity
                : line.CountedQuantity - line.SystemQuantity;

            entity.Lines.Add(new PhysicalCountLine
            {
                Id = line.Id ?? Guid.NewGuid(),
                PhysicalCountId = entity.Id,
                LineNumber = line.LineNumber,
                ItemId = line.ItemId!.Value,
                LotId = line.LotId,
                SerialId = line.SerialId,
                UnitId = line.UnitId,
                SystemQuantity = line.SystemQuantity,
                CountedQuantity = line.CountedQuantity,
                DifferenceQuantity = difference
            });
        }
    }

    private static IEnumerable<InventoryLineRequest> NormalizeLines(IEnumerable<InventoryLineRequest> lines)
        => lines.Where(x => x.ItemId.HasValue)
            .Select((x, index) =>
            {
                x.LineNumber = x.LineNumber <= 0 ? index + 1 : x.LineNumber;
                return x;
            })
            .OrderBy(x => x.LineNumber)
            .ToList();

    private static async Task<Company> GetCompanyAsync(NanchesoftDbContext db, Guid? companyId)
    {
        if (companyId.HasValue)
        {
            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId.Value);
            if (company is not null)
            {
                return company;
            }
        }

        return await db.Companies.OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static async Task<Branch> GetBranchAsync(NanchesoftDbContext db, Guid companyId, Guid? branchId)
    {
        if (branchId.HasValue)
        {
            var branch = await db.Branches.FirstOrDefaultAsync(x => x.Id == branchId.Value);
            if (branch is not null)
            {
                return branch;
            }
        }

        return await db.Branches.Where(x => x.CompanyId == companyId).OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static async Task<Warehouse> GetWarehouseAsync(NanchesoftDbContext db, Guid companyId, Guid? warehouseId, Guid? fallbackWarehouseId = null)
    {
        if (warehouseId.HasValue)
        {
            var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == warehouseId.Value);
            if (warehouse is not null)
            {
                return warehouse;
            }
        }

        if (fallbackWarehouseId.HasValue)
        {
            var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == fallbackWarehouseId.Value);
            if (warehouse is not null)
            {
                return warehouse;
            }
        }

        return await db.Warehouses.Where(x => x.CompanyId == companyId).OrderBy(x => x.CreatedAt).FirstAsync();
    }

    private static string NormalizeFolio(string? folio, string defaultPrefix)
        => string.IsNullOrWhiteSpace(folio)
            ? $"{defaultPrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : folio.Trim();

    private static string NormalizeStatus(string? status, string fallback = "draft")
        => string.IsNullOrWhiteSpace(status) ? fallback : status.Trim().ToLowerInvariant();

    private static async Task<IResult> DeleteEntity<TEntity>(Guid id, NanchesoftDbContext db, string notFoundMessage) where TEntity : class
    {
        var entity = await db.Set<TEntity>().FindAsync(id);
        if (entity is null)
        {
            return Results.NotFound(new { message = notFoundMessage });
        }

        db.Set<TEntity>().Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<InventoryLineLookupBag> BuildInventoryLineLookupsAsync(
        NanchesoftDbContext db,
        IEnumerable<Guid> itemIds,
        IEnumerable<Guid?> unitIds,
        IEnumerable<Guid?> lotIds,
        IEnumerable<Guid?> serialIds)
    {
        var itemKeys = itemIds.Distinct().ToList();
        var unitKeys = unitIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var lotKeys = lotIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var serialKeys = serialIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();

        return new InventoryLineLookupBag
        {
            Items = await db.Items.AsNoTracking().Where(x => itemKeys.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LookupCodeName(x.Code, x.Name)),
            Units = await db.Units.AsNoTracking().Where(x => unitKeys.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LookupCodeName(x.Abbreviation, x.Name)),
            Lots = await db.ItemLots.AsNoTracking().Where(x => lotKeys.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LookupCodeName(x.LotNumber, x.LotNumber)),
            Serials = await db.ItemSerials.AsNoTracking().Where(x => serialKeys.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LookupCodeName(x.SerialNumber, x.SerialNumber))
        };
    }
}

public sealed class InventoryDocumentRequest
{
    public Guid? InventoryEntryId { get; set; }
    public Guid? InventoryExitId { get; set; }
    public Guid? InventoryTransferId { get; set; }
    public Guid? InventoryAdjustmentId { get; set; }
    public Guid? PhysicalCountId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public Guid? TargetWarehouseId { get; set; }
    public string? Folio { get; set; }
    public DateTime? DocumentDate { get; set; }
    public string? Status { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? AdjustmentType { get; set; }
    public bool IsActive { get; set; } = true;
    public List<InventoryLineRequest> Lines { get; set; } = [];
}

public sealed class InventoryLineRequest
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? UnitName { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public sealed class InventoryLookupsResponse
{
    public List<LookupItem> Companies { get; set; } = [];
    public List<LookupItem> Branches { get; set; } = [];
    public List<LookupItem> Warehouses { get; set; } = [];
    public List<LookupItem> Items { get; set; } = [];
    public List<LookupItem> Units { get; set; } = [];
    public List<LookupItem> Lots { get; set; } = [];
    public List<LookupItem> Serials { get; set; } = [];
}

public sealed record LookupItem(Guid Id, string Name);
public sealed record LookupCodeName(string Code, string Name);

public sealed class InventoryStockDetailRow
{
    public Guid StockBalanceId { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid? SerialId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public decimal LastCost { get; set; }
}

public sealed class InventoryKardexDetailRow
{
    public Guid InventoryMovementId { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public DateTime MovementDate { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Reference { get; set; } = string.Empty;
}

internal sealed class InventoryLineLookupBag
{
    public Dictionary<Guid, LookupCodeName> Items { get; set; } = new();
    public Dictionary<Guid, LookupCodeName> Units { get; set; } = new();
    public Dictionary<Guid, LookupCodeName> Lots { get; set; } = new();
    public Dictionary<Guid, LookupCodeName> Serials { get; set; } = new();
}
