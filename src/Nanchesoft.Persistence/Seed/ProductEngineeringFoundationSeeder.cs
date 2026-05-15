using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class ProductEngineeringFoundationSeeder
{
    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed-orange-core-08a";

        var tenant = await dbContext.Tenants.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var company = await dbContext.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (tenant is null || company is null)
            return;

        var pieceUnit = await dbContext.Units.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Code == "PZA")
            ?? await dbContext.Units.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        var serviceUnit = await dbContext.Units.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Code == "S");

        if (serviceUnit is null)
        {
            serviceUnit = new Unit
            {
                TenantId = tenant.Id,
                Code = "S",
                Name = "Service Unit",
                Abbreviation = "S",
                CreatedBy = seedUser
            };
            dbContext.Units.Add(serviceUnit);
            await dbContext.SaveChangesAsync();
        }

        var uniqueRun = await dbContext.ProductSizeRuns.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "U");
        if (uniqueRun is null)
        {
            uniqueRun = new ProductSizeRun
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "U",
                Name = "Unique Size Run",
                DisplayName = "Unique Size",
                IsUniqueSizeRun = true,
                SizeCount = 1,
                CreatedBy = seedUser
            };
            dbContext.ProductSizeRuns.Add(uniqueRun);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.ProductSizeRunSizes.AnyAsync(x => x.ProductSizeRunId == uniqueRun.Id))
        {
            dbContext.ProductSizeRunSizes.Add(new ProductSizeRunSize
            {
                ProductSizeRunId = uniqueRun.Id,
                Sequence = 1,
                SizeCode = "U",
                DisplayLabel = "U",
                BarcodeLabel = "U",
                CreatedBy = seedUser
            });
            await dbContext.SaveChangesAsync();
        }

        var family = await dbContext.ProductFamilies.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "FOOTWEAR");
        if (family is null)
        {
            family = new ProductFamily
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "FOOTWEAR",
                Name = "Footwear",
                StatisticsGroup = "Finished Goods",
                IsFinishedProductFamily = true,
                CreatedBy = seedUser
            };
            dbContext.ProductFamilies.Add(family);
            await dbContext.SaveChangesAsync();
        }

        var productLast = await dbContext.ProductLasts.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "CASUAL-01");
        if (productLast is null)
        {
            productLast = new ProductLast
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "CASUAL-01",
                Name = "Casual Last 01",
                WidthReference = "STANDARD",
                CreatedBy = seedUser
            };
            dbContext.ProductLasts.Add(productLast);
            await dbContext.SaveChangesAsync();
        }

        var line = await dbContext.ProductLines.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "CASUAL");
        if (line is null)
        {
            line = new ProductLine
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                ProductFamilyId = family.Id,
                ProductLastId = productLast.Id,
                Code = "CASUAL",
                Name = "Casual Line",
                ShortName = "CAS",
                AllowsDiscount = true,
                CreatedBy = seedUser
            };
            dbContext.ProductLines.Add(line);
            await dbContext.SaveChangesAsync();
        }

        var style = await dbContext.ProductStyles.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "209");
        if (style is null)
        {
            style = new ProductStyle
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                ProductLineId = line.Id,
                ProductLastId = productLast.Id,
                Code = "209",
                Name = "Style 209",
                CustomerLabel1 = "STYLE 209",
                CustomerLabel2 = "BASIC CASUAL",
                ColorLabel = "MAIN COLOR",
                DieCutReference = "DIE-209",
                MaxLotSize = 240,
                HasAuthorizedConsumption = false,
                HandlesFractionsByStyle = true,
                TechnicalNotes = "Base style migrated from legacy Orange/Silvasoft naming conventions.",
                ProductionCardNotes = "Technical card data will be completed in the next Orange engineering package.",
                OutsourcedProcessName = "",
                PhotoUrl = "",
                CreatedBy = seedUser
            };
            dbContext.ProductStyles.Add(style);
            await dbContext.SaveChangesAsync();
        }

        var embroidery = await dbContext.EmbroideryPatterns.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "NONE");
        if (embroidery is null)
        {
            embroidery = new EmbroideryPattern
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "NONE",
                Name = "No Embroidery",
                Sequence = 1,
                CreatedBy = seedUser
            };
            dbContext.EmbroideryPatterns.Add(embroidery);
            await dbContext.SaveChangesAsync();
        }

        var category = await dbContext.ItemCategories.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.CompanyId == company.Id)
            ?? new ItemCategory
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = "PROD",
                Name = "Products",
                CreatedBy = seedUser
            };
        if (category.Id == Guid.Empty || category.CompanyId == Guid.Empty)
        {
            dbContext.ItemCategories.Add(category);
            await dbContext.SaveChangesAsync();
        }

        var item = await dbContext.Items.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "SHOE-209-U");
        if (item is null)
        {
            item = new Item
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                CategoryId = category.Id,
                UnitId = pieceUnit?.Id,
                Code = "SHOE-209-U",
                Name = "Style 209 Unique Size",
                Description = "Enterprise sample product created for Orange product engineering migration.",
                ItemType = "product",
                BasePrice = 0m,
                BaseCost = 0m,
                ManagesInventory = true,
                UsesLots = false,
                UsesSerials = false,
                IsSaleItem = true,
                IsPurchaseItem = false,
                CreatedBy = seedUser
            };
            dbContext.Items.Add(item);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.ItemEngineeringProfiles.AnyAsync(x => x.ItemId == item.Id))
        {
            dbContext.ItemEngineeringProfiles.Add(new ItemEngineeringProfile
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                ItemId = item.Id,
                ProductStyleId = style.Id,
                ProductSizeRunId = uniqueRun.Id,
                EmbroideryPatternId = embroidery.Id,
                FolioPattern = "STANDARD",
                TechnicalSheetMode = "style",
                ProcessVoucherProfile = "DEFAULT",
                TechnicalSheetNotes = "Initial engineering profile created from Orange/Silvasoft migration package 08A.",
                ProductionCardNotes = "Pending BOM, process vouchers and cost sheet structure in next deliveries.",
                HasPhoto = false,
                HasConsumptionDefinition = false,
                HasMaterialAssignments = false,
                IsAuthorizedForExplosion = false,
                CreatedBy = seedUser
            });
        }

        if (pieceUnit is not null && !await dbContext.UnitConversions.AnyAsync(x => x.CompanyId == company.Id && x.FromUnitId == pieceUnit.Id && x.ToUnitId == pieceUnit.Id))
        {
            dbContext.UnitConversions.Add(new UnitConversion
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                FromUnitId = pieceUnit.Id,
                ToUnitId = pieceUnit.Id,
                ConversionFactor = 1m,
                IsBidirectional = true,
                Notes = "Identity conversion.",
                CreatedBy = seedUser
            });
        }

        if (serviceUnit is not null && !await dbContext.UnitConversions.AnyAsync(x => x.CompanyId == company.Id && x.FromUnitId == serviceUnit.Id && x.ToUnitId == serviceUnit.Id))
        {
            dbContext.UnitConversions.Add(new UnitConversion
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                FromUnitId = serviceUnit.Id,
                ToUnitId = serviceUnit.Id,
                ConversionFactor = 1m,
                IsBidirectional = true,
                Notes = "Service unit identity conversion.",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
