using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class ProductTechnicalCostingSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        var tenantId = await dbContext.Tenants.AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync();
        var companyId = await dbContext.Companies.AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync();
        var productId = await dbContext.Set<FinishedProduct>().AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync();
        var styleId = await dbContext.Set<ProductStyle>().AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync();
        var componentId = await dbContext.Set<ProductComponent>().AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync();
        var materialId = await dbContext.Set<MaterialItem>().AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync();

        if (tenantId == Guid.Empty || companyId == Guid.Empty || productId == Guid.Empty)
        {
            return;
        }

        if (!await dbContext.Set<ProductTechnicalSheet>().AnyAsync())
        {
            var sheetId = Guid.NewGuid();
            dbContext.Set<ProductTechnicalSheet>().Add(new ProductTechnicalSheet
            {
                Id = sheetId,
                TenantId = tenantId,
                CompanyId = companyId,
                FinishedProductId = productId,
                ProductStyleId = styleId == Guid.Empty ? null : styleId,
                SheetCode = "TS-0001",
                SheetName = "Default technical sheet",
                Status = "approved",
                ProductDisplayName = "Sample finished product",
                PhotoUrl = string.Empty,
                MainMaterialName = "Primary material",
                MainColorName = "Black",
                SizeRunCode = "U",
                Notes = "Base technical sheet created from Orange migration starter block.",
                IsApproved = true,
                ApprovedAtUtc = DateTime.UtcNow,
                ApprovedBy = "system",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            });

            if (materialId != Guid.Empty)
            {
                dbContext.Set<ProductTechnicalSheetMaterial>().Add(new ProductTechnicalSheetMaterial
                {
                    Id = Guid.NewGuid(),
                    ProductTechnicalSheetId = sheetId,
                    MaterialItemId = materialId,
                    ComponentCode = "UPPER",
                    ComponentName = "Upper",
                    MaterialCode = "MAT-001",
                    MaterialName = "Primary material",
                    UnitCode = "PAIR",
                    Quantity = 1,
                    WastePercent = 0,
                    SortOrder = 1,
                    ShowOnTechnicalSheet = true,
                    Notes = string.Empty
                });
            }

            dbContext.Set<ProductTechnicalSheetProcess>().AddRange(
                new ProductTechnicalSheetProcess
                {
                    Id = Guid.NewGuid(),
                    ProductTechnicalSheetId = sheetId,
                    ProcessCode = "CUT",
                    ProcessName = "Cutting",
                    WorkstationCode = "CUT-01",
                    DeliverToWarehouseCode = "RAW",
                    RequiresVoucherCard = true,
                    ShowMaterialsOnVoucher = true,
                    SortOrder = 1,
                    Notes = string.Empty
                },
                new ProductTechnicalSheetProcess
                {
                    Id = Guid.NewGuid(),
                    ProductTechnicalSheetId = sheetId,
                    ProcessCode = "SEW",
                    ProcessName = "Sewing",
                    WorkstationCode = "SEW-01",
                    DeliverToWarehouseCode = "WIP",
                    RequiresVoucherCard = true,
                    ShowMaterialsOnVoucher = false,
                    SortOrder = 2,
                    Notes = string.Empty
                });
        }

        if (!await dbContext.Set<ProductCostSheet>().AnyAsync())
        {
            dbContext.Set<ProductCostSheet>().Add(new ProductCostSheet
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CompanyId = companyId,
                FinishedProductId = productId,
                CostSheetCode = "CS-0001",
                Status = "approved",
                DirectMaterialCost = 110.0000m,
                DirectLaborCost = 38.5000m,
                IndirectManufacturingCost = 21.0000m,
                PackagingCost = 8.0000m,
                ServiceCost = 5.5000m,
                TotalCost = 183.0000m,
                TargetMarginPercent = 35.0000m,
                SuggestedSalePrice = 247.0500m,
                CurrencyCode = "MXN",
                Notes = "Starter cost sheet for Orange migration.",
                IsApproved = true,
                ApprovedAtUtc = DateTime.UtcNow,
                ApprovedBy = "system",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            });
        }

        if (!await dbContext.Set<ProductAuthorizationRecord>().AnyAsync())
        {
            dbContext.Set<ProductAuthorizationRecord>().Add(new ProductAuthorizationRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CompanyId = companyId,
                FinishedProductId = productId,
                AuthorizationCode = "PA-0001",
                Status = "authorized",
                RequiresPhoto = true,
                RequiresConsumption = true,
                RequiresMaterialAssignment = true,
                RequiresCostSheet = true,
                HasPhoto = false,
                HasConsumption = true,
                HasMaterialAssignment = true,
                HasCostSheet = true,
                AuthorizedAtUtc = DateTime.UtcNow,
                AuthorizedBy = "system",
                RejectionReason = string.Empty,
                Notes = "Base authorization created as reference.",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            });
        }

        if (!await dbContext.Set<ProductSizeConsumptionVariation>().AnyAsync())
        {
            dbContext.Set<ProductSizeConsumptionVariation>().AddRange(
                new ProductSizeConsumptionVariation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CompanyId = companyId,
                    FinishedProductId = productId,
                    ProductComponentId = componentId == Guid.Empty ? null : componentId,
                    BaseSizeCode = "26",
                    TargetSizeCode = "27",
                    VariationPercent = 1.5000m,
                    QuantityDelta = 0.015000m,
                    AppliesToConsumption = true,
                    AppliesToCosting = true,
                    Notes = "Sample upward size variation.",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new ProductSizeConsumptionVariation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CompanyId = companyId,
                    FinishedProductId = productId,
                    ProductComponentId = componentId == Guid.Empty ? null : componentId,
                    BaseSizeCode = "26",
                    TargetSizeCode = "25",
                    VariationPercent = -1.5000m,
                    QuantityDelta = -0.015000m,
                    AppliesToConsumption = true,
                    AppliesToCosting = true,
                    Notes = "Sample downward size variation.",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                });
        }

        await dbContext.SaveChangesAsync();
    }
}
