
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductTechnicalCostingEndpoints
{
    public static void MapProductTechnicalCostingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Product Technical Costing");

        group.MapGet("/technical-center/overview", async (NanchesoftDbContext db) =>
        {
            var items = await BuildTechnicalCenterOverviewAsync(db);
            return Results.Ok(items);
        });

        group.MapGet("/engineering-readiness", async (NanchesoftDbContext db) =>
        {
            var rows = await BuildEngineeringReadinessAsync(db);
            return Results.Ok(rows);
        });

        group.MapGet("/technical-center/overview/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var items = await BuildTechnicalCenterOverviewAsync(db, finishedProductId);
            var row = items.FirstOrDefault();
            return row is null ? Results.NotFound() : Results.Ok(row);
        });

        group.MapPost("/technical-sheets/generate-from-product/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var product = await LoadFinishedProductAsync(db, finishedProductId);
            if (product is null)
            {
                return Results.NotFound();
            }

            var existing = await db.Set<ProductTechnicalSheet>()
                .Include(x => x.Materials)
                .Include(x => x.Processes)
                .FirstOrDefaultAsync(x => x.FinishedProductId == finishedProductId);

            if (existing is not null)
            {
                db.Set<ProductTechnicalSheetMaterial>().RemoveRange(existing.Materials);
                db.Set<ProductTechnicalSheetProcess>().RemoveRange(existing.Processes);
            }

            var sheet = await BuildTechnicalSheetFromProductAsync(db, product, existing, "web-api");
            if (existing is null)
            {
                db.Set<ProductTechnicalSheet>().Add(sheet);
            }

            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, finishedProductId, "web-api");
            var overview = (await BuildTechnicalCenterOverviewAsync(db, finishedProductId)).First();
            return Results.Ok(new ProductTechnicalActionResponseDto(
                overview.FinishedProductId,
                overview.ProductCode,
                overview.ProductName,
                "generate-technical-sheet",
                "ok",
                $"Ficha técnica {(existing is null ? "creada" : "regenerada")} correctamente.",
                overview.IsAuthorizedForExplosion,
                overview.ReadinessPercent,
                overview.PrincipalBlocker));
        });

        group.MapPost("/cost-sheets/generate-from-product/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var product = await LoadFinishedProductAsync(db, finishedProductId);
            if (product is null)
            {
                return Results.NotFound();
            }

            var existing = await db.Set<ProductCostSheet>().FirstOrDefaultAsync(x => x.FinishedProductId == finishedProductId);
            var costSheet = await BuildCostSheetFromProductAsync(db, product, existing, "web-api");
            if (existing is null)
            {
                db.Set<ProductCostSheet>().Add(costSheet);
            }

            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, finishedProductId, "web-api");
            var overview = (await BuildTechnicalCenterOverviewAsync(db, finishedProductId)).First();
            return Results.Ok(new ProductTechnicalActionResponseDto(
                overview.FinishedProductId,
                overview.ProductCode,
                overview.ProductName,
                "generate-cost-sheet",
                "ok",
                $"Hoja de costo {(existing is null ? "creada" : "recalculada")} correctamente.",
                overview.IsAuthorizedForExplosion,
                overview.ReadinessPercent,
                overview.PrincipalBlocker));
        });

        group.MapPost("/authorizations/sync/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var response = await SyncAuthorizationRecordAsync(db, finishedProductId, "web-api", autoAuthorize: false);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapPost("/authorizations/auto-authorize/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var response = await SyncAuthorizationRecordAsync(db, finishedProductId, "web-api", autoAuthorize: true);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapGet("/technical-sheets", async (NanchesoftDbContext db) =>
        {
            var items = await db.Set<ProductTechnicalSheet>()
                .Include(x => x.Materials)
                .Include(x => x.Processes)
                .OrderBy(x => x.SheetCode)
                .ToListAsync();

            return Results.Ok(items.Select(MapTechnicalSheetDto));
        });

        group.MapGet("/technical-sheets/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var item = await db.Set<ProductTechnicalSheet>()
                .Include(x => x.Materials)
                .Include(x => x.Processes)
                .FirstOrDefaultAsync(x => x.Id == id);

            return item is null ? Results.NotFound() : Results.Ok(MapTechnicalSheetDto(item));
        });

        group.MapPost("/technical-sheets", async (ProductTechnicalSheetUpsertRequest request, NanchesoftDbContext db) =>
        {
            var validationError = ValidateTechnicalSheetRequest(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var entity = new ProductTechnicalSheet
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                FinishedProductId = request.FinishedProductId,
                ProductStyleId = request.ProductStyleId,
                SheetCode = NormalizeCode(request.SheetCode),
                SheetName = NormalizeText(request.SheetName),
                Status = NormalizeStatus(request.Status, "draft"),
                ProductDisplayName = NormalizeText(request.ProductDisplayName),
                PhotoUrl = NormalizeText(request.PhotoUrl),
                MainMaterialName = NormalizeText(request.MainMaterialName),
                MainColorName = NormalizeText(request.MainColorName),
                SizeRunCode = NormalizeText(request.SizeRunCode),
                Notes = NormalizeText(request.Notes),
                IsApproved = request.IsApproved,
                ApprovedAtUtc = request.IsApproved ? request.ApprovedAtUtc ?? DateTime.UtcNow : null,
                ApprovedBy = request.IsApproved ? NormalizeText(request.ApprovedBy) : string.Empty,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.UpdatedBy ?? "api"
            };

            ReplaceTechnicalSheetChildren(entity, request);

            db.Set<ProductTechnicalSheet>().Add(entity);
            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, entity.FinishedProductId, request.UpdatedBy ?? "api");
            return Results.Created($"/api/products/technical-sheets/{entity.Id}", MapTechnicalSheetDto(entity));
        });

        group.MapPut("/technical-sheets/{id:guid}", async (Guid id, ProductTechnicalSheetUpsertRequest request, NanchesoftDbContext db) =>
        {
            var validationError = ValidateTechnicalSheetRequest(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var entity = await db.Set<ProductTechnicalSheet>()
                .Include(x => x.Materials)
                .Include(x => x.Processes)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity is null) return Results.NotFound();

            entity.TenantId = request.TenantId;
            entity.CompanyId = request.CompanyId;
            entity.FinishedProductId = request.FinishedProductId;
            entity.ProductStyleId = request.ProductStyleId;
            entity.SheetCode = NormalizeCode(request.SheetCode);
            entity.SheetName = NormalizeText(request.SheetName);
            entity.Status = NormalizeStatus(request.Status, entity.Status);
            entity.ProductDisplayName = NormalizeText(request.ProductDisplayName);
            entity.PhotoUrl = NormalizeText(request.PhotoUrl);
            entity.MainMaterialName = NormalizeText(request.MainMaterialName);
            entity.MainColorName = NormalizeText(request.MainColorName);
            entity.SizeRunCode = NormalizeText(request.SizeRunCode);
            entity.Notes = NormalizeText(request.Notes);
            entity.IsApproved = request.IsApproved;
            entity.ApprovedAtUtc = request.IsApproved ? request.ApprovedAtUtc ?? entity.ApprovedAtUtc ?? DateTime.UtcNow : null;
            entity.ApprovedBy = request.IsApproved ? NormalizeText(request.ApprovedBy) : string.Empty;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = request.UpdatedBy ?? "api";

            db.Set<ProductTechnicalSheetMaterial>().RemoveRange(entity.Materials);
            db.Set<ProductTechnicalSheetProcess>().RemoveRange(entity.Processes);
            ReplaceTechnicalSheetChildren(entity, request);

            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, entity.FinishedProductId, request.UpdatedBy ?? "api");
            return Results.Ok(MapTechnicalSheetDto(entity));
        });

        group.MapDelete("/technical-sheets/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<ProductTechnicalSheet>().FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            var finishedProductId = entity.FinishedProductId;
            db.Remove(entity);
            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, finishedProductId, "api");
            return Results.NoContent();
        });

        group.MapGet("/cost-sheets", async (NanchesoftDbContext db) =>
        {
            var items = await db.Set<ProductCostSheet>().OrderBy(x => x.CostSheetCode).ToListAsync();
            return Results.Ok(items.Select(MapCostSheetDto));
        });

        group.MapGet("/cost-sheets/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<ProductCostSheet>().FindAsync(id);
            return entity is null ? Results.NotFound() : Results.Ok(MapCostSheetDto(entity));
        });

        group.MapPost("/cost-sheets", async (ProductCostSheetUpsertRequest request, NanchesoftDbContext db) =>
        {
            var entity = ApplyCostSheet(request, null);
            db.Set<ProductCostSheet>().Add(entity);
            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, entity.FinishedProductId, request.UpdatedBy ?? "api");
            return Results.Created($"/api/products/cost-sheets/{entity.Id}", MapCostSheetDto(entity));
        });

        group.MapPut("/cost-sheets/{id:guid}", async (Guid id, ProductCostSheetUpsertRequest request, NanchesoftDbContext db) =>
        {
            var existing = await db.Set<ProductCostSheet>().FindAsync(id);
            if (existing is null) return Results.NotFound();
            var entity = ApplyCostSheet(request, existing);
            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, entity.FinishedProductId, request.UpdatedBy ?? "api");
            return Results.Ok(MapCostSheetDto(entity));
        });

        group.MapDelete("/cost-sheets/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<ProductCostSheet>().FindAsync(id);
            if (entity is null) return Results.NotFound();
            var finishedProductId = entity.FinishedProductId;
            db.Set<ProductCostSheet>().Remove(entity);
            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, finishedProductId, "api");
            return Results.NoContent();
        });

        group.MapGet("/authorizations", async (NanchesoftDbContext db) =>
        {
            var items = await db.Set<ProductAuthorizationRecord>().OrderBy(x => x.AuthorizationCode).ToListAsync();
            return Results.Ok(items.Select(MapAuthorizationDto));
        });

        group.MapGet("/authorizations/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<ProductAuthorizationRecord>().FindAsync(id);
            return entity is null ? Results.NotFound() : Results.Ok(MapAuthorizationDto(entity));
        });

        group.MapPost("/authorizations", async (ProductAuthorizationRecordUpsertRequest request, NanchesoftDbContext db) =>
        {
            var entity = ApplyAuthorization(request, null);
            db.Set<ProductAuthorizationRecord>().Add(entity);
            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, entity.FinishedProductId, request.UpdatedBy ?? "api");
            return Results.Created($"/api/products/authorizations/{entity.Id}", MapAuthorizationDto(entity));
        });

        group.MapPut("/authorizations/{id:guid}", async (Guid id, ProductAuthorizationRecordUpsertRequest request, NanchesoftDbContext db) =>
        {
            var existing = await db.Set<ProductAuthorizationRecord>().FindAsync(id);
            if (existing is null) return Results.NotFound();
            var entity = ApplyAuthorization(request, existing);
            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, entity.FinishedProductId, request.UpdatedBy ?? "api");
            return Results.Ok(MapAuthorizationDto(entity));
        });

        group.MapDelete("/authorizations/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<ProductAuthorizationRecord>().FindAsync(id);
            if (entity is null) return Results.NotFound();
            var finishedProductId = entity.FinishedProductId;
            db.Set<ProductAuthorizationRecord>().Remove(entity);
            await db.SaveChangesAsync();
            await SyncFinishedProductStatusAsync(db, finishedProductId, "api");
            return Results.NoContent();
        });

        MapSimpleCrud<ProductSizeConsumptionVariation, ProductSizeConsumptionVariationDto, ProductSizeConsumptionVariationUpsertRequest>(
            group,
            "size-consumption-variations",
            db => db.Set<ProductSizeConsumptionVariation>().OrderBy(x => x.FinishedProductId).ThenBy(x => x.BaseSizeCode).ThenBy(x => x.TargetSizeCode),
            MapVariationDto,
            (request, existing) =>
            {
                var entity = existing ?? new ProductSizeConsumptionVariation { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, CreatedBy = request.UpdatedBy ?? "api" };
                entity.TenantId = request.TenantId;
                entity.CompanyId = request.CompanyId;
                entity.FinishedProductId = request.FinishedProductId;
                entity.ProductComponentId = request.ProductComponentId;
                entity.BaseSizeCode = NormalizeText(request.BaseSizeCode);
                entity.TargetSizeCode = NormalizeText(request.TargetSizeCode);
                entity.VariationPercent = request.VariationPercent;
                entity.QuantityDelta = request.QuantityDelta;
                entity.AppliesToConsumption = request.AppliesToConsumption;
                entity.AppliesToCosting = request.AppliesToCosting;
                entity.Notes = NormalizeText(request.Notes);
                entity.IsActive = request.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = request.UpdatedBy ?? "api";
                return entity;
            });
    }

    private static async Task<List<ProductTechnicalCenterProductOverviewDto>> BuildTechnicalCenterOverviewAsync(NanchesoftDbContext db, Guid? finishedProductId = null)
    {
        var productsQuery = db.Set<FinishedProduct>()
            .AsNoTracking()
            .Include(x => x.ProductStyle)
            .Include(x => x.ProductSizeRun)
            .Include(x => x.MainMaterialItem)
            .AsQueryable();

        if (finishedProductId.HasValue)
        {
            productsQuery = productsQuery.Where(x => x.Id == finishedProductId.Value);
        }

        var products = await productsQuery.OrderBy(x => x.Code).ToListAsync();
        if (products.Count == 0)
        {
            return [];
        }

        var productIds = products.Select(x => x.Id).ToList();
        var technicalSheets = await db.Set<ProductTechnicalSheet>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        var costSheets = await db.Set<ProductCostSheet>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        var authorizations = await db.Set<ProductAuthorizationRecord>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        var materialCounts = await db.Set<FinishedProductMaterial>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId) && x.IsActive)
            .GroupBy(x => x.FinishedProductId)
            .Select(x => new { FinishedProductId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.FinishedProductId, x => x.Count);
        var legacyConsumptionCounts = await db.Set<ProductConsumptionProfile>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId) && x.IsActive)
            .GroupBy(x => x.FinishedProductId)
            .Select(x => new { FinishedProductId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.FinishedProductId, x => x.Count);

        // Authorized consumption templates (style + size run)
        var overviewStyleIds = products.Where(x => x.ProductStyleId.HasValue).Select(x => x.ProductStyleId!.Value).ToHashSet();
        var overviewRunIds = products.Where(x => x.ProductSizeRunId.HasValue).Select(x => x.ProductSizeRunId!.Value).ToHashSet();
        var authorizedTemplateKeys2 = await db.Set<ConsumptionTemplate>().AsNoTracking()
            .Where(x => x.IsActive && x.IsAuthorized && overviewStyleIds.Contains(x.ProductStyleId) && overviewRunIds.Contains(x.ProductSizeRunId))
            .Select(x => new { x.ProductStyleId, x.ProductSizeRunId })
            .ToListAsync();
        var templateKeySet = authorizedTemplateKeys2.Select(x => (x.ProductStyleId, x.ProductSizeRunId)).ToHashSet();

        var results = new List<ProductTechnicalCenterProductOverviewDto>();
        foreach (var product in products)
        {
            var sheet = technicalSheets.FirstOrDefault(x => x.FinishedProductId == product.Id);
            var cost = costSheets.FirstOrDefault(x => x.FinishedProductId == product.Id);
            var authorization = authorizations.FirstOrDefault(x => x.FinishedProductId == product.Id);
            var legacyCount = legacyConsumptionCounts.TryGetValue(product.Id, out var lc) ? lc : 0;
            var hasTemplate = product.ProductStyleId.HasValue && product.ProductSizeRunId.HasValue
                && templateKeySet.Contains((product.ProductStyleId.Value, product.ProductSizeRunId.Value));
            var conCount = legacyCount > 0 ? legacyCount : (hasTemplate ? 1 : 0);
            var row = BuildOverview(product, sheet, cost, authorization,
                materialCounts.TryGetValue(product.Id, out var matCount) ? matCount : 0,
                conCount);
            results.Add(row);
        }

        return results;
    }

    private static ProductTechnicalCenterProductOverviewDto BuildOverview(
        FinishedProduct product,
        ProductTechnicalSheet? sheet,
        ProductCostSheet? cost,
        ProductAuthorizationRecord? authorization,
        int materialAssignmentCount,
        int consumptionProfileCount)
    {
        var hasPhoto = product.HasPhoto || !string.IsNullOrWhiteSpace(sheet?.PhotoUrl) || !string.IsNullOrWhiteSpace(product.ProductStyle?.PhotoUrl);
        var hasConsumption = product.HasConsumptionDefinition || consumptionProfileCount > 0;
        var hasMaterials = product.HasMaterialAssignments || materialAssignmentCount > 0;
        var hasTechnicalSheet = sheet is not null;
        var hasApprovedTechnicalSheet = sheet?.IsApproved ?? false;
        var hasCostSheet = cost is not null;
        var hasApprovedCostSheet = cost?.IsApproved ?? false;

        var checks = new[]
        {
            hasPhoto,
            hasConsumption,
            hasMaterials,
            hasTechnicalSheet,
            hasApprovedTechnicalSheet,
            hasCostSheet,
            hasApprovedCostSheet
        };
        var readinessPercent = (int)Math.Round(checks.Count(x => x) * 100m / checks.Length, MidpointRounding.AwayFromZero);

        var missing = new List<string>();
        if (!hasPhoto) missing.Add("foto");
        if (!hasConsumption) missing.Add("consumos");
        if (!hasMaterials) missing.Add("materiales");
        if (!hasTechnicalSheet) missing.Add("ficha técnica");
        if (hasTechnicalSheet && !hasApprovedTechnicalSheet) missing.Add("ficha aprobada");
        if (!hasCostSheet) missing.Add("hoja de costo");
        if (hasCostSheet && !hasApprovedCostSheet) missing.Add("hoja costo aprobada");

        var principalBlocker = missing.FirstOrDefault() ?? string.Empty;
        var authorizationStatus = authorization?.Status ?? (missing.Count == 0 ? "authorized" : "pending");

        return new ProductTechnicalCenterProductOverviewDto(
            product.Id,
            product.TenantId,
            product.CompanyId,
            product.Code,
            product.Name,
            product.ProductStyle?.Name ?? string.Empty,
            product.ProductSizeRun?.Name ?? string.Empty,
            product.MainMaterialItem?.Name ?? string.Empty,
            hasPhoto,
            hasConsumption,
            hasMaterials,
            hasTechnicalSheet,
            hasApprovedTechnicalSheet,
            hasCostSheet,
            hasApprovedCostSheet,
            product.IsAuthorizedForExplosion,
            authorizationStatus,
            sheet?.SheetCode ?? string.Empty,
            cost?.CostSheetCode ?? string.Empty,
            authorization?.AuthorizationCode ?? string.Empty,
            materialAssignmentCount,
            consumptionProfileCount,
            cost?.TotalCost ?? 0m,
            cost?.SuggestedSalePrice ?? 0m,
            readinessPercent,
            principalBlocker,
            string.Join(", ", missing));
    }

    private static async Task<FinishedProduct?> LoadFinishedProductAsync(NanchesoftDbContext db, Guid finishedProductId)
        => await db.Set<FinishedProduct>()
            .Include(x => x.ProductStyle)
            .Include(x => x.ProductSizeRun)
            .Include(x => x.ProductColor)
            .Include(x => x.MainMaterialItem)
            .FirstOrDefaultAsync(x => x.Id == finishedProductId);

    private static async Task<ProductTechnicalSheet> BuildTechnicalSheetFromProductAsync(NanchesoftDbContext db, FinishedProduct product, ProductTechnicalSheet? existing, string user)
    {
        var assignments = await db.Set<FinishedProductMaterial>()
            .Include(x => x.ProductComponent)
                .ThenInclude(x => x!.ProductionPhase)
            .Include(x => x.MaterialItem)
                .ThenInclude(x => x!.IssueUnit)
            .Include(x => x.MaterialItem)
                .ThenInclude(x => x!.PurchaseUnit)
            .Where(x => x.FinishedProductId == product.Id && x.IsActive)
            .OrderBy(x => x.ProductComponent!.Code)
            .ThenBy(x => x.SizeCode)
            .ToListAsync();

        var components = assignments
            .Where(x => x.ProductComponent is not null)
            .Select(x => x.ProductComponent!)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Code)
            .ToList();

        var sheet = existing ?? new ProductTechnicalSheet
        {
            Id = Guid.NewGuid(),
            TenantId = product.TenantId,
            CompanyId = product.CompanyId,
            FinishedProductId = product.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user
        };

        sheet.ProductStyleId = product.ProductStyleId;
        sheet.SheetCode = string.IsNullOrWhiteSpace(existing?.SheetCode) ? $"FT-{product.Code}" : existing!.SheetCode;
        sheet.SheetName = string.IsNullOrWhiteSpace(existing?.SheetName) ? $"Ficha técnica {product.Name}" : existing!.SheetName;
        sheet.Status = NormalizeStatus(existing?.Status, "draft");
        sheet.ProductDisplayName = product.Name;
        sheet.PhotoUrl = !string.IsNullOrWhiteSpace(product.ProductStyle?.PhotoUrl) ? product.ProductStyle!.PhotoUrl : existing?.PhotoUrl ?? string.Empty;
        sheet.MainMaterialName = product.MainMaterialItem?.Name ?? existing?.MainMaterialName ?? string.Empty;
        sheet.MainColorName = product.ProductColor?.Name ?? string.Empty;
        sheet.SizeRunCode = product.ProductSizeRun?.Code ?? existing?.SizeRunCode ?? string.Empty;
        sheet.Notes = existing?.Notes ?? string.Empty;
        sheet.IsApproved = existing?.IsApproved ?? false;
        sheet.ApprovedAtUtc = existing?.ApprovedAtUtc;
        sheet.ApprovedBy = existing?.ApprovedBy ?? string.Empty;
        sheet.IsActive = existing?.IsActive ?? true;
        sheet.UpdatedAt = DateTime.UtcNow;
        sheet.UpdatedBy = user;

        sheet.Materials = assignments.Select((x, index) => new ProductTechnicalSheetMaterial
        {
            Id = Guid.NewGuid(),
            ProductTechnicalSheetId = sheet.Id,
            MaterialItemId = x.MaterialItemId,
            ComponentCode = x.ProductComponent?.Code ?? string.Empty,
            ComponentName = x.ProductComponent?.Name ?? string.Empty,
            MaterialCode = x.MaterialItem?.Code ?? string.Empty,
            MaterialName = x.MaterialItem?.Name ?? string.Empty,
            UnitCode = x.MaterialItem?.IssueUnit?.Code ?? x.MaterialItem?.PurchaseUnit?.Code ?? string.Empty,
            Quantity = x.Quantity,
            WastePercent = 0m,
            SortOrder = index + 1,
            ShowOnTechnicalSheet = true,
            Notes = string.IsNullOrWhiteSpace(x.SizeCode) ? string.Empty : $"Talla {x.SizeCode}"
        }).ToList();

        sheet.Processes = components.Select((x, index) => new ProductTechnicalSheetProcess
        {
            Id = Guid.NewGuid(),
            ProductTechnicalSheetId = sheet.Id,
            ProcessCode = x.Code,
            ProcessName = x.Name,
            WorkstationCode = x.ProductionPhase?.Code ?? string.Empty,
            DeliverToWarehouseCode = string.Empty,
            RequiresVoucherCard = x.ShowOnProductionCard,
            ShowMaterialsOnVoucher = x.ShowOnProductionCard,
            SortOrder = index + 1,
            Notes = x.Notes
        }).ToList();

        return sheet;
    }

    private static async Task<ProductCostSheet> BuildCostSheetFromProductAsync(NanchesoftDbContext db, FinishedProduct product, ProductCostSheet? existing, string user)
    {
        var assignments = await db.Set<FinishedProductMaterial>()
            .Include(x => x.MaterialItem)
            .Where(x => x.FinishedProductId == product.Id && x.IsActive)
            .ToListAsync();

        decimal directMaterialCost = 0m;
        decimal serviceCost = 0m;

        foreach (var row in assignments)
        {
            var unitCost = row.MaterialItem?.StandardCost
                ?? row.MaterialItem?.AuthorizedCost
                ?? row.MaterialItem?.LastPurchaseCost
                ?? 0m;
            var lineTotal = Math.Round(row.Quantity * unitCost, 4, MidpointRounding.AwayFromZero);
            if (row.MaterialItem?.IsServiceItem == true)
            {
                serviceCost += lineTotal;
            }
            else
            {
                directMaterialCost += lineTotal;
            }
        }

        var directLaborCost = existing?.DirectLaborCost ?? 0m;
        var indirectManufacturingCost = existing?.IndirectManufacturingCost ?? 0m;
        var packagingCost = existing?.PackagingCost ?? 0m;
        var targetMarginPercent = existing?.TargetMarginPercent ?? 35m;
        var totalCost = Math.Round(directMaterialCost + directLaborCost + indirectManufacturingCost + packagingCost + serviceCost, 4, MidpointRounding.AwayFromZero);
        var suggestedSalePrice = Math.Round(totalCost * (1m + (targetMarginPercent / 100m)), 4, MidpointRounding.AwayFromZero);
        var technicalSheetId = existing?.ProductTechnicalSheetId ?? await db.Set<ProductTechnicalSheet>()
            .Where(x => x.FinishedProductId == product.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        var entity = existing ?? new ProductCostSheet
        {
            Id = Guid.NewGuid(),
            TenantId = product.TenantId,
            CompanyId = product.CompanyId,
            FinishedProductId = product.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user
        };

        entity.ProductTechnicalSheetId = technicalSheetId;
        entity.CostSheetCode = string.IsNullOrWhiteSpace(existing?.CostSheetCode) ? $"HC-{product.Code}" : existing!.CostSheetCode;
        entity.Status = NormalizeStatus(existing?.Status, "draft");
        entity.DirectMaterialCost = directMaterialCost;
        entity.DirectLaborCost = directLaborCost;
        entity.IndirectManufacturingCost = indirectManufacturingCost;
        entity.PackagingCost = packagingCost;
        entity.ServiceCost = serviceCost;
        entity.TotalCost = totalCost;
        entity.TargetMarginPercent = targetMarginPercent;
        entity.SuggestedSalePrice = suggestedSalePrice;
        entity.CurrencyCode = string.IsNullOrWhiteSpace(existing?.CurrencyCode) ? "MXN" : existing!.CurrencyCode;
        entity.Notes = existing?.Notes ?? "Generada automáticamente a partir de materiales y consumos del producto.";
        entity.IsApproved = existing?.IsApproved ?? false;
        entity.ApprovedAtUtc = existing?.ApprovedAtUtc;
        entity.ApprovedBy = existing?.ApprovedBy ?? string.Empty;
        entity.IsActive = existing?.IsActive ?? true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = user;
        return entity;
    }

    private static async Task<ProductTechnicalActionResponseDto?> SyncAuthorizationRecordAsync(NanchesoftDbContext db, Guid finishedProductId, string user, bool autoAuthorize)
    {
        var product = await LoadFinishedProductAsync(db, finishedProductId);
        if (product is null)
        {
            return null;
        }

        var sheet = await db.Set<ProductTechnicalSheet>()
            .Where(x => x.FinishedProductId == finishedProductId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        var cost = await db.Set<ProductCostSheet>()
            .Where(x => x.FinishedProductId == finishedProductId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        var authorization = await db.Set<ProductAuthorizationRecord>()
            .Where(x => x.FinishedProductId == finishedProductId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        var materialCount = await db.Set<FinishedProductMaterial>().CountAsync(x => x.FinishedProductId == finishedProductId && x.IsActive);
        var legacyConsumptionCount = await db.Set<ProductConsumptionProfile>().CountAsync(x => x.FinishedProductId == finishedProductId && x.IsActive);
        var hasTemplateConsumption = product.ProductStyleId.HasValue && product.ProductSizeRunId.HasValue
            && await db.Set<ConsumptionTemplate>().AnyAsync(x => x.IsActive && x.IsAuthorized
                && x.ProductStyleId == product.ProductStyleId.Value
                && x.ProductSizeRunId == product.ProductSizeRunId.Value);
        var consumptionCount = legacyConsumptionCount > 0 ? legacyConsumptionCount : (hasTemplateConsumption ? 1 : 0);

        var hasPhoto = product.HasPhoto || !string.IsNullOrWhiteSpace(sheet?.PhotoUrl) || !string.IsNullOrWhiteSpace(product.ProductStyle?.PhotoUrl);
        var hasConsumption = product.HasConsumptionDefinition || consumptionCount > 0;
        var hasMaterials = product.HasMaterialAssignments || materialCount > 0;
        var hasCostSheet = cost is not null;
        var hasApprovedTechnicalSheet = sheet?.IsApproved ?? false;
        var hasApprovedCostSheet = cost?.IsApproved ?? false;

        var blockers = new List<string>();
        if (!hasPhoto) blockers.Add("Falta foto.");
        if (!hasConsumption) blockers.Add("Faltan consumos.");
        if (!hasMaterials) blockers.Add("Faltan materiales asignados.");
        if (sheet is null) blockers.Add("Falta ficha técnica.");
        else if (!hasApprovedTechnicalSheet) blockers.Add("La ficha técnica no está aprobada.");
        if (!hasCostSheet) blockers.Add("Falta hoja de costo.");
        else if (!hasApprovedCostSheet) blockers.Add("La hoja de costo no está aprobada.");

        var canAuthorize = blockers.Count == 0;
        authorization ??= new ProductAuthorizationRecord
        {
            Id = Guid.NewGuid(),
            TenantId = product.TenantId,
            CompanyId = product.CompanyId,
            FinishedProductId = finishedProductId,
            AuthorizationCode = $"AP-{product.Code}",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user
        };

        authorization.ProductTechnicalSheetId = sheet?.Id;
        authorization.ProductCostSheetId = cost?.Id;
        authorization.RequiresPhoto = true;
        authorization.RequiresConsumption = true;
        authorization.RequiresMaterialAssignment = true;
        authorization.RequiresCostSheet = true;
        authorization.HasPhoto = hasPhoto;
        authorization.HasConsumption = hasConsumption;
        authorization.HasMaterialAssignment = hasMaterials;
        authorization.HasCostSheet = hasCostSheet;
        authorization.Status = canAuthorize ? "authorized" : (autoAuthorize ? "pending" : authorization.Status);
        authorization.RejectionReason = string.Join(" ", blockers);
        authorization.Notes = canAuthorize ? "Autorización sincronizada y lista para explosión." : "Autorización sincronizada con bloqueos pendientes.";
        authorization.IsActive = true;
        authorization.UpdatedAt = DateTime.UtcNow;
        authorization.UpdatedBy = user;
        authorization.AuthorizedAtUtc = canAuthorize ? DateTime.UtcNow : null;
        authorization.AuthorizedBy = canAuthorize ? user : string.Empty;

        if (authorization.CreatedAt == default)
        {
            db.Set<ProductAuthorizationRecord>().Add(authorization);
        }
        else if (authorization.Id != Guid.Empty && db.Entry(authorization).State == EntityState.Detached)
        {
            db.Set<ProductAuthorizationRecord>().Add(authorization);
        }

        product.HasPhoto = hasPhoto;
        product.HasConsumptionDefinition = hasConsumption;
        product.HasMaterialAssignments = hasMaterials;
        product.IsAuthorizedForExplosion = canAuthorize;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = user;

        await db.SaveChangesAsync();
        var overview = (await BuildTechnicalCenterOverviewAsync(db, finishedProductId)).First();
        return new ProductTechnicalActionResponseDto(
            overview.FinishedProductId,
            overview.ProductCode,
            overview.ProductName,
            autoAuthorize ? "auto-authorize" : "sync-authorization",
            canAuthorize ? "authorized" : "pending",
            canAuthorize ? "Producto autorizado automáticamente por reglas." : string.IsNullOrWhiteSpace(overview.MissingItems) ? "Sincronización realizada." : $"Pendientes: {overview.MissingItems}",
            overview.IsAuthorizedForExplosion,
            overview.ReadinessPercent,
            overview.PrincipalBlocker);
    }

    private static async Task SyncFinishedProductStatusAsync(NanchesoftDbContext db, Guid finishedProductId, string user)
    {
        var product = await db.Set<FinishedProduct>().Include(x => x.ProductStyle).FirstOrDefaultAsync(x => x.Id == finishedProductId);
        if (product is null)
        {
            return;
        }

        var hasPhoto = product.HasPhoto
            || !string.IsNullOrWhiteSpace(product.ProductStyle?.PhotoUrl)
            || await db.Set<ProductTechnicalSheet>().AnyAsync(x => x.FinishedProductId == finishedProductId && !string.IsNullOrWhiteSpace(x.PhotoUrl));
        var hasLegacyConsumption = await db.Set<ProductConsumptionProfile>().AnyAsync(x => x.FinishedProductId == finishedProductId && x.IsActive);
        var hasTemplateConsumptionSync = product.ProductStyleId.HasValue && product.ProductSizeRunId.HasValue
            && await db.Set<ConsumptionTemplate>().AnyAsync(x => x.IsActive && x.IsAuthorized
                && x.ProductStyleId == product.ProductStyleId!.Value
                && x.ProductSizeRunId == product.ProductSizeRunId!.Value);
        var hasConsumption = hasLegacyConsumption || hasTemplateConsumptionSync;
        var hasMaterials = await db.Set<FinishedProductMaterial>().AnyAsync(x => x.FinishedProductId == finishedProductId && x.IsActive);
        var hasApprovedTechnicalSheet = await db.Set<ProductTechnicalSheet>().AnyAsync(x => x.FinishedProductId == finishedProductId && x.IsApproved);
        var hasApprovedCostSheet = await db.Set<ProductCostSheet>().AnyAsync(x => x.FinishedProductId == finishedProductId && x.IsApproved);
        var canAuthorize = hasPhoto && hasConsumption && hasMaterials && hasApprovedTechnicalSheet && hasApprovedCostSheet;

        product.HasPhoto = hasPhoto;
        product.HasConsumptionDefinition = hasConsumption;
        product.HasMaterialAssignments = hasMaterials;
        product.IsAuthorizedForExplosion = canAuthorize;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = user;

        await db.SaveChangesAsync();
    }

    private static string? ValidateTechnicalSheetRequest(ProductTechnicalSheetUpsertRequest request)
    {
        if (request.FinishedProductId == Guid.Empty)
        {
            return "FinishedProductId es obligatorio.";
        }
        if (string.IsNullOrWhiteSpace(request.SheetCode))
        {
            return "SheetCode es obligatorio.";
        }
        if (string.IsNullOrWhiteSpace(request.SheetName))
        {
            return "SheetName es obligatorio.";
        }
        return null;
    }

    private static void ReplaceTechnicalSheetChildren(ProductTechnicalSheet entity, ProductTechnicalSheetUpsertRequest request)
    {
        entity.Materials = request.Materials.Select((x, index) => new ProductTechnicalSheetMaterial
        {
            Id = Guid.NewGuid(),
            ProductTechnicalSheetId = entity.Id,
            MaterialItemId = x.MaterialItemId,
            ComponentCode = NormalizeText(x.ComponentCode),
            ComponentName = NormalizeText(x.ComponentName),
            MaterialCode = NormalizeText(x.MaterialCode),
            MaterialName = NormalizeText(x.MaterialName),
            UnitCode = NormalizeText(x.UnitCode),
            Quantity = x.Quantity,
            WastePercent = x.WastePercent,
            SortOrder = x.SortOrder == 0 ? index + 1 : x.SortOrder,
            ShowOnTechnicalSheet = x.ShowOnTechnicalSheet,
            Notes = NormalizeText(x.Notes)
        }).ToList();

        entity.Processes = request.Processes.Select((x, index) => new ProductTechnicalSheetProcess
        {
            Id = Guid.NewGuid(),
            ProductTechnicalSheetId = entity.Id,
            ProcessCode = NormalizeText(x.ProcessCode),
            ProcessName = NormalizeText(x.ProcessName),
            WorkstationCode = NormalizeText(x.WorkstationCode),
            DeliverToWarehouseCode = NormalizeText(x.DeliverToWarehouseCode),
            RequiresVoucherCard = x.RequiresVoucherCard,
            ShowMaterialsOnVoucher = x.ShowMaterialsOnVoucher,
            SortOrder = x.SortOrder == 0 ? index + 1 : x.SortOrder,
            Notes = NormalizeText(x.Notes)
        }).ToList();
    }

    private static ProductCostSheet ApplyCostSheet(ProductCostSheetUpsertRequest request, ProductCostSheet? existing)
    {
        var entity = existing ?? new ProductCostSheet { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, CreatedBy = request.UpdatedBy ?? "api" };
        entity.TenantId = request.TenantId;
        entity.CompanyId = request.CompanyId;
        entity.FinishedProductId = request.FinishedProductId;
        entity.ProductTechnicalSheetId = request.ProductTechnicalSheetId;
        entity.CostSheetCode = NormalizeCode(request.CostSheetCode);
        entity.Status = NormalizeStatus(request.Status, entity.Status);
        entity.DirectMaterialCost = request.DirectMaterialCost;
        entity.DirectLaborCost = request.DirectLaborCost;
        entity.IndirectManufacturingCost = request.IndirectManufacturingCost;
        entity.PackagingCost = request.PackagingCost;
        entity.ServiceCost = request.ServiceCost;
        entity.TotalCost = request.TotalCost <= 0
            ? Math.Round(request.DirectMaterialCost + request.DirectLaborCost + request.IndirectManufacturingCost + request.PackagingCost + request.ServiceCost, 4, MidpointRounding.AwayFromZero)
            : request.TotalCost;
        entity.TargetMarginPercent = request.TargetMarginPercent;
        entity.SuggestedSalePrice = request.SuggestedSalePrice <= 0
            ? Math.Round(entity.TotalCost * (1m + (request.TargetMarginPercent / 100m)), 4, MidpointRounding.AwayFromZero)
            : request.SuggestedSalePrice;
        entity.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "MXN" : NormalizeText(request.CurrencyCode);
        entity.Notes = NormalizeText(request.Notes);
        entity.IsApproved = request.IsApproved;
        entity.ApprovedAtUtc = request.IsApproved ? request.ApprovedAtUtc ?? entity.ApprovedAtUtc ?? DateTime.UtcNow : null;
        entity.ApprovedBy = request.IsApproved ? NormalizeText(request.ApprovedBy) : string.Empty;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy ?? "api";
        return entity;
    }

    private static ProductAuthorizationRecord ApplyAuthorization(ProductAuthorizationRecordUpsertRequest request, ProductAuthorizationRecord? existing)
    {
        var entity = existing ?? new ProductAuthorizationRecord { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, CreatedBy = request.UpdatedBy ?? "api" };
        entity.TenantId = request.TenantId;
        entity.CompanyId = request.CompanyId;
        entity.FinishedProductId = request.FinishedProductId;
        entity.ProductTechnicalSheetId = request.ProductTechnicalSheetId;
        entity.ProductCostSheetId = request.ProductCostSheetId;
        entity.AuthorizationCode = NormalizeCode(request.AuthorizationCode);
        entity.Status = NormalizeStatus(request.Status, entity.Status);
        entity.RequiresPhoto = request.RequiresPhoto;
        entity.RequiresConsumption = request.RequiresConsumption;
        entity.RequiresMaterialAssignment = request.RequiresMaterialAssignment;
        entity.RequiresCostSheet = request.RequiresCostSheet;
        entity.HasPhoto = request.HasPhoto;
        entity.HasConsumption = request.HasConsumption;
        entity.HasMaterialAssignment = request.HasMaterialAssignment;
        entity.HasCostSheet = request.HasCostSheet;
        entity.AuthorizedAtUtc = request.AuthorizedAtUtc;
        entity.AuthorizedBy = NormalizeText(request.AuthorizedBy);
        entity.RejectionReason = NormalizeText(request.RejectionReason);
        entity.Notes = NormalizeText(request.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy ?? "api";
        return entity;
    }

    private static string NormalizeCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    private static string NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeStatus(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static void MapSimpleCrud<TEntity, TDto, TRequest>(RouteGroupBuilder group, string route, Func<NanchesoftDbContext, IQueryable<TEntity>> queryFactory, Func<TEntity, TDto> map, Func<TRequest, TEntity?, TEntity> apply)
        where TEntity : class
    {
        group.MapGet($"/{route}", async (NanchesoftDbContext db) =>
        {
            var items = await queryFactory(db).ToListAsync();
            return Results.Ok(items.Select(map));
        });

        group.MapGet($"/{route}/{{id:guid}}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<TEntity>().FindAsync(id);
            return entity is null ? Results.NotFound() : Results.Ok(map(entity));
        });

        group.MapPost($"/{route}", async (TRequest request, NanchesoftDbContext db) =>
        {
            var entity = apply(request, null);
            db.Set<TEntity>().Add(entity);
            await db.SaveChangesAsync();
            var id = (Guid)entity!.GetType().GetProperty("Id")!.GetValue(entity)!;
            return Results.Created($"/api/products/{route}/{id}", map(entity));
        });

        group.MapPut($"/{route}/{{id:guid}}", async (Guid id, TRequest request, NanchesoftDbContext db) =>
        {
            var existing = await db.Set<TEntity>().FindAsync(id);
            if (existing is null) return Results.NotFound();
            var entity = apply(request, existing);
            await db.SaveChangesAsync();
            return Results.Ok(map(entity));
        });

        group.MapDelete($"/{route}/{{id:guid}}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<TEntity>().FindAsync(id);
            if (entity is null) return Results.NotFound();
            db.Set<TEntity>().Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static ProductTechnicalSheetDto MapTechnicalSheetDto(ProductTechnicalSheet x) => new(
        x.Id, x.TenantId, x.CompanyId, x.FinishedProductId, x.ProductStyleId, x.SheetCode, x.SheetName, x.Status, x.ProductDisplayName,
        x.PhotoUrl, x.MainMaterialName, x.MainColorName, x.SizeRunCode, x.Notes, x.IsApproved, x.ApprovedAtUtc, x.ApprovedBy,
        x.IsActive, x.CreatedAt, x.CreatedBy, x.UpdatedAt, x.UpdatedBy,
        x.Materials.OrderBy(m => m.SortOrder).Select(m => new ProductTechnicalSheetMaterialDto(m.Id, m.MaterialItemId, m.ComponentCode, m.ComponentName, m.MaterialCode, m.MaterialName, m.UnitCode, m.Quantity, m.WastePercent, m.SortOrder, m.ShowOnTechnicalSheet, m.Notes)).ToList(),
        x.Processes.OrderBy(p => p.SortOrder).Select(p => new ProductTechnicalSheetProcessDto(p.Id, p.ProcessCode, p.ProcessName, p.WorkstationCode, p.DeliverToWarehouseCode, p.RequiresVoucherCard, p.ShowMaterialsOnVoucher, p.SortOrder, p.Notes)).ToList());

    private static ProductCostSheetDto MapCostSheetDto(ProductCostSheet x) => new(
        x.Id, x.TenantId, x.CompanyId, x.FinishedProductId, x.ProductTechnicalSheetId, x.CostSheetCode, x.Status,
        x.DirectMaterialCost, x.DirectLaborCost, x.IndirectManufacturingCost, x.PackagingCost, x.ServiceCost, x.TotalCost,
        x.TargetMarginPercent, x.SuggestedSalePrice, x.CurrencyCode, x.Notes, x.IsApproved, x.ApprovedAtUtc, x.ApprovedBy,
        x.IsActive, x.CreatedAt, x.CreatedBy, x.UpdatedAt, x.UpdatedBy);

    private static ProductAuthorizationRecordDto MapAuthorizationDto(ProductAuthorizationRecord x) => new(
        x.Id, x.TenantId, x.CompanyId, x.FinishedProductId, x.ProductTechnicalSheetId, x.ProductCostSheetId, x.AuthorizationCode,
        x.Status, x.RequiresPhoto, x.RequiresConsumption, x.RequiresMaterialAssignment, x.RequiresCostSheet, x.HasPhoto,
        x.HasConsumption, x.HasMaterialAssignment, x.HasCostSheet, x.AuthorizedAtUtc, x.AuthorizedBy, x.RejectionReason,
        x.Notes, x.IsActive, x.CreatedAt, x.CreatedBy, x.UpdatedAt, x.UpdatedBy);

    private static ProductSizeConsumptionVariationDto MapVariationDto(ProductSizeConsumptionVariation x) => new(
        x.Id, x.TenantId, x.CompanyId, x.FinishedProductId, x.ProductComponentId, x.BaseSizeCode, x.TargetSizeCode,
        x.VariationPercent, x.QuantityDelta, x.AppliesToConsumption, x.AppliesToCosting, x.Notes, x.IsActive,
        x.CreatedAt, x.CreatedBy, x.UpdatedAt, x.UpdatedBy);

    private static async Task<List<EngineeringReadinessRowDto>> BuildEngineeringReadinessAsync(NanchesoftDbContext db)
    {
        var products = await db.Set<FinishedProduct>()
            .AsNoTracking()
            .Include(x => x.ProductStyle)
            .Include(x => x.ProductSizeRun)
            .Include(x => x.MainMaterialItem)
            .OrderBy(x => x.Code)
            .ToListAsync();

        if (products.Count == 0) return [];

        var productIds = products.Select(x => x.Id).ToHashSet();

        var materialCounts = await db.Set<FinishedProductMaterial>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId) && x.IsActive)
            .GroupBy(x => x.FinishedProductId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var legacyConsumptionCounts = await db.Set<ProductConsumptionProfile>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId) && x.IsActive)
            .GroupBy(x => x.FinishedProductId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var styleIds = products.Where(x => x.ProductStyleId.HasValue).Select(x => x.ProductStyleId!.Value).ToHashSet();
        var runIds = products.Where(x => x.ProductSizeRunId.HasValue).Select(x => x.ProductSizeRunId!.Value).ToHashSet();
        var authorizedTemplates = await db.Set<ConsumptionTemplate>().AsNoTracking()
            .Where(x => x.IsActive && x.IsAuthorized && styleIds.Contains(x.ProductStyleId) && runIds.Contains(x.ProductSizeRunId))
            .Select(x => new { x.ProductStyleId, x.ProductSizeRunId })
            .ToListAsync();
        var templateKeySet = authorizedTemplates.Select(x => (x.ProductStyleId, x.ProductSizeRunId)).ToHashSet();

        var technicalSheetCounts = await db.Set<ProductTechnicalSheet>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId))
            .GroupBy(x => x.FinishedProductId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var costSheetCounts = await db.Set<ProductCostSheet>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId))
            .GroupBy(x => x.FinishedProductId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var authCounts = await db.Set<ProductAuthorizationRecord>().AsNoTracking()
            .Where(x => productIds.Contains(x.FinishedProductId))
            .GroupBy(x => x.FinishedProductId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var rows = new List<EngineeringReadinessRowDto>();
        foreach (var p in products)
        {
            var legacyCount = legacyConsumptionCounts.TryGetValue(p.Id, out var lc) ? lc : 0;
            var hasTemplate = p.ProductStyleId.HasValue && p.ProductSizeRunId.HasValue
                && templateKeySet.Contains((p.ProductStyleId.Value, p.ProductSizeRunId.Value));
            var consumptionCount = legacyCount > 0 ? legacyCount : (hasTemplate ? 1 : 0);
            var matCount = materialCounts.TryGetValue(p.Id, out var mc) ? mc : 0;
            var sheetCount = technicalSheetCounts.TryGetValue(p.Id, out var sc) ? sc : 0;
            var costCount = costSheetCounts.TryGetValue(p.Id, out var cc) ? cc : 0;
            var authCount = authCounts.TryGetValue(p.Id, out var ac) ? ac : 0;

            var hasPhoto = p.HasPhoto;
            var hasConsumption = p.HasConsumptionDefinition || consumptionCount > 0;
            var hasMaterials = p.HasMaterialAssignments || matCount > 0;
            var hasTechSheet = sheetCount > 0;
            var hasCostSheet = costCount > 0;
            var isReady = p.IsAuthorizedForExplosion;

            var checks = new[] { hasPhoto, hasConsumption, hasMaterials, hasTechSheet, hasCostSheet, isReady };
            var readiness = (int)Math.Round(checks.Count(x => x) * 100m / checks.Length, 0);

            var missing = new List<string>();
            if (!hasPhoto) missing.Add("foto");
            if (!hasConsumption) missing.Add("consumos");
            if (!hasMaterials) missing.Add("materiales");
            if (!hasTechSheet) missing.Add("ficha técnica");
            if (!hasCostSheet) missing.Add("hoja de costo");
            if (!isReady) missing.Add("autorización");

            rows.Add(new EngineeringReadinessRowDto(
                p.Id, p.Code, p.Name ?? string.Empty,
                p.ProductStyle?.Name ?? string.Empty,
                p.ProductSizeRun?.Name ?? string.Empty,
                p.MainMaterialItem?.Name ?? string.Empty,
                hasPhoto, p.HasConsumptionDefinition, p.HasMaterialAssignments,
                p.IsAuthorizedForExplosion, p.IsActive,
                matCount, consumptionCount, sheetCount, costCount, authCount,
                readiness, string.Join(", ", missing), isReady));
        }

        return rows;
    }
}

public sealed record ProductTechnicalSheetUpsertRequest(
    Guid TenantId,
    Guid CompanyId,
    Guid FinishedProductId,
    Guid? ProductStyleId,
    string SheetCode,
    string SheetName,
    string Status,
    string ProductDisplayName,
    string PhotoUrl,
    string MainMaterialName,
    string MainColorName,
    string SizeRunCode,
    string Notes,
    bool IsApproved,
    DateTime? ApprovedAtUtc,
    string ApprovedBy,
    bool IsActive,
    string? UpdatedBy,
    List<ProductTechnicalSheetMaterialUpsertRequest> Materials,
    List<ProductTechnicalSheetProcessUpsertRequest> Processes);

public sealed record ProductTechnicalSheetMaterialUpsertRequest(
    Guid MaterialItemId,
    string ComponentCode,
    string ComponentName,
    string MaterialCode,
    string MaterialName,
    string UnitCode,
    decimal Quantity,
    decimal WastePercent,
    int SortOrder,
    bool ShowOnTechnicalSheet,
    string Notes);

public sealed record ProductTechnicalSheetProcessUpsertRequest(
    string ProcessCode,
    string ProcessName,
    string WorkstationCode,
    string DeliverToWarehouseCode,
    bool RequiresVoucherCard,
    bool ShowMaterialsOnVoucher,
    int SortOrder,
    string Notes);

public sealed record ProductCostSheetUpsertRequest(
    Guid TenantId,
    Guid CompanyId,
    Guid FinishedProductId,
    Guid? ProductTechnicalSheetId,
    string CostSheetCode,
    string Status,
    decimal DirectMaterialCost,
    decimal DirectLaborCost,
    decimal IndirectManufacturingCost,
    decimal PackagingCost,
    decimal ServiceCost,
    decimal TotalCost,
    decimal TargetMarginPercent,
    decimal SuggestedSalePrice,
    string CurrencyCode,
    string Notes,
    bool IsApproved,
    DateTime? ApprovedAtUtc,
    string ApprovedBy,
    bool IsActive,
    string? UpdatedBy);

public sealed record ProductAuthorizationRecordUpsertRequest(
    Guid TenantId,
    Guid CompanyId,
    Guid FinishedProductId,
    Guid? ProductTechnicalSheetId,
    Guid? ProductCostSheetId,
    string AuthorizationCode,
    string Status,
    bool RequiresPhoto,
    bool RequiresConsumption,
    bool RequiresMaterialAssignment,
    bool RequiresCostSheet,
    bool HasPhoto,
    bool HasConsumption,
    bool HasMaterialAssignment,
    bool HasCostSheet,
    DateTime? AuthorizedAtUtc,
    string AuthorizedBy,
    string RejectionReason,
    string Notes,
    bool IsActive,
    string? UpdatedBy);

public sealed record ProductSizeConsumptionVariationUpsertRequest(
    Guid TenantId,
    Guid CompanyId,
    Guid FinishedProductId,
    Guid? ProductComponentId,
    string BaseSizeCode,
    string TargetSizeCode,
    decimal VariationPercent,
    decimal QuantityDelta,
    bool AppliesToConsumption,
    bool AppliesToCosting,
    string Notes,
    bool IsActive,
    string? UpdatedBy);

public sealed record ProductTechnicalSheetDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    Guid FinishedProductId,
    Guid? ProductStyleId,
    string SheetCode,
    string SheetName,
    string Status,
    string ProductDisplayName,
    string PhotoUrl,
    string MainMaterialName,
    string MainColorName,
    string SizeRunCode,
    string Notes,
    bool IsApproved,
    DateTime? ApprovedAtUtc,
    string ApprovedBy,
    bool IsActive,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    List<ProductTechnicalSheetMaterialDto> Materials,
    List<ProductTechnicalSheetProcessDto> Processes);

public sealed record ProductTechnicalSheetMaterialDto(
    Guid Id,
    Guid MaterialItemId,
    string ComponentCode,
    string ComponentName,
    string MaterialCode,
    string MaterialName,
    string UnitCode,
    decimal Quantity,
    decimal WastePercent,
    int SortOrder,
    bool ShowOnTechnicalSheet,
    string Notes);

public sealed record ProductTechnicalSheetProcessDto(
    Guid Id,
    string ProcessCode,
    string ProcessName,
    string WorkstationCode,
    string DeliverToWarehouseCode,
    bool RequiresVoucherCard,
    bool ShowMaterialsOnVoucher,
    int SortOrder,
    string Notes);

public sealed record ProductCostSheetDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    Guid FinishedProductId,
    Guid? ProductTechnicalSheetId,
    string CostSheetCode,
    string Status,
    decimal DirectMaterialCost,
    decimal DirectLaborCost,
    decimal IndirectManufacturingCost,
    decimal PackagingCost,
    decimal ServiceCost,
    decimal TotalCost,
    decimal TargetMarginPercent,
    decimal SuggestedSalePrice,
    string CurrencyCode,
    string Notes,
    bool IsApproved,
    DateTime? ApprovedAtUtc,
    string ApprovedBy,
    bool IsActive,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public sealed record ProductAuthorizationRecordDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    Guid FinishedProductId,
    Guid? ProductTechnicalSheetId,
    Guid? ProductCostSheetId,
    string AuthorizationCode,
    string Status,
    bool RequiresPhoto,
    bool RequiresConsumption,
    bool RequiresMaterialAssignment,
    bool RequiresCostSheet,
    bool HasPhoto,
    bool HasConsumption,
    bool HasMaterialAssignment,
    bool HasCostSheet,
    DateTime? AuthorizedAtUtc,
    string AuthorizedBy,
    string RejectionReason,
    string Notes,
    bool IsActive,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public sealed record ProductSizeConsumptionVariationDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    Guid FinishedProductId,
    Guid? ProductComponentId,
    string BaseSizeCode,
    string TargetSizeCode,
    decimal VariationPercent,
    decimal QuantityDelta,
    bool AppliesToConsumption,
    bool AppliesToCosting,
    string Notes,
    bool IsActive,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public sealed record ProductTechnicalCenterProductOverviewDto(
    Guid FinishedProductId,
    Guid TenantId,
    Guid CompanyId,
    string ProductCode,
    string ProductName,
    string ProductStyleName,
    string SizeRunName,
    string MainMaterialName,
    bool HasPhoto,
    bool HasConsumption,
    bool HasMaterialAssignments,
    bool HasTechnicalSheet,
    bool HasApprovedTechnicalSheet,
    bool HasCostSheet,
    bool HasApprovedCostSheet,
    bool IsAuthorizedForExplosion,
    string AuthorizationStatus,
    string TechnicalSheetCode,
    string CostSheetCode,
    string AuthorizationCode,
    int MaterialAssignmentCount,
    int ConsumptionProfileCount,
    decimal TotalCost,
    decimal SuggestedSalePrice,
    int ReadinessPercent,
    string PrincipalBlocker,
    string MissingItems);

public sealed record ProductTechnicalActionResponseDto(
    Guid FinishedProductId,
    string ProductCode,
    string ProductName,
    string Action,
    string Status,
    string Message,
    bool IsAuthorizedForExplosion,
    int ReadinessPercent,
    string PrincipalBlocker);

public sealed record EngineeringReadinessRowDto(
    Guid FinishedProductId,
    string Code,
    string Name,
    string ProductStyleName,
    string ProductSizeRunName,
    string MainMaterialItemName,
    bool HasPhoto,
    bool HeaderHasConsumptionDefinition,
    bool HeaderHasMaterialAssignments,
    bool IsAuthorizedForExplosion,
    bool IsActive,
    int MaterialAssignmentsCount,
    int ConsumptionProfilesCount,
    int TechnicalSheetsCount,
    int CostSheetsCount,
    int AuthorizationRecordsCount,
    int ReadinessPercent,
    string MissingSteps,
    bool IsReadyForExplosion);
