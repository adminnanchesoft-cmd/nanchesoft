using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductGovernanceEndpoints
{
    public static void MapProductGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/technical-center").WithTags("Product Technical Center");

        group.MapGet("/overview", async (NanchesoftDbContext db) =>
        {
            var rows = await BuildSummaryAsync(db);

            return Results.Ok(new ProductTechnicalCenterOverviewDto(
                TotalProducts: rows.Count,
                AuthorizedProducts: rows.Count(x => x.IsAuthorizedForExplosion),
                ReadyForLaunchProducts: rows.Count(x => x.ReadinessPercent >= 100m),
                MissingTechnicalSheetProducts: rows.Count(x => !x.HasTechnicalSheet),
                MissingCostSheetProducts: rows.Count(x => !x.HasCostSheet),
                MissingPhotoProducts: rows.Count(x => !x.HasPhoto),
                AverageReadinessPercent: rows.Count == 0 ? 0m : Math.Round(rows.Average(x => x.ReadinessPercent), 2)));
        });

        group.MapGet("/summary", async (NanchesoftDbContext db) =>
        {
            var rows = await BuildSummaryAsync(db);
            return Results.Ok(rows.OrderByDescending(x => x.ReadinessPercent).ThenBy(x => x.ProductCode));
        });

        group.MapGet("/summary/{finishedProductId:guid}", async (Guid finishedProductId, NanchesoftDbContext db) =>
        {
            var row = (await BuildSummaryAsync(db)).FirstOrDefault(x => x.FinishedProductId == finishedProductId);
            return row is null ? Results.NotFound() : Results.Ok(row);
        });
    }

    private static async Task<List<ProductTechnicalCenterSummaryDto>> BuildSummaryAsync(NanchesoftDbContext db)
    {
        var finishedProducts = await db.Set<FinishedProduct>()
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync();

        if (finishedProducts.Count == 0)
        {
            return new List<ProductTechnicalCenterSummaryDto>();
        }

        var finishedProductIds = finishedProducts.Select(x => x.Id).ToHashSet();

        var technicalSheets = await db.Set<ProductTechnicalSheet>()
            .AsNoTracking()
            .Include(x => x.Materials)
            .Include(x => x.Processes)
            .Where(x => finishedProductIds.Contains(x.FinishedProductId))
            .ToListAsync();

        var costSheets = await db.Set<ProductCostSheet>()
            .AsNoTracking()
            .Where(x => finishedProductIds.Contains(x.FinishedProductId))
            .ToListAsync();

        var authorizations = await db.Set<ProductAuthorizationRecord>()
            .AsNoTracking()
            .Where(x => finishedProductIds.Contains(x.FinishedProductId))
            .ToListAsync();

        var productMaterials = await db.Set<FinishedProductMaterial>()
            .AsNoTracking()
            .Where(x => finishedProductIds.Contains(x.FinishedProductId) && x.IsActive)
            .ToListAsync();

        // Legacy consumption profiles (by finished product)
        var legacyConsumptions = await db.Set<ProductConsumptionProfile>()
            .AsNoTracking()
            .Where(x => finishedProductIds.Contains(x.FinishedProductId) && x.IsActive)
            .Select(x => new { x.FinishedProductId })
            .ToListAsync();

        // New consumption templates (by style + size run)
        var styleRunPairs = finishedProducts
            .Where(x => x.ProductStyleId.HasValue && x.ProductSizeRunId.HasValue)
            .Select(x => new { StyleId = x.ProductStyleId!.Value, RunId = x.ProductSizeRunId!.Value })
            .Distinct()
            .ToList();

        var styleIds = styleRunPairs.Select(x => x.StyleId).ToHashSet();
        var runIds = styleRunPairs.Select(x => x.RunId).ToHashSet();

        var activeTemplates = await db.Set<ConsumptionTemplate>()
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsAuthorized && styleIds.Contains(x.ProductStyleId) && runIds.Contains(x.ProductSizeRunId))
            .Select(x => new { x.ProductStyleId, x.ProductSizeRunId })
            .ToListAsync();

        var authorizedTemplateKeys = activeTemplates
            .Select(x => (x.ProductStyleId, x.ProductSizeRunId))
            .ToHashSet();

        var components = await db.Set<ProductComponent>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync();

        var componentIds = components.Select(x => x.Id).ToHashSet();
        productMaterials = productMaterials.Where(x => componentIds.Contains(x.ProductComponentId)).ToList();

        var rows = new List<ProductTechnicalCenterSummaryDto>();

        foreach (var product in finishedProducts)
        {
            var technicalSheet = technicalSheets
                .Where(x => x.FinishedProductId == product.Id)
                .OrderByDescending(x => x.IsApproved)
                .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefault();

            var costSheet = costSheets
                .Where(x => x.FinishedProductId == product.Id)
                .OrderByDescending(x => x.IsApproved)
                .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefault();

            var authorization = authorizations
                .Where(x => x.FinishedProductId == product.Id)
                .OrderByDescending(x => x.AuthorizedAtUtc ?? x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefault();

            var assignedMaterialCount = productMaterials.Count(x => x.FinishedProductId == product.Id);

            // Has consumption = legacy profiles OR authorized consumption template for this style+run
            var hasLegacyConsumption = legacyConsumptions.Any(x => x.FinishedProductId == product.Id);
            var hasTemplateConsumption = product.ProductStyleId.HasValue && product.ProductSizeRunId.HasValue
                && authorizedTemplateKeys.Contains((product.ProductStyleId.Value, product.ProductSizeRunId.Value));
            var consumptionLineCount = hasLegacyConsumption ? legacyConsumptions.Count(x => x.FinishedProductId == product.Id) : (hasTemplateConsumption ? 1 : 0);
            var technicalSheetMaterialCount = technicalSheet?.Materials.Count ?? 0;
            var technicalSheetProcessCount = technicalSheet?.Processes.Count ?? 0;

            var hasPhoto = product.HasPhoto || !string.IsNullOrWhiteSpace(technicalSheet?.PhotoUrl) || authorization?.HasPhoto == true;
            var hasConsumption = product.HasConsumptionDefinition || hasLegacyConsumption || hasTemplateConsumption || authorization?.HasConsumption == true;
            var hasMaterialAssignment = product.HasMaterialAssignments || assignedMaterialCount > 0 || authorization?.HasMaterialAssignment == true;
            var hasTechnicalSheet = technicalSheet is not null;
            var hasCostSheet = costSheet is not null || authorization?.HasCostSheet == true;

            var checks = new[]
            {
                hasPhoto,
                hasConsumption,
                hasMaterialAssignment,
                hasTechnicalSheet,
                hasCostSheet
            };

            var readinessPercent = checks.Length == 0
                ? 0m
                : Math.Round((decimal)checks.Count(x => x) * 100m / checks.Length, 2);

            var missingRequirements = new List<string>();
            if (!hasPhoto) missingRequirements.Add("Foto");
            if (!hasConsumption) missingRequirements.Add("Consumos");
            if (!hasMaterialAssignment) missingRequirements.Add("Materiales asignados");
            if (!hasTechnicalSheet) missingRequirements.Add("Ficha técnica");
            if (!hasCostSheet) missingRequirements.Add("Hoja de costo");

            rows.Add(new ProductTechnicalCenterSummaryDto(
                FinishedProductId: product.Id,
                ProductCode: product.Code,
                ProductName: product.Name,
                BillingName: product.BillingName,
                HasPhoto: hasPhoto,
                HasConsumptionDefinition: hasConsumption,
                HasMaterialAssignments: hasMaterialAssignment,
                HasTechnicalSheet: hasTechnicalSheet,
                HasCostSheet: hasCostSheet,
                TechnicalSheetApproved: technicalSheet?.IsApproved ?? false,
                CostSheetApproved: costSheet?.IsApproved ?? false,
                IsAuthorizedForExplosion: product.IsAuthorizedForExplosion || string.Equals(authorization?.Status, "authorized", StringComparison.OrdinalIgnoreCase),
                ReadinessPercent: readinessPercent,
                AssignedMaterialCount: assignedMaterialCount,
                ConsumptionLineCount: consumptionLineCount,
                TechnicalSheetMaterialCount: technicalSheetMaterialCount,
                TechnicalSheetProcessCount: technicalSheetProcessCount,
                TechnicalSheetCode: technicalSheet?.SheetCode ?? string.Empty,
                CostSheetCode: costSheet?.CostSheetCode ?? string.Empty,
                AuthorizationCode: authorization?.AuthorizationCode ?? string.Empty,
                AuthorizationStatus: authorization?.Status ?? string.Empty,
                TotalCost: costSheet?.TotalCost ?? 0m,
                SuggestedSalePrice: costSheet?.SuggestedSalePrice ?? 0m,
                MarginPercent: costSheet?.TargetMarginPercent ?? 0m,
                MissingRequirements: missingRequirements,
                PrimaryBlocker: missingRequirements.FirstOrDefault() ?? string.Empty,
                LastUpdatedAt: new[]
                {
                    product.UpdatedAt ?? product.CreatedAt,
                    technicalSheet?.UpdatedAt ?? technicalSheet?.CreatedAt,
                    costSheet?.UpdatedAt ?? costSheet?.CreatedAt,
                    authorization?.UpdatedAt ?? authorization?.CreatedAt
                }.Where(x => x.HasValue).Max() ?? product.CreatedAt));
        }

        return rows;
    }
}

