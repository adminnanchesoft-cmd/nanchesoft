using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductTechnicalCenterEndpoints
{
    public static void MapProductTechnicalCenterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/technical-center").WithTags("Product Technical Center");

        group.MapGet("/overview", async (NanchesoftDbContext db) =>
        {
            var items = await BuildOverviewAsync(db);
            return Results.Ok(items);
        });

        group.MapGet("/overview/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var item = (await BuildOverviewAsync(db)).FirstOrDefault(x => x.FinishedProductId == finishedProductId);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/generate-sheet/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var product = await db.FinishedProducts
                .Include(x => x.ProductStyle)
                .Include(x => x.ProductSizeRun)
                .Include(x => x.ProductColor)
                .Include(x => x.MainMaterialItem)
                .FirstOrDefaultAsync(x => x.Id == finishedProductId);

            if (product is null)
                return Results.NotFound(new { message = "Finished product not found." });

            var assignments = await db.FinishedProductMaterials
                .Where(x => x.FinishedProductId == finishedProductId)
                .Include(x => x.ProductComponent)
                .Include(x => x.MaterialItem)
                    .ThenInclude(x => x!.IssueUnit)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            var existing = await db.ProductTechnicalSheets
                .Include(x => x.Materials)
                .Include(x => x.Processes)
                .FirstOrDefaultAsync(x => x.FinishedProductId == finishedProductId);

            if (existing is null)
            {
                existing = new ProductTechnicalSheet
                {
                    Id = Guid.NewGuid(),
                    TenantId = product.TenantId,
                    CompanyId = product.CompanyId,
                    FinishedProductId = product.Id,
                    ProductStyleId = product.ProductStyleId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "technical-center"
                };
                db.ProductTechnicalSheets.Add(existing);
            }
            else
            {
                db.ProductTechnicalSheetMaterials.RemoveRange(existing.Materials);
                db.ProductTechnicalSheetProcesses.RemoveRange(existing.Processes);
            }

            existing.SheetCode = string.IsNullOrWhiteSpace(existing.SheetCode) ? $"TS-{product.Code}" : existing.SheetCode;
            existing.SheetName = $"Ficha técnica {product.Code}";
            existing.Status = "draft";
            existing.ProductDisplayName = product.Name;
            existing.PhotoUrl = product.HasPhoto ? $"/products/photos/{product.Id:N}.jpg" : string.Empty;
            existing.MainMaterialName = product.MainMaterialItem?.Name ?? string.Empty;
            existing.MainColorName = product.ProductColor?.Name ?? string.Empty;
            existing.SizeRunCode = product.ProductSizeRun?.Code ?? string.Empty;
            existing.Notes = "Generada desde Centro Técnico.";
            existing.IsApproved = false;
            existing.ApprovedAtUtc = null;
            existing.ApprovedBy = string.Empty;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = "technical-center";

            existing.Materials = assignments.Select((x, index) => new ProductTechnicalSheetMaterial
            {
                Id = Guid.NewGuid(),
                ProductTechnicalSheetId = existing.Id,
                MaterialItemId = x.MaterialItemId,
                ComponentCode = x.ProductComponent?.Code ?? string.Empty,
                ComponentName = x.ProductComponent?.Name ?? string.Empty,
                MaterialCode = x.MaterialItem?.Code ?? string.Empty,
                MaterialName = x.MaterialItem?.Name ?? string.Empty,
                UnitCode = x.MaterialItem?.IssueUnit?.Code ?? x.MaterialItem?.IssueUnit?.Name ?? string.Empty,
                Quantity = x.Quantity,
                WastePercent = 0m,
                SortOrder = index + 1,
                ShowOnTechnicalSheet = true,
                Notes = x.Notes
            }).ToList();

            existing.Processes = assignments
                .Where(x => x.ProductComponent is not null)
                .Select(x => x.ProductComponent!)
                .GroupBy(x => new { x.Code, x.Name, x.ProductionPhase, x.WarehouseDeliveryRole, x.ShowOnProductionCard })
                .Select((g, index) => new ProductTechnicalSheetProcess
                {
                    Id = Guid.NewGuid(),
                    ProductTechnicalSheetId = existing.Id,
                    ProcessCode = g.Key.Code,
                    ProcessName = g.Key.Name,
                    WorkstationCode = g.Key.ProductionPhase,
                    DeliverToWarehouseCode = g.Key.WarehouseDeliveryRole,
                    RequiresVoucherCard = g.Key.ShowOnProductionCard,
                    ShowMaterialsOnVoucher = true,
                    SortOrder = index + 1,
                    Notes = string.Empty
                }).ToList();

            await db.SaveChangesAsync();
            var item = (await BuildOverviewAsync(db)).First(x => x.FinishedProductId == finishedProductId);
            return Results.Ok(item);
        });

        group.MapPost("/generate-cost/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var product = await db.FinishedProducts.FirstOrDefaultAsync(x => x.Id == finishedProductId);
            if (product is null)
                return Results.NotFound(new { message = "Finished product not found." });

            var sheet = await db.ProductTechnicalSheets.FirstOrDefaultAsync(x => x.FinishedProductId == finishedProductId);
            var assignments = await db.FinishedProductMaterials
                .Where(x => x.FinishedProductId == finishedProductId)
                .Include(x => x.MaterialItem)
                .ToListAsync();

            var directMaterial = assignments
                .Where(x => x.MaterialItem is not null && !x.MaterialItem.IsServiceItem)
                .Sum(x => x.Quantity * PickCost(x.MaterialItem!));

            var services = assignments
                .Where(x => x.MaterialItem is not null && x.MaterialItem.IsServiceItem)
                .Sum(x => x.Quantity * PickCost(x.MaterialItem!));

            var packaging = assignments
                .Where(x => x.ProductComponentId != Guid.Empty)
                .Where(x => (x.Notes ?? string.Empty).Contains("empaque", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Quantity * PickCost(x.MaterialItem!));

            var labor = Math.Round(directMaterial * 0.18m, 4);
            var indirect = Math.Round(directMaterial * 0.12m, 4);
            var total = Math.Round(directMaterial + services + packaging + labor + indirect, 4);
            var margin = 35m;
            var suggested = total <= 0 ? 0 : Math.Round(total / (1 - (margin / 100m)), 4);

            var cost = await db.ProductCostSheets.FirstOrDefaultAsync(x => x.FinishedProductId == finishedProductId);
            if (cost is null)
            {
                cost = new ProductCostSheet
                {
                    Id = Guid.NewGuid(),
                    TenantId = product.TenantId,
                    CompanyId = product.CompanyId,
                    FinishedProductId = product.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "technical-center"
                };
                db.ProductCostSheets.Add(cost);
            }

            cost.ProductTechnicalSheetId = sheet?.Id;
            cost.CostSheetCode = string.IsNullOrWhiteSpace(cost.CostSheetCode) ? $"CS-{product.Code}" : cost.CostSheetCode;
            cost.Status = total > 0 ? "draft" : "pending";
            cost.DirectMaterialCost = directMaterial;
            cost.DirectLaborCost = labor;
            cost.IndirectManufacturingCost = indirect;
            cost.PackagingCost = packaging;
            cost.ServiceCost = services;
            cost.TotalCost = total;
            cost.TargetMarginPercent = margin;
            cost.SuggestedSalePrice = suggested;
            cost.CurrencyCode = "MXN";
            cost.Notes = "Generada desde Centro Técnico.";
            cost.IsApproved = false;
            cost.ApprovedAtUtc = null;
            cost.ApprovedBy = string.Empty;
            cost.IsActive = true;
            cost.UpdatedAt = DateTime.UtcNow;
            cost.UpdatedBy = "technical-center";

            await db.SaveChangesAsync();
            var item = (await BuildOverviewAsync(db)).First(x => x.FinishedProductId == finishedProductId);
            return Results.Ok(item);
        });

        group.MapPost("/sync-authorization/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var product = await db.FinishedProducts.FirstOrDefaultAsync(x => x.Id == finishedProductId);
            if (product is null)
                return Results.NotFound(new { message = "Finished product not found." });

            var tech = await db.ProductTechnicalSheets.FirstOrDefaultAsync(x => x.FinishedProductId == finishedProductId);
            var cost = await db.ProductCostSheets.FirstOrDefaultAsync(x => x.FinishedProductId == finishedProductId);
            var auth = await db.ProductAuthorizationRecords.FirstOrDefaultAsync(x => x.FinishedProductId == finishedProductId);

            if (auth is null)
            {
                auth = new ProductAuthorizationRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = product.TenantId,
                    CompanyId = product.CompanyId,
                    FinishedProductId = product.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "technical-center"
                };
                db.ProductAuthorizationRecords.Add(auth);
            }

            auth.ProductTechnicalSheetId = tech?.Id;
            auth.ProductCostSheetId = cost?.Id;
            auth.AuthorizationCode = string.IsNullOrWhiteSpace(auth.AuthorizationCode) ? $"PA-{product.Code}" : auth.AuthorizationCode;
            auth.RequiresPhoto = true;
            auth.RequiresConsumption = true;
            auth.RequiresMaterialAssignment = true;
            auth.RequiresCostSheet = true;
            auth.HasPhoto = product.HasPhoto;
            auth.HasConsumption = product.HasConsumptionDefinition;
            auth.HasMaterialAssignment = product.HasMaterialAssignments;
            auth.HasCostSheet = cost is not null;

            var authorized = auth.HasPhoto && auth.HasConsumption && auth.HasMaterialAssignment && auth.HasCostSheet;
            auth.Status = authorized ? "authorized" : "pending";
            auth.AuthorizedAtUtc = authorized ? DateTime.UtcNow : null;
            auth.AuthorizedBy = authorized ? "technical-center" : string.Empty;
            auth.RejectionReason = authorized ? string.Empty : BuildMissingRequirements(product, tech, cost, auth);
            auth.Notes = "Sincronizada desde Centro Técnico.";
            auth.IsActive = true;
            auth.UpdatedAt = DateTime.UtcNow;
            auth.UpdatedBy = "technical-center";

            product.IsAuthorizedForExplosion = authorized;
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = "technical-center";

            await db.SaveChangesAsync();
            var item = (await BuildOverviewAsync(db)).First(x => x.FinishedProductId == finishedProductId);
            return Results.Ok(item);
        });
    }

    private static async Task<List<ProductTechnicalCenterOverviewItemDto>> BuildOverviewAsync(NanchesoftDbContext db)
    {
        var products = await db.FinishedProducts
            .AsNoTracking()
            .Include(x => x.ProductStyle)
            .Include(x => x.ProductSizeRun)
            .Include(x => x.MainMaterialItem)
            .OrderBy(x => x.Code)
            .ToListAsync();

        var sheets = await db.ProductTechnicalSheets.AsNoTracking().ToListAsync();
        var materialsBySheet = await db.ProductTechnicalSheetMaterials.AsNoTracking()
            .GroupBy(x => x.ProductTechnicalSheetId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        var processesBySheet = await db.ProductTechnicalSheetProcesses.AsNoTracking()
            .GroupBy(x => x.ProductTechnicalSheetId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        var costs = await db.ProductCostSheets.AsNoTracking().ToListAsync();
        var auths = await db.ProductAuthorizationRecords.AsNoTracking().ToListAsync();

        var rows = products.Select(product =>
        {
            var sheet = sheets.Where(x => x.FinishedProductId == product.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            var cost = costs.Where(x => x.FinishedProductId == product.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            var auth = auths.Where(x => x.FinishedProductId == product.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            var missing = BuildMissingRequirements(product, sheet, cost, auth);

            var readinessParts = new[]
            {
                product.HasPhoto,
                product.HasConsumptionDefinition,
                product.HasMaterialAssignments,
                sheet is not null,
                cost is not null,
                product.IsAuthorizedForExplosion
            };

            var readiness = (int)Math.Round(readinessParts.Count(x => x) * 100m / readinessParts.Length, 0);

            return new ProductTechnicalCenterOverviewItemDto(
                product.Id,
                product.Code,
                product.Name,
                product.ProductStyle?.Name ?? string.Empty,
                product.ProductSizeRun?.Name ?? string.Empty,
                product.MainMaterialItem?.Name ?? string.Empty,
                product.HasPhoto,
                product.HasConsumptionDefinition,
                product.HasMaterialAssignments,
                sheet?.SheetCode ?? string.Empty,
                sheet?.Status ?? "missing",
                sheet?.IsApproved ?? false,
                sheet is null ? 0 : materialsBySheet.GetValueOrDefault(sheet.Id),
                sheet is null ? 0 : processesBySheet.GetValueOrDefault(sheet.Id),
                cost?.CostSheetCode ?? string.Empty,
                cost?.Status ?? "missing",
                cost?.IsApproved ?? false,
                cost?.TotalCost ?? 0m,
                cost?.SuggestedSalePrice ?? 0m,
                auth?.AuthorizationCode ?? string.Empty,
                auth?.Status ?? "pending",
                product.IsAuthorizedForExplosion,
                readiness,
                missing);
        }).ToList();

        return rows;
    }

    private static decimal PickCost(MaterialItem material)
    {
        if (material.StandardCost > 0) return material.StandardCost;
        if (material.LastPurchaseCost > 0) return material.LastPurchaseCost;
        if (material.AuthorizedCost > 0) return material.AuthorizedCost;
        return 0m;
    }

    private static string BuildMissingRequirements(FinishedProduct product, ProductTechnicalSheet? sheet, ProductCostSheet? cost, ProductAuthorizationRecord? auth)
    {
        var missing = new List<string>();
        if (!product.HasPhoto) missing.Add("foto");
        if (!product.HasConsumptionDefinition) missing.Add("consumos");
        if (!product.HasMaterialAssignments) missing.Add("materiales asignados");
        if (sheet is null) missing.Add("ficha técnica");
        if (cost is null) missing.Add("hoja de costo");
        if (auth is not null && !string.IsNullOrWhiteSpace(auth.RejectionReason) && auth.Status != "authorized")
            missing.Add(auth.RejectionReason);
        return missing.Count == 0 ? "Completo" : string.Join(", ", missing.Distinct(StringComparer.OrdinalIgnoreCase));
    }
}

public sealed record ProductTechnicalCenterOverviewItemDto(
    Guid FinishedProductId,
    string ProductCode,
    string ProductName,
    string StyleName,
    string SizeRunName,
    string MainMaterialName,
    bool HasPhoto,
    bool HasConsumptionDefinition,
    bool HasMaterialAssignments,
    string TechnicalSheetCode,
    string TechnicalSheetStatus,
    bool TechnicalSheetApproved,
    int TechnicalSheetMaterialCount,
    int TechnicalSheetProcessCount,
    string CostSheetCode,
    string CostSheetStatus,
    bool CostSheetApproved,
    decimal TotalCost,
    decimal SuggestedPrice,
    string AuthorizationCode,
    string AuthorizationStatus,
    bool IsAuthorizedForExplosion,
    int ReadinessPercent,
    string MissingRequirements);
