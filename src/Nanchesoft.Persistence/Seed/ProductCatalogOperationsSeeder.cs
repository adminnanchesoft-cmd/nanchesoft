using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class ProductCatalogOperationsSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null) return;

        var unit = await dbContext.Units.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var supplier = await dbContext.Suppliers.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var style = await dbContext.ProductStyles.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var line = await dbContext.ProductLines.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var last = await dbContext.ProductLasts.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var sizeRun = await dbContext.ProductSizeRuns.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

        var directFamily = await dbContext.MaterialFamilies.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "LEATHER");
        if (directFamily is null)
        {
            directFamily = new MaterialFamily
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                Code = "LEATHER",
                Name = "Leather and Synthetic",
                InventoryGroup = "Direct Material",
                Notes = "Orange migrated example",
                CreatedBy = "seed"
            };
            dbContext.MaterialFamilies.Add(directFamily);
            await dbContext.SaveChangesAsync();
        }

        var upperSubfamily = await dbContext.MaterialSubfamilies.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.MaterialFamilyId == directFamily.Id && x.Code == "UPPER");
        if (upperSubfamily is null)
        {
            upperSubfamily = new MaterialSubfamily
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                MaterialFamilyId = directFamily.Id,
                Code = "UPPER",
                Name = "Upper Materials",
                MaterialType = "direct",
                IsDirectMaterial = true,
                CreatedBy = "seed"
            };
            dbContext.MaterialSubfamilies.Add(upperSubfamily);
            await dbContext.SaveChangesAsync();
        }

        var notApplicableFamily = await dbContext.MaterialFamilies.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "GENERAL");
        if (notApplicableFamily is null)
        {
            notApplicableFamily = new MaterialFamily
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                Code = "GENERAL",
                Name = "General and Support",
                InventoryGroup = "Support",
                Notes = "Seeded to support NO APLICA assignments from Orange.",
                CreatedBy = "seed"
            };
            dbContext.MaterialFamilies.Add(notApplicableFamily);
            await dbContext.SaveChangesAsync();
        }

        var notApplicableSubfamily = await dbContext.MaterialSubfamilies.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.MaterialFamilyId == notApplicableFamily.Id && x.Code == "NA");
        if (notApplicableSubfamily is null)
        {
            notApplicableSubfamily = new MaterialSubfamily
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                MaterialFamilyId = notApplicableFamily.Id,
                Code = "NA",
                Name = "No aplica",
                MaterialType = "indirect",
                IsDirectMaterial = false,
                Notes = "Special material used when a component does not consume a physical material.",
                CreatedBy = "seed"
            };
            dbContext.MaterialSubfamilies.Add(notApplicableSubfamily);
            await dbContext.SaveChangesAsync();
        }

        var mainMaterial = await dbContext.MaterialItems.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "MAT-LEA-001");
        if (mainMaterial is null)
        {
            mainMaterial = new MaterialItem
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                MaterialSubfamilyId = upperSubfamily.Id,
                PurchaseUnitId = unit?.Id,
                IssueUnitId = unit?.Id,
                SupplierId = supplier?.Id,
                Code = "MAT-LEA-001",
                Name = "Cow Leather Black",
                Description = "Example migrated material",
                AuthorizedCost = 120m,
                LastPurchaseCost = 118m,
                StandardCost = 120m,
                CostStatus = "authorized",
                CreatedBy = "seed"
            };
            dbContext.MaterialItems.Add(mainMaterial);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.MaterialItems.AnyAsync(x => x.CompanyId == company.Id && x.Code == "NO-APLICA"))
        {
            dbContext.MaterialItems.Add(new MaterialItem
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                MaterialSubfamilyId = notApplicableSubfamily.Id,
                PurchaseUnitId = unit?.Id,
                IssueUnitId = unit?.Id,
                Code = "NO-APLICA",
                Name = "NO APLICA",
                Description = "Special placeholder material for components without direct material assignment.",
                LegacyMaterialName = "NO APLICA",
                CostStatus = "authorized",
                CreatedBy = "seed"
            });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.ProductComponents.AnyAsync())
        {
            dbContext.ProductComponents.AddRange(
                new ProductComponent { TenantId = company.TenantId, CompanyId = company.Id, ConsumptionUnitId = unit?.Id, Code = "UPPER", Name = "Upper", DefaultConsumption = 1m, ActivateForAllProducts = true, ShowOnProductionCard = true, CreatedBy = "seed" },
                new ProductComponent { TenantId = company.TenantId, CompanyId = company.Id, ConsumptionUnitId = unit?.Id, Code = "SOLE", Name = "Sole", DefaultConsumption = 1m, ActivateForAllProducts = true, ShowOnProductionCard = true, CreatedBy = "seed" },
                new ProductComponent { TenantId = company.TenantId, CompanyId = company.Id, ConsumptionUnitId = unit?.Id, Code = "BOX", Name = "Box", DefaultConsumption = 1m, ActivateForAllProducts = true, ShowOnProductionCard = true, CreatedBy = "seed" }
            );
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.FinishedProducts.AnyAsync())
        {
            var product = new FinishedProduct
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                ProductStyleId = style?.Id,
                ProductSizeRunId = sizeRun?.Id,
                ProductLineId = line?.Id,
                ProductLastId = last?.Id,
                MainMaterialItemId = mainMaterial?.Id,
                Code = "FP-0001",
                Name = "Sample Finished Product",
                BillingName = "Sample Finished Product",
                HasPhoto = true,
                HasConsumptionDefinition = true,
                HasMaterialAssignments = true,
                IsAuthorizedForExplosion = true,
                Notes = "Orange example migrated to Nanchesoft",
                CreatedBy = "seed"
            };
            dbContext.FinishedProducts.Add(product);
            await dbContext.SaveChangesAsync();

            var component = await dbContext.ProductComponents.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
            if (component is not null && mainMaterial is not null)
            {
                dbContext.FinishedProductMaterials.Add(new FinishedProductMaterial
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    FinishedProductId = product.Id,
                    ProductComponentId = component.Id,
                    MaterialItemId = mainMaterial.Id,
                    SizeCode = "ALL",
                    Quantity = 1m,
                    CreatedBy = "seed"
                });

                dbContext.ProductConsumptionProfiles.Add(new ProductConsumptionProfile
                {
                    TenantId = company.TenantId,
                    CompanyId = company.Id,
                    FinishedProductId = product.Id,
                    ProductComponentId = component.Id,
                    SizeCode = "ALL",
                    Pieces = 2,
                    Consumption = 1m,
                    Status = "authorized",
                    CreatedBy = "seed"
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