public sealed record ProductTechnicalCenterOverviewDto(
    int TotalProducts,
    int AuthorizedProducts,
    int ReadyForLaunchProducts,
    int MissingTechnicalSheetProducts,
    int MissingCostSheetProducts,
    int MissingPhotoProducts,
    decimal AverageReadinessPercent);

public sealed record ProductTechnicalCenterSummaryDto(
    Guid FinishedProductId,
    string ProductCode,
    string ProductName,
    string BillingName,
    bool HasPhoto,
    bool HasConsumptionDefinition,
    bool HasMaterialAssignments,
    bool HasTechnicalSheet,
    bool HasCostSheet,
    bool TechnicalSheetApproved,
    bool CostSheetApproved,
    bool IsAuthorizedForExplosion,
    decimal ReadinessPercent,
    int AssignedMaterialCount,
    int ConsumptionLineCount,
    int TechnicalSheetMaterialCount,
    int TechnicalSheetProcessCount,
    string TechnicalSheetCode,
    string CostSheetCode,
    string AuthorizationCode,
    string AuthorizationStatus,
    decimal TotalCost,
    decimal SuggestedSalePrice,
    decimal MarginPercent,
    List<string> MissingRequirements,
    string PrimaryBlocker,
    DateTime LastUpdatedAt);
