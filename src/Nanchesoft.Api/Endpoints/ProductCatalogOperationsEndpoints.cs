using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductCatalogOperationsEndpoints
{
    public static void MapProductCatalogOperations(IEndpointRouteBuilder app)
    {
        MapProductionPhases(app);
        MapMaterialCharacteristics(app);
        MapMaterialSizes(app);
        MapMaterialFamilies(app);
        MapMaterialSubfamilies(app);
        MapMaterialItems(app);
        MapFinishedProducts(app);
        MapProductComponents(app);
        MapFinishedProductMaterials(app);
        MapProductConsumptionProfiles(app);
        MapFinishedProductSupplies(app);
        MapMaterialSizeDistributions(app);
        MapLookups(app);
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(NanchesoftDbContext db, HttpContext httpContext)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        if (companyId.HasValue)
        {
            var comp = await db.Companies.AsNoTracking()
                .Where(x => x.Id == companyId.Value)
                .Select(x => new { x.Id, x.TenantId })
                .FirstOrDefaultAsync();
            if (comp is not null) return (comp.TenantId, comp.Id);
        }
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        if (tenantId.HasValue)
        {
            var comp = await db.Companies.AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new { x.Id, x.TenantId })
                .FirstOrDefaultAsync();
            if (comp is not null) return (comp.TenantId, comp.Id);
        }
        var company = await db.Companies.AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { x.Id, x.TenantId })
            .FirstOrDefaultAsync();
        return company is null ? (null, null) : (company.TenantId, company.Id);
    }

    private static string N(string? value, bool upper = false) => string.IsNullOrWhiteSpace(value) ? string.Empty : (upper ? value.Trim().ToUpperInvariant() : value.Trim());

    // Returns null for platform owners (see all); Guid.Empty when no company header is present (returns empty list); otherwise the company's ID.
    private static Guid? GetCompanyFilter(HttpContext httpContext)
    {
        if (ApiTenantScope.IsPlatformOwner(httpContext)) return null;
        return ApiTenantScope.ResolveCompanyId(httpContext) ?? Guid.Empty;
    }

    private static void MapProductionPhases(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/production-phases").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
            var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);
            var query = db.ProductionPhases.AsNoTracking();
            if (!isPlatformOwner && tenantId.HasValue)
                query = query.Where(x => x.TenantId == tenantId.Value);
            var rows = await query.OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new ProductionPhaseDto { ProductionPhaseId = x.Id, TenantId = x.TenantId, Code = x.Code, Name = x.Name, Description = x.Description, Sequence = x.Sequence, IsActive = x.IsActive })
                .ToListAsync();
            return Results.Ok(rows);
        });
        g.MapGet("/options", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
            var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);
            var query = db.ProductionPhases.AsNoTracking().Where(x => x.IsActive);
            if (!isPlatformOwner && tenantId.HasValue)
                query = query.Where(x => x.TenantId == tenantId.Value);
            var rows = await query.OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new ProductionPhaseOptionDto { ProductionPhaseId = x.Id, Code = x.Code, Name = x.Name })
                .ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (ProductionPhaseRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertProductionPhaseAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, ProductionPhaseRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertProductionPhaseAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.ProductionPhases.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound(new { message = "No se encontró la fase de producción." });
            if (await db.ProductComponents.AnyAsync(x => x.ProductionPhaseId == id))
                return Results.BadRequest(new { message = "No puedes eliminar una fase que ya está siendo utilizada por componentes." });
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static void MapMaterialCharacteristics(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-characteristics").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialCharacteristics.AsNoTracking();
            if (companyId.HasValue)
                query = query.Where(x => x.CompanyId == companyId.Value);
            var rows = await query.OrderBy(x => x.Code)
                .Select(x => new MaterialCharacteristicDto { MaterialCharacteristicId = x.Id, CompanyId = x.CompanyId, Code = x.Code, Name = x.Name, Description = x.Description, IsActive = x.IsActive })
                .ToListAsync();
            return Results.Ok(rows);
        });
        g.MapGet("/options", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialCharacteristics.AsNoTracking().Where(x => x.IsActive);
            if (companyId.HasValue)
                query = query.Where(x => x.CompanyId == companyId.Value);
            var rows = await query.OrderBy(x => x.Code)
                .Select(x => new MaterialCharacteristicOptionDto { MaterialCharacteristicId = x.Id, Code = x.Code, Name = x.Name })
                .ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (MaterialCharacteristicRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialCharacteristicAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialCharacteristicRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialCharacteristicAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.MaterialCharacteristics.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            if (await db.MaterialItems.AnyAsync(y => y.MaterialCharacteristicId == id))
                return Results.BadRequest(new { message = "La característica está en uso por materiales existentes." });
            db.MaterialCharacteristics.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static void MapMaterialSizes(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-sizes").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialSizes.AsNoTracking();
            if (companyId.HasValue)
                query = query.Where(x => x.CompanyId == companyId.Value);
            var rows = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
                .Select(x => new MaterialSizeDto { MaterialSizeId = x.Id, CompanyId = x.CompanyId, Code = x.Code, Name = x.Name, Description = x.Description, SortOrder = x.SortOrder, IsActive = x.IsActive })
                .ToListAsync();
            return Results.Ok(rows);
        });
        g.MapGet("/options", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialSizes.AsNoTracking().Where(x => x.IsActive);
            if (companyId.HasValue)
                query = query.Where(x => x.CompanyId == companyId.Value);
            var rows = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
                .Select(x => new MaterialSizeOptionDto { MaterialSizeId = x.Id, Code = x.Code, Name = x.Name })
                .ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (MaterialSizeRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialSizeAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialSizeRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialSizeAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.MaterialSizes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            if (await db.MaterialItems.AnyAsync(y => y.MaterialSizeId == id))
                return Results.BadRequest(new { message = "La talla está en uso por materiales existentes." });
            db.MaterialSizes.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static void MapMaterialFamilies(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-families").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialFamilies.AsNoTracking();
            if (companyId.HasValue)
                query = query.Where(x => x.CompanyId == companyId.Value);
            return Results.Ok(await query.OrderBy(x => x.Code)
                .Select(x => new MaterialFamilyDto { MaterialFamilyId = x.Id, CompanyId = x.CompanyId, Code = x.Code, Name = x.Name, InventoryGroup = x.InventoryGroup, Notes = x.Notes, IsActive = x.IsActive })
                .ToListAsync());
        });
        g.MapPost("/", async (MaterialFamilyRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialFamilyAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialFamilyRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialFamilyAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<MaterialFamily>(db, id, x => db.MaterialSubfamilies.AnyAsync(y => y.MaterialFamilyId == x.Id), "The material family has related subfamilies."));
    }

    private static void MapMaterialSubfamilies(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-subfamilies").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialSubfamilies.AsNoTracking().Include(x => x.MaterialFamily);
            if (companyId.HasValue)
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<MaterialSubfamily, MaterialFamily?>)query.Where(x => x.CompanyId == companyId.Value);
            var rows = await ((IQueryable<MaterialSubfamily>)query).OrderBy(x => x.Code)
                .Select(x => new MaterialSubfamilyDto { MaterialSubfamilyId = x.Id, CompanyId = x.CompanyId, MaterialFamilyId = x.MaterialFamilyId, MaterialFamilyName = x.MaterialFamily != null ? x.MaterialFamily.Name : string.Empty, Code = x.Code, Name = x.Name, MaterialType = x.MaterialType, IsDirectMaterial = x.IsDirectMaterial, Notes = x.Notes, IsActive = x.IsActive })
                .ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (MaterialSubfamilyRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialSubfamilyAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialSubfamilyRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialSubfamilyAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<MaterialSubfamily>(db, id, x => db.MaterialItems.AnyAsync(y => y.MaterialSubfamilyId == x.Id), "The material subfamily has related materials."));
    }

    private static void MapMaterialItems(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-items").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialItems.AsNoTracking()
                .Include(x => x.MaterialSubfamily)
                .Include(x => x.MaterialCharacteristic)
                .Include(x => x.MaterialSize)
                .Include(x => x.PurchaseUnit)
                .Include(x => x.IssueUnit)
                .Include(x => x.Supplier);
            if (companyId.HasValue)
            {
                var rows = await query.Where(x => x.CompanyId == companyId.Value).OrderBy(x => x.Code)
                    .Select(x => new MaterialItemDto
                    {
                        MaterialItemId = x.Id, CompanyId = x.CompanyId,
                        MaterialSubfamilyId = x.MaterialSubfamilyId,
                        MaterialSubfamilyName = x.MaterialSubfamily != null ? x.MaterialSubfamily.Name : string.Empty,
                        MaterialCharacteristicId = x.MaterialCharacteristicId,
                        MaterialCharacteristicName = x.MaterialCharacteristic != null ? x.MaterialCharacteristic.Name : string.Empty,
                        MaterialSizeId = x.MaterialSizeId,
                        MaterialSizeName = x.MaterialSize != null ? x.MaterialSize.Name : string.Empty,
                        PurchaseUnitId = x.PurchaseUnitId,
                        PurchaseUnitName = x.PurchaseUnit != null ? x.PurchaseUnit.Name : string.Empty,
                        IssueUnitId = x.IssueUnitId,
                        IssueUnitName = x.IssueUnit != null ? x.IssueUnit.Name : string.Empty,
                        SupplierId = x.SupplierId,
                        SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                        Code = x.Code, Name = x.Name, Description = x.Description,
                        LegacyMaterialName = x.LegacyMaterialName,
                        AuthorizedCost = x.AuthorizedCost, LastPurchaseCost = x.LastPurchaseCost,
                        StandardCost = x.StandardCost, CostStatus = x.CostStatus,
                        IsServiceItem = x.IsServiceItem, Notes = x.Notes, IsActive = x.IsActive
                    }).ToListAsync();
                return Results.Ok(rows);
            }
            else
            {
                var rows = await query.OrderBy(x => x.Code)
                    .Select(x => new MaterialItemDto
                    {
                        MaterialItemId = x.Id, CompanyId = x.CompanyId,
                        MaterialSubfamilyId = x.MaterialSubfamilyId,
                        MaterialSubfamilyName = x.MaterialSubfamily != null ? x.MaterialSubfamily.Name : string.Empty,
                        MaterialCharacteristicId = x.MaterialCharacteristicId,
                        MaterialCharacteristicName = x.MaterialCharacteristic != null ? x.MaterialCharacteristic.Name : string.Empty,
                        MaterialSizeId = x.MaterialSizeId,
                        MaterialSizeName = x.MaterialSize != null ? x.MaterialSize.Name : string.Empty,
                        PurchaseUnitId = x.PurchaseUnitId,
                        PurchaseUnitName = x.PurchaseUnit != null ? x.PurchaseUnit.Name : string.Empty,
                        IssueUnitId = x.IssueUnitId,
                        IssueUnitName = x.IssueUnit != null ? x.IssueUnit.Name : string.Empty,
                        SupplierId = x.SupplierId,
                        SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                        Code = x.Code, Name = x.Name, Description = x.Description,
                        LegacyMaterialName = x.LegacyMaterialName,
                        AuthorizedCost = x.AuthorizedCost, LastPurchaseCost = x.LastPurchaseCost,
                        StandardCost = x.StandardCost, CostStatus = x.CostStatus,
                        IsServiceItem = x.IsServiceItem, Notes = x.Notes, IsActive = x.IsActive
                    }).ToListAsync();
                return Results.Ok(rows);
            }
        });
        g.MapPost("/", async (MaterialItemRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialItemAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialItemRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertMaterialItemAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<MaterialItem>(db, id, async x => await db.FinishedProducts.AnyAsync(y => y.MainMaterialItemId == x.Id) || await db.FinishedProductMaterials.AnyAsync(y => y.MaterialItemId == x.Id), "The material item is used by products."));
    }

    private static void MapFinishedProducts(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/finished-products").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.FinishedProducts.AsNoTracking()
                .Include(x => x.ProductStyle).Include(x => x.ItemModel).Include(x => x.ItemBrand)
                .Include(x => x.ProductLeatherType).Include(x => x.ProductColor).Include(x => x.ProductToeCap)
                .Include(x => x.ProductSole).Include(x => x.ProductSoleColor).Include(x => x.ProductFolioPattern)
                .Include(x => x.ProductSizeRun).Include(x => x.MainMaterialItem)
                .Include(x => x.ProductLine).Include(x => x.ProductLast).Include(x => x.ProductManufacturingType);
            IQueryable<FinishedProduct> filtered = companyId.HasValue ? query.Where(x => x.CompanyId == companyId.Value) : query;
            var rows = await filtered.OrderBy(x => x.Code)
                .Select(x => new FinishedProductDto
                {
                    FinishedProductId = x.Id, CompanyId = x.CompanyId,
                    ProductStyleId = x.ProductStyleId, ProductStyleName = x.ProductStyle != null ? x.ProductStyle.Code : string.Empty,
                    ItemModelId = x.ItemModelId, ItemModelName = x.ItemModel != null ? x.ItemModel.Name : string.Empty,
                    ItemBrandId = x.ItemBrandId, ItemBrandName = x.ItemBrand != null ? x.ItemBrand.Name : string.Empty,
                    ProductLeatherTypeId = x.ProductLeatherTypeId, ProductLeatherTypeName = x.ProductLeatherType != null ? x.ProductLeatherType.Name : string.Empty,
                    ProductColorId = x.ProductColorId, ProductColorName = x.ProductColor != null ? x.ProductColor.Name : string.Empty,
                    ProductToeCapId = x.ProductToeCapId, ProductToeCapName = x.ProductToeCap != null ? x.ProductToeCap.Name : string.Empty,
                    ProductSoleId = x.ProductSoleId, ProductSoleName = x.ProductSole != null ? x.ProductSole.Name : string.Empty,
                    ProductSoleColorId = x.ProductSoleColorId, ProductSoleColorName = x.ProductSoleColor != null ? x.ProductSoleColor.Name : string.Empty,
                    ProductFolioPatternId = x.ProductFolioPatternId, ProductFolioPatternName = x.ProductFolioPattern != null ? x.ProductFolioPattern.Name : string.Empty,
                    ProductSizeRunId = x.ProductSizeRunId, ProductSizeRunName = x.ProductSizeRun != null ? x.ProductSizeRun.Name : string.Empty,
                    ProductLineId = x.ProductLineId, ProductLineName = x.ProductLine != null ? x.ProductLine.Name : string.Empty,
                    ProductLastId = x.ProductLastId, ProductLastName = x.ProductLast != null ? x.ProductLast.Name : string.Empty,
                    ProductManufacturingTypeId = x.ProductManufacturingTypeId, ProductManufacturingTypeName = x.ProductManufacturingType != null ? x.ProductManufacturingType.Name : string.Empty,
                    MainMaterialItemId = x.MainMaterialItemId, MainMaterialItemName = x.MainMaterialItem != null ? x.MainMaterialItem.Name : string.Empty,
                    Code = x.Code, Name = x.Name, BillingName = x.BillingName,
                    HasPhoto = x.HasPhoto, HasConsumptionDefinition = x.HasConsumptionDefinition,
                    HasMaterialAssignments = x.HasMaterialAssignments, IsAuthorizedForExplosion = x.IsAuthorizedForExplosion,
                    Notes = x.Notes, IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (FinishedProductRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertFinishedProductAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, FinishedProductRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertFinishedProductAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<FinishedProduct>(db, id, async x => await db.FinishedProductMaterials.AnyAsync(y => y.FinishedProductId == x.Id) || await db.ProductConsumptionProfiles.AnyAsync(y => y.FinishedProductId == x.Id), "The finished product has materials or consumptions."));
    }

    private static void MapProductComponents(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/product-components").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.ProductComponents.AsNoTracking().Include(x => x.ConsumptionUnit).Include(x => x.ProductionPhase);
            IQueryable<ProductComponent> filtered = companyId.HasValue ? query.Where(x => x.CompanyId == companyId.Value) : query;
            return Results.Ok(await filtered.OrderBy(x => x.Code)
                .Select(x => new ProductComponentDto { ProductComponentId = x.Id, CompanyId = x.CompanyId, ConsumptionUnitId = x.ConsumptionUnitId, ConsumptionUnitName = x.ConsumptionUnit != null ? x.ConsumptionUnit.Name : string.Empty, ProductionPhaseId = x.ProductionPhaseId, ProductionPhaseName = x.ProductionPhase != null ? $"{x.ProductionPhase.Code} · {x.ProductionPhase.Name}" : string.Empty, Code = x.Code, Name = x.Name, DefaultConsumption = x.DefaultConsumption, ActivateForAllProducts = x.ActivateForAllProducts, ShowOnProductionCard = x.ShowOnProductionCard, Notes = x.Notes, IsActive = x.IsActive })
                .ToListAsync());
        });
        g.MapPost("/", async (ProductComponentRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertProductComponentAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, ProductComponentRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertProductComponentAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ProductComponent>(db, id, async x => await db.FinishedProductMaterials.AnyAsync(y => y.ProductComponentId == x.Id) || await db.ProductConsumptionProfiles.AnyAsync(y => y.ProductComponentId == x.Id), "The component has material or consumption rows."));
    }

    private static void MapFinishedProductMaterials(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/finished-product-materials").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.FinishedProductMaterials.AsNoTracking().Include(x => x.FinishedProduct).Include(x => x.ProductComponent).Include(x => x.MaterialItem);
            IQueryable<FinishedProductMaterial> filtered = companyId.HasValue ? query.Where(x => x.CompanyId == companyId.Value) : query;
            var rows = await filtered.OrderBy(x => x.CreatedAt)
                .Select(x => new FinishedProductMaterialDto { FinishedProductMaterialId = x.Id, CompanyId = x.CompanyId, FinishedProductId = x.FinishedProductId, FinishedProductName = x.FinishedProduct != null ? x.FinishedProduct.Name : string.Empty, ProductComponentId = x.ProductComponentId, ProductComponentName = x.ProductComponent != null ? x.ProductComponent.Name : string.Empty, MaterialItemId = x.MaterialItemId, MaterialItemName = x.MaterialItem != null ? x.MaterialItem.Name : string.Empty, SizeCode = x.SizeCode, Quantity = x.Quantity, IsRequired = x.IsRequired, Notes = x.Notes, IsActive = x.IsActive }).ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (FinishedProductMaterialRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertFinishedProductMaterialAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, FinishedProductMaterialRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertFinishedProductMaterialAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<FinishedProductMaterial>(db, id, x => Task.FromResult(false), string.Empty));
    }

    private static void MapProductConsumptionProfiles(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/product-consumption-profiles").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.ProductConsumptionProfiles.AsNoTracking().Include(x => x.FinishedProduct).Include(x => x.ProductComponent);
            IQueryable<ProductConsumptionProfile> filtered = companyId.HasValue ? query.Where(x => x.CompanyId == companyId.Value) : query;
            var rows = await filtered.OrderBy(x => x.CreatedAt)
                .Select(x => new ProductConsumptionProfileDto { ProductConsumptionProfileId = x.Id, CompanyId = x.CompanyId, FinishedProductId = x.FinishedProductId, FinishedProductName = x.FinishedProduct != null ? x.FinishedProduct.Name : string.Empty, ProductComponentId = x.ProductComponentId, ProductComponentName = x.ProductComponent != null ? x.ProductComponent.Name : string.Empty, SizeCode = x.SizeCode, Pieces = x.Pieces, Consumption = x.Consumption, Status = x.Status, Notes = x.Notes, IsActive = x.IsActive }).ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (ProductConsumptionProfileRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertProductConsumptionProfileAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, ProductConsumptionProfileRequest request, HttpContext httpContext, NanchesoftDbContext db) => await UpsertProductConsumptionProfileAsync(id, request, httpContext, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ProductConsumptionProfile>(db, id, x => Task.FromResult(false), string.Empty));
    }

    private static void MapFinishedProductSupplies(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/finished-product-supplies").WithTags("ProductCatalogOperations");

        // Get all supplies for a product (with sizes)
        g.MapGet("/{productId:guid}", async (Guid productId, NanchesoftDbContext db) =>
        {
            var supplies = await db.FinishedProductSupplies.AsNoTracking()
                .Where(x => x.FinishedProductId == productId)
                .Include(x => x.ProductComponent)
                .Include(x => x.Sizes).ThenInclude(s => s.SizeRunSize)
                .Include(x => x.Sizes).ThenInclude(s => s.MaterialItem)
                .OrderBy(x => x.ProductComponent!.Code)
                .Select(x => new FinishedProductSupplyDto
                {
                    FinishedProductSupplyId = x.Id,
                    FinishedProductId = x.FinishedProductId,
                    ProductComponentId = x.ProductComponentId,
                    ProductComponentCode = x.ProductComponent != null ? x.ProductComponent.Code : string.Empty,
                    ProductComponentName = x.ProductComponent != null ? x.ProductComponent.Name : string.Empty,
                    IsAuthorized = x.IsAuthorized,
                    AuthorizedAt = x.AuthorizedAt,
                    AuthorizedBy = x.AuthorizedBy ?? string.Empty,
                    Notes = x.Notes,
                    IsActive = x.IsActive,
                    Sizes = x.Sizes.Select(s => new FinishedProductSupplySizeDto
                    {
                        FinishedProductSupplySizeId = s.Id,
                        FinishedProductSupplyId = s.FinishedProductSupplyId,
                        ProductSizeRunSizeId = s.ProductSizeRunSizeId,
                        SizeLabel = s.SizeRunSize != null ? s.SizeRunSize.SizeCode : string.Empty,
                        MaterialItemId = s.MaterialItemId,
                        MaterialItemName = s.MaterialItem != null ? s.MaterialItem.Name : string.Empty,
                        Notes = s.Notes
                    }).OrderBy(s => s.SizeLabel).ToList()
                }).ToListAsync();
            return Results.Ok(supplies);
        });

        // Initialize supplies from consumption template
        g.MapPost("/{productId:guid}/initialize", async (Guid productId, NanchesoftDbContext db) =>
        {
            var product = await db.FinishedProducts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == productId);
            if (product is null) return Results.NotFound(new { message = "Producto no encontrado." });
            if (!product.ProductStyleId.HasValue || !product.ProductSizeRunId.HasValue)
                return Results.BadRequest(new { message = "El producto debe tener estilo y corrida asignados." });

            var template = await db.ConsumptionTemplates.AsNoTracking()
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.CompanyId == product.CompanyId
                    && x.ProductStyleId == product.ProductStyleId.Value
                    && x.ProductSizeRunId == product.ProductSizeRunId.Value
                    && x.IsActive);
            if (template is null)
                return Results.BadRequest(new { message = "No hay una plantilla de consumo activa para el estilo y corrida del producto." });

            var sizes = await db.ProductSizeRunSizes.AsNoTracking()
                .Where(x => x.ProductSizeRunId == product.ProductSizeRunId.Value && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync();

            var existingSupplies = await db.FinishedProductSupplies
                .Include(x => x.Sizes)
                .Where(x => x.FinishedProductId == productId)
                .ToListAsync();

            int created = 0;
            foreach (var detail in template.Details.Where(d => d.IsActive))
            {
                var supply = existingSupplies.FirstOrDefault(s => s.ProductComponentId == detail.ProductComponentId);
                if (supply is null)
                {
                    supply = new FinishedProductSupply
                    {
                        TenantId = product.TenantId,
                        CompanyId = product.CompanyId,
                        FinishedProductId = productId,
                        ProductComponentId = detail.ProductComponentId,
                        CreatedBy = "web-api"
                    };
                    db.FinishedProductSupplies.Add(supply);
                    await db.SaveChangesAsync();
                    created++;
                }

                foreach (var size in sizes)
                {
                    if (!supply.Sizes.Any(ss => ss.ProductSizeRunSizeId == size.Id))
                    {
                        db.FinishedProductSupplySizes.Add(new FinishedProductSupplySize
                        {
                            FinishedProductSupplyId = supply.Id,
                            ProductSizeRunSizeId = size.Id,
                            CreatedBy = "web-api"
                        });
                    }
                }
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, created });
        });

        // Save material assignment for a single supply-size cell
        g.MapPut("/sizes/{sizeId:guid}", async (Guid sizeId, FinishedProductSupplySizeRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.FinishedProductSupplySizes.FirstOrDefaultAsync(x => x.Id == sizeId);
            if (entity is null) return Results.NotFound();
            entity.MaterialItemId = request.MaterialItemId;
            entity.Notes = request.Notes ?? string.Empty;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Batch save sizes for one supply row
        g.MapPut("/{supplyId:guid}/sizes", async (Guid supplyId, List<FinishedProductSupplySizeRequest> requests, NanchesoftDbContext db) =>
        {
            var supply = await db.FinishedProductSupplies.Include(x => x.Sizes).FirstOrDefaultAsync(x => x.Id == supplyId);
            if (supply is null) return Results.NotFound();
            foreach (var req in requests)
            {
                var sz = supply.Sizes.FirstOrDefault(s => s.ProductSizeRunSizeId == req.ProductSizeRunSizeId);
                if (sz is null) continue;
                sz.MaterialItemId = req.MaterialItemId;
                sz.Notes = req.Notes ?? string.Empty;
                sz.UpdatedAt = DateTime.UtcNow;
                sz.UpdatedBy = "web-api";
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Authorize supplies
        g.MapPost("/{productId:guid}/authorize", async (Guid productId, NanchesoftDbContext db) =>
        {
            var supplies = await db.FinishedProductSupplies
                .Include(x => x.Sizes)
                .Where(x => x.FinishedProductId == productId && x.IsActive)
                .ToListAsync();
            if (!supplies.Any()) return Results.BadRequest(new { message = "El producto no tiene insumos inicializados." });

            var errors = new List<string>();
            foreach (var s in supplies)
            {
                var missing = s.Sizes.Count(ss => ss.MaterialItemId is null);
                if (missing > 0)
                    errors.Add($"Componente {s.ProductComponentId}: faltan {missing} tallas sin material asignado.");
            }
            if (errors.Any()) return Results.BadRequest(new { message = "No se puede autorizar.", errors });

            var now = DateTime.UtcNow;
            foreach (var s in supplies)
            {
                s.IsAuthorized = true;
                s.AuthorizedAt = now;
                s.AuthorizedBy = "web-api";
                s.UpdatedAt = now;
                s.UpdatedBy = "web-api";
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static void MapMaterialSizeDistributions(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-size-distributions").WithTags("ProductCatalogOperations");

        g.MapGet("/", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialSizeDistributions.AsNoTracking()
                .Include(x => x.MaterialSubfamily)
                .Include(x => x.ProductSizeRun)
                .Include(x => x.ProductLast)
                .Include(x => x.Details).ThenInclude(d => d.SizeRunSize)
                .Include(x => x.Details).ThenInclude(d => d.MaterialItem);
            IQueryable<MaterialSizeDistribution> filtered = companyId.HasValue ? query.Where(x => x.CompanyId == companyId.Value) : query;
            var rows = await filtered
                .OrderBy(x => x.MaterialSubfamily!.Name).ThenBy(x => x.ProductSizeRun!.Name)
                .Select(x => new MaterialSizeDistributionDto
                {
                    MaterialSizeDistributionId = x.Id,
                    CompanyId = x.CompanyId,
                    MaterialSubfamilyId = x.MaterialSubfamilyId,
                    MaterialSubfamilyName = x.MaterialSubfamily != null ? x.MaterialSubfamily.Name : string.Empty,
                    ProductSizeRunId = x.ProductSizeRunId,
                    ProductSizeRunName = x.ProductSizeRun != null ? x.ProductSizeRun.Name : string.Empty,
                    ProductLastId = x.ProductLastId,
                    ProductLastName = x.ProductLast != null ? x.ProductLast.Name : string.Empty,
                    Notes = x.Notes,
                    IsActive = x.IsActive,
                    Details = x.Details.Select(d => new MaterialSizeDistributionDetailDto
                    {
                        MaterialSizeDistributionDetailId = d.Id,
                        MaterialSizeDistributionId = d.MaterialSizeDistributionId,
                        ProductSizeRunSizeId = d.ProductSizeRunSizeId,
                        SizeLabel = d.SizeRunSize != null ? d.SizeRunSize.SizeCode : string.Empty,
                        MaterialItemId = d.MaterialItemId,
                        MaterialItemName = d.MaterialItem != null ? d.MaterialItem.Name : string.Empty,
                        Notes = d.Notes
                    }).OrderBy(d => d.SizeLabel).ToList()
                }).ToListAsync();
            return Results.Ok(rows);
        });

        // Lookup for dispersion: find distribution by subfamily+run+last
        g.MapGet("/lookup", async (Guid subfamilyId, Guid runId, Guid? lastId, NanchesoftDbContext db) =>
        {
            var dist = await db.MaterialSizeDistributions.AsNoTracking()
                .Include(x => x.Details).ThenInclude(d => d.SizeRunSize)
                .Include(x => x.Details).ThenInclude(d => d.MaterialItem)
                .FirstOrDefaultAsync(x => x.MaterialSubfamilyId == subfamilyId
                    && x.ProductSizeRunId == runId
                    && x.ProductLastId == lastId
                    && x.IsActive);
            if (dist is null) return Results.NotFound();
            var result = dist.Details.Select(d => new
            {
                productSizeRunSizeId = d.ProductSizeRunSizeId,
                sizeLabel = d.SizeRunSize != null ? d.SizeRunSize.SizeCode : string.Empty,
                materialItemId = d.MaterialItemId,
                materialItemName = d.MaterialItem != null ? d.MaterialItem.Name : string.Empty
            }).OrderBy(d => d.sizeLabel).ToList();
            return Results.Ok(result);
        });

        g.MapPost("/", async (MaterialSizeDistributionRequest request, HttpContext httpContext, NanchesoftDbContext db) =>
            await UpsertMaterialSizeDistributionAsync(null, request, httpContext, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialSizeDistributionRequest request, HttpContext httpContext, NanchesoftDbContext db) =>
            await UpsertMaterialSizeDistributionAsync(id, request, httpContext, db));

        // Save all detail rows for a distribution
        g.MapPut("/{id:guid}/details", async (Guid id, List<MaterialSizeDistributionDetailRequest> requests, NanchesoftDbContext db) =>
        {
            var dist = await db.MaterialSizeDistributions.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
            if (dist is null) return Results.NotFound();
            foreach (var req in requests)
            {
                var detail = dist.Details.FirstOrDefault(d => d.ProductSizeRunSizeId == req.ProductSizeRunSizeId);
                if (detail is null)
                {
                    detail = new MaterialSizeDistributionDetail
                    {
                        MaterialSizeDistributionId = id,
                        ProductSizeRunSizeId = req.ProductSizeRunSizeId,
                        CreatedBy = "web-api"
                    };
                    db.MaterialSizeDistributionDetails.Add(detail);
                }
                detail.MaterialItemId = req.MaterialItemId;
                detail.Notes = req.Notes ?? string.Empty;
                detail.UpdatedAt = DateTime.UtcNow;
                detail.UpdatedBy = "web-api";
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.MaterialSizeDistributions.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            db.MaterialSizeDistributions.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static async Task<IResult> UpsertMaterialSizeDistributionAsync(Guid? id, MaterialSizeDistributionRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        if (request.MaterialSubfamilyId == Guid.Empty || request.ProductSizeRunId == Guid.Empty)
            return Results.BadRequest(new { message = "Subfamilia y corrida son obligatorias." });

        if (await db.MaterialSizeDistributions.AnyAsync(x =>
            x.CompanyId == ctx.CompanyId
            && x.MaterialSubfamilyId == request.MaterialSubfamilyId
            && x.ProductSizeRunId == request.ProductSizeRunId
            && x.ProductLastId == request.ProductLastId
            && (!id.HasValue || x.Id != id.Value)))
            return Results.BadRequest(new { message = "Ya existe una distribución con esa combinación de subfamilia, corrida y horma." });

        var entity = id.HasValue ? await db.MaterialSizeDistributions.FirstOrDefaultAsync(x => x.Id == id.Value) : null;
        if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialSizeDistribution { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.MaterialSubfamilyId = request.MaterialSubfamilyId;
        entity.ProductSizeRunId = request.ProductSizeRunId;
        entity.ProductLastId = request.ProductLastId;
        entity.Notes = request.Notes ?? string.Empty;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialSizeDistributions.Add(entity);

        // If new, auto-create detail rows for each size in the run
        if (!id.HasValue)
        {
            await db.SaveChangesAsync();
            var sizes = await db.ProductSizeRunSizes.AsNoTracking()
                .Where(x => x.ProductSizeRunId == request.ProductSizeRunId && x.IsActive)
                .ToListAsync();
            foreach (var s in sizes)
            {
                db.MaterialSizeDistributionDetails.Add(new MaterialSizeDistributionDetail
                {
                    MaterialSizeDistributionId = entity.Id,
                    ProductSizeRunSizeId = s.Id,
                    CreatedBy = "web-api"
                });
            }
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static void MapLookups(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/material-families/options", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialFamilies.AsNoTracking();
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            return Results.Ok(await query.OrderBy(x => x.Code).Select(x => new MaterialFamilyOptionDto { MaterialFamilyId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync());
        }).WithTags("ProductCatalogOperations");

        app.MapGet("/api/products/material-subfamilies/options", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialSubfamilies.AsNoTracking();
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            return Results.Ok(await query.OrderBy(x => x.Code).Select(x => new MaterialSubfamilyOptionDto { MaterialSubfamilyId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync());
        }).WithTags("ProductCatalogOperations");

        app.MapGet("/api/products/material-items/options", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.MaterialItems.AsNoTracking();
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            return Results.Ok(await query.OrderBy(x => x.Code).Select(x => new MaterialItemOptionDto { MaterialItemId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync());
        }).WithTags("ProductCatalogOperations");

        app.MapGet("/api/products/finished-products/options", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.FinishedProducts.AsNoTracking();
            if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
            return Results.Ok(await query.OrderBy(x => x.Code).Select(x => new FinishedProductOptionDto { FinishedProductId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync());
        }).WithTags("ProductCatalogOperations");

        app.MapGet("/api/products/product-components/options", async (HttpContext httpContext, NanchesoftDbContext db) =>
        {
            var companyId = GetCompanyFilter(httpContext);
            var query = db.ProductComponents.AsNoTracking().Include(x => x.ProductionPhase);
            IQueryable<ProductComponent> filtered = companyId.HasValue ? query.Where(x => x.CompanyId == companyId.Value) : query;
            return Results.Ok(await filtered.OrderBy(x => x.Code).Select(x => new ProductComponentOptionDto { ProductComponentId = x.Id, Code = x.Code, Name = x.Name, ProductionPhaseName = x.ProductionPhase != null ? x.ProductionPhase.Name : string.Empty }).ToListAsync());
        }).WithTags("ProductCatalogOperations");
    }

    // upserts
    private static async Task<IResult> UpsertProductionPhaseAsync(Guid? id, ProductionPhaseRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var currentTenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);
        var tenantId = currentTenantId ?? await db.Tenants.OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        if (!tenantId.HasValue) return Results.BadRequest(new { message = "No existe un tenant disponible." });
        if (!isPlatformOwner && currentTenantId.HasValue && tenantId.Value != currentTenantId.Value)
            return Results.BadRequest(new { message = "La fase no pertenece al tenant activo." });

        var code = N(request.Code, true);
        var name = N(request.Name);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.ProductionPhases.AnyAsync(x => x.TenantId == tenantId.Value && x.Code == code && (!id.HasValue || x.Id != id.Value)))
            return Results.BadRequest(new { message = "Ya existe una fase con ese código." });

        var entity = id.HasValue ? await db.ProductionPhases.FirstOrDefaultAsync(x => x.Id == id.Value) : null;
        if (id.HasValue && entity is null) return Results.NotFound(new { message = "No se encontró la fase de producción." });

        entity ??= new ProductionPhase { TenantId = tenantId.Value, CreatedBy = "web-api" };
        entity.Code = code;
        entity.Name = name;
        entity.Description = N(request.Description);
        entity.Sequence = request.Sequence;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        if (!id.HasValue) db.ProductionPhases.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpsertMaterialFamilyAsync(Guid? id, MaterialFamilyRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = N(request.Code, true); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Code and Name are required." });
        if (await db.MaterialFamilies.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Material family code already exists." });
        var entity = id.HasValue ? await db.MaterialFamilies.FirstOrDefaultAsync(x => x.Id == id.Value) : null;
        if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialFamily { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.Code = code; entity.Name = N(request.Name); entity.InventoryGroup = N(request.InventoryGroup); entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialFamilies.Add(entity);
        await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertMaterialSubfamilyAsync(Guid? id, MaterialSubfamilyRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        if (request.MaterialFamilyId == Guid.Empty) return Results.BadRequest(new { message = "MaterialFamilyId is required." });
        var code = N(request.Code, true); if (await db.MaterialSubfamilies.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.MaterialFamilyId == request.MaterialFamilyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Subfamily code already exists." });
        var entity = id.HasValue ? await db.MaterialSubfamilies.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialSubfamily { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.MaterialFamilyId = request.MaterialFamilyId; entity.Code = code; entity.Name = N(request.Name); entity.MaterialType = N(request.MaterialType); entity.IsDirectMaterial = request.IsDirectMaterial; entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialSubfamilies.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertMaterialCharacteristicAsync(Guid? id, MaterialCharacteristicRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = N(request.Code, true); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Código y nombre son obligatorios." });
        if (await db.MaterialCharacteristics.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Ya existe una característica con ese código." });
        var entity = id.HasValue ? await db.MaterialCharacteristics.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialCharacteristic { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.Code = code; entity.Name = N(request.Name, true); entity.Description = N(request.Description); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialCharacteristics.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpsertMaterialSizeAsync(Guid? id, MaterialSizeRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = N(request.Code, true); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Código y nombre son obligatorios." });
        if (await db.MaterialSizes.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Ya existe una talla con ese código." });
        var entity = id.HasValue ? await db.MaterialSizes.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialSize { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.Code = code; entity.Name = N(request.Name, true); entity.Description = N(request.Description); entity.SortOrder = request.SortOrder; entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialSizes.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpsertMaterialItemAsync(Guid? id, MaterialItemRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        if (request.MaterialSubfamilyId == Guid.Empty) return Results.BadRequest(new { message = "MaterialSubfamilyId is required." });
        var code = N(request.Code, true);
        if (await db.MaterialItems.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value)))
            return Results.BadRequest(new { message = "Material item code already exists." });

        // Resolve auto-generated name from characteristic + size
        string resolvedName;
        if (request.MaterialCharacteristicId.HasValue && request.MaterialSizeId.HasValue)
        {
            var characteristic = await db.MaterialCharacteristics.FindAsync(request.MaterialCharacteristicId.Value);
            var size = await db.MaterialSizes.FindAsync(request.MaterialSizeId.Value);
            if (characteristic is null) return Results.BadRequest(new { message = "No se encontró la característica de material." });
            if (size is null) return Results.BadRequest(new { message = "No se encontró la talla de material." });
            resolvedName = MaterialItem.BuildName(characteristic.Name, size.Name);

            // Unique by characteristic + size within company (excluding current record)
            if (await db.MaterialItems.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.MaterialCharacteristicId == request.MaterialCharacteristicId && x.MaterialSizeId == request.MaterialSizeId && (!id.HasValue || x.Id != id.Value)))
                return Results.BadRequest(new { message = "Ya existe un material con esa combinación de característica y talla." });
        }
        else
        {
            resolvedName = N(request.Name, true);
            if (string.IsNullOrWhiteSpace(resolvedName)) return Results.BadRequest(new { message = "Nombre es obligatorio cuando no se usa característica + talla." });
        }

        // Unique name within company
        if (await db.MaterialItems.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Name == resolvedName && (!id.HasValue || x.Id != id.Value)))
            return Results.BadRequest(new { message = $"Ya existe un material con el nombre '{resolvedName}'." });

        var entity = id.HasValue ? await db.MaterialItems.FirstOrDefaultAsync(x => x.Id == id.Value) : null;
        if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialItem { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.MaterialSubfamilyId = request.MaterialSubfamilyId;
        entity.MaterialCharacteristicId = request.MaterialCharacteristicId;
        entity.MaterialSizeId = request.MaterialSizeId;
        entity.PurchaseUnitId = request.PurchaseUnitId;
        entity.IssueUnitId = request.IssueUnitId;
        entity.SupplierId = request.SupplierId;
        entity.Code = code;
        entity.Name = resolvedName;
        entity.Description = N(request.Description);
        entity.LegacyMaterialName = N(request.LegacyMaterialName);
        entity.AuthorizedCost = request.AuthorizedCost;
        entity.LastPurchaseCost = request.LastPurchaseCost;
        entity.StandardCost = request.StandardCost;
        entity.CostStatus = N(request.CostStatus);
        entity.IsServiceItem = request.IsServiceItem;
        entity.Notes = N(request.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialItems.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id, generatedName = resolvedName });
    }
    private static async Task<IResult> UpsertFinishedProductAsync(Guid? id, FinishedProductRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = N(request.Code, true); if (string.IsNullOrWhiteSpace(code)) return Results.BadRequest(new { message = "Code is required." });
        if (await db.FinishedProducts.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Finished product code already exists." });
        var entity = id.HasValue ? await db.FinishedProducts.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new FinishedProduct { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.ProductStyleId = request.ProductStyleId;
        entity.ItemModelId = request.ItemModelId;
        entity.ItemBrandId = request.ItemBrandId;
        entity.ProductLeatherTypeId = request.ProductLeatherTypeId;
        entity.ProductColorId = request.ProductColorId;
        entity.ProductToeCapId = request.ProductToeCapId;
        entity.ProductSoleId = request.ProductSoleId;
        entity.ProductSoleColorId = request.ProductSoleColorId;
        entity.ProductFolioPatternId = request.ProductFolioPatternId;
        entity.ProductSizeRunId = request.ProductSizeRunId;
        entity.ProductLineId = request.ProductLineId;
        entity.ProductLastId = request.ProductLastId;
        entity.ProductManufacturingTypeId = request.ProductManufacturingTypeId;
        entity.MainMaterialItemId = request.MainMaterialItemId;
        entity.Code = code;
        entity.BillingName = N(request.BillingName);
        entity.HasPhoto = request.HasPhoto;
        entity.HasConsumptionDefinition = request.HasConsumptionDefinition;
        entity.HasMaterialAssignments = request.HasMaterialAssignments;
        entity.IsAuthorizedForExplosion = request.IsAuthorizedForExplosion;
        entity.Notes = N(request.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        entity.Name = string.IsNullOrWhiteSpace(request.Name)
            ? await BuildFinishedProductNameAsync(entity, db)
            : request.Name.Trim();
        if (!id.HasValue) db.FinishedProducts.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<string?> BuildFinishedProductNameAsync(FinishedProduct e, NanchesoftDbContext db)
    {
        var parts = new List<string>();
        if (e.ProductStyleId.HasValue) { var x = await db.ProductStyles.FindAsync(e.ProductStyleId); if (x != null) parts.Add(x.Code); }
        if (e.ItemModelId.HasValue) { var x = await db.ItemModels.FindAsync(e.ItemModelId); if (x != null) parts.Add(x.Name); }
        if (e.ItemBrandId.HasValue) { var x = await db.ItemBrands.FindAsync(e.ItemBrandId); if (x != null) parts.Add(x.Name); }
        if (e.ProductLeatherTypeId.HasValue) { var x = await db.ProductLeatherTypes.FindAsync(e.ProductLeatherTypeId); if (x != null) parts.Add(x.Name); }
        if (e.ProductColorId.HasValue) { var x = await db.ProductColors.FindAsync(e.ProductColorId); if (x != null) parts.Add(x.Name); }
        if (e.ProductToeCapId.HasValue) { var x = await db.ProductToeCaps.FindAsync(e.ProductToeCapId); if (x != null) parts.Add(x.Name); }
        if (e.ProductSoleId.HasValue) { var x = await db.ProductSoles.FindAsync(e.ProductSoleId); if (x != null) parts.Add(x.Name); }
        if (e.ProductSoleColorId.HasValue) { var x = await db.ProductSoleColors.FindAsync(e.ProductSoleColorId); if (x != null) parts.Add(x.Name); }
        if (e.ProductSizeRunId.HasValue) { var x = await db.ProductSizeRuns.FindAsync(e.ProductSizeRunId); if (x != null) parts.Add(x.Name); }
        var result = string.Join(" ", parts).Trim();
        return result.Length > 0 ? result : null;
    }

    private static async Task<IResult> UpsertProductComponentAsync(Guid? id, ProductComponentRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = N(request.Code, true); if (await db.ProductComponents.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Component code already exists." });
        var entity = id.HasValue ? await db.ProductComponents.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new ProductComponent { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.ConsumptionUnitId = request.ConsumptionUnitId; entity.ProductionPhaseId = request.ProductionPhaseId; entity.Code = code; entity.Name = N(request.Name); entity.DefaultConsumption = request.DefaultConsumption; entity.ActivateForAllProducts = request.ActivateForAllProducts; entity.ShowOnProductionCard = request.ShowOnProductionCard; entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.ProductComponents.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertFinishedProductMaterialAsync(Guid? id, FinishedProductMaterialRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var entity = id.HasValue ? await db.FinishedProductMaterials.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new FinishedProductMaterial { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.FinishedProductId = request.FinishedProductId; entity.ProductComponentId = request.ProductComponentId; entity.MaterialItemId = request.MaterialItemId; entity.SizeCode = N(request.SizeCode); entity.Quantity = request.Quantity; entity.IsRequired = request.IsRequired; entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.FinishedProductMaterials.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertProductConsumptionProfileAsync(Guid? id, ProductConsumptionProfileRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db, httpContext); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var entity = id.HasValue ? await db.ProductConsumptionProfiles.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new ProductConsumptionProfile { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.FinishedProductId = request.FinishedProductId; entity.ProductComponentId = request.ProductComponentId; entity.SizeCode = N(request.SizeCode); entity.Pieces = request.Pieces; entity.Consumption = request.Consumption; entity.Status = N(request.Status); entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.ProductConsumptionProfiles.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> DeleteEntityAsync<TEntity>(NanchesoftDbContext db, Guid id, Func<TEntity, Task<bool>> hasRelations, string relationMessage) where TEntity : class
    {
        var entity = await db.Set<TEntity>().FindAsync(id);
        if (entity is null) return Results.NotFound();
        if (hasRelations != null && await hasRelations(entity) && !string.IsNullOrWhiteSpace(relationMessage)) return Results.BadRequest(new { message = relationMessage });
        db.Set<TEntity>().Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class ProductionPhaseOptionDto { public Guid ProductionPhaseId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class ProductionPhaseDto { public Guid ProductionPhaseId { get; set; } public Guid TenantId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public int Sequence { get; set; } public bool IsActive { get; set; } }
public sealed class ProductionPhaseRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public int Sequence { get; set; } public bool IsActive { get; set; } = true; }
public sealed class MaterialFamilyOptionDto { public Guid MaterialFamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class MaterialSubfamilyOptionDto { public Guid MaterialSubfamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class MaterialItemOptionDto { public Guid MaterialItemId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class FinishedProductOptionDto { public Guid FinishedProductId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class ProductComponentOptionDto { public Guid ProductComponentId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string ProductionPhaseName { get; set; } = string.Empty; }

public class MaterialFamilyRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string InventoryGroup { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialFamilyDto : MaterialFamilyRequest { public Guid MaterialFamilyId { get; set; } public Guid CompanyId { get; set; } }
public class MaterialSubfamilyRequest { public Guid MaterialFamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string MaterialType { get; set; } = string.Empty; public bool IsDirectMaterial { get; set; } = true; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialSubfamilyDto : MaterialSubfamilyRequest { public Guid MaterialSubfamilyId { get; set; } public Guid CompanyId { get; set; } public string MaterialFamilyName { get; set; } = string.Empty; }
public class MaterialCharacteristicRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialCharacteristicDto : MaterialCharacteristicRequest { public Guid MaterialCharacteristicId { get; set; } public Guid CompanyId { get; set; } }
public sealed class MaterialCharacteristicOptionDto { public Guid MaterialCharacteristicId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public class MaterialSizeRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public int SortOrder { get; set; } public bool IsActive { get; set; } = true; }
public sealed class MaterialSizeDto : MaterialSizeRequest { public Guid MaterialSizeId { get; set; } public Guid CompanyId { get; set; } }
public sealed class MaterialSizeOptionDto { public Guid MaterialSizeId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public class MaterialItemRequest { public Guid MaterialSubfamilyId { get; set; } public Guid? MaterialCharacteristicId { get; set; } public Guid? MaterialSizeId { get; set; } public Guid? PurchaseUnitId { get; set; } public Guid? IssueUnitId { get; set; } public Guid? SupplierId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string LegacyMaterialName { get; set; } = string.Empty; public decimal AuthorizedCost { get; set; } public decimal LastPurchaseCost { get; set; } public decimal StandardCost { get; set; } public string CostStatus { get; set; } = string.Empty; public bool IsServiceItem { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialItemDto : MaterialItemRequest { public Guid MaterialItemId { get; set; } public Guid CompanyId { get; set; } public string MaterialSubfamilyName { get; set; } = string.Empty; public string MaterialCharacteristicName { get; set; } = string.Empty; public string MaterialSizeName { get; set; } = string.Empty; public string PurchaseUnitName { get; set; } = string.Empty; public string IssueUnitName { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; }
public class FinishedProductRequest { public Guid? ProductStyleId { get; set; } public Guid? ItemModelId { get; set; } public Guid? ItemBrandId { get; set; } public Guid? ProductLeatherTypeId { get; set; } public Guid? ProductColorId { get; set; } public Guid? ProductToeCapId { get; set; } public Guid? ProductSoleId { get; set; } public Guid? ProductSoleColorId { get; set; } public Guid? ProductFolioPatternId { get; set; } public Guid? ProductSizeRunId { get; set; } public Guid? ProductLineId { get; set; } public Guid? ProductLastId { get; set; } public Guid? ProductManufacturingTypeId { get; set; } public Guid? MainMaterialItemId { get; set; } public string Code { get; set; } = string.Empty; public string? Name { get; set; } public string BillingName { get; set; } = string.Empty; public bool HasPhoto { get; set; } public bool HasConsumptionDefinition { get; set; } public bool HasMaterialAssignments { get; set; } public bool IsAuthorizedForExplosion { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class FinishedProductDto : FinishedProductRequest { public Guid FinishedProductId { get; set; } public Guid CompanyId { get; set; } public string ProductStyleName { get; set; } = string.Empty; public string ItemModelName { get; set; } = string.Empty; public string ItemBrandName { get; set; } = string.Empty; public string ProductLeatherTypeName { get; set; } = string.Empty; public string ProductColorName { get; set; } = string.Empty; public string ProductToeCapName { get; set; } = string.Empty; public string ProductSoleName { get; set; } = string.Empty; public string ProductSoleColorName { get; set; } = string.Empty; public string ProductFolioPatternName { get; set; } = string.Empty; public string ProductSizeRunName { get; set; } = string.Empty; public string ProductLineName { get; set; } = string.Empty; public string ProductLastName { get; set; } = string.Empty; public string ProductManufacturingTypeName { get; set; } = string.Empty; public string MainMaterialItemName { get; set; } = string.Empty; }
public class ProductComponentRequest { public Guid? ConsumptionUnitId { get; set; } public Guid? ProductionPhaseId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public decimal DefaultConsumption { get; set; } public bool ActivateForAllProducts { get; set; } public bool ShowOnProductionCard { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductComponentDto : ProductComponentRequest { public Guid ProductComponentId { get; set; } public Guid CompanyId { get; set; } public string ConsumptionUnitName { get; set; } = string.Empty; public string ProductionPhaseName { get; set; } = string.Empty; }
public class FinishedProductMaterialRequest { public Guid FinishedProductId { get; set; } public Guid ProductComponentId { get; set; } public Guid MaterialItemId { get; set; } public string SizeCode { get; set; } = string.Empty; public decimal Quantity { get; set; } public bool IsRequired { get; set; } = true; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class FinishedProductMaterialDto : FinishedProductMaterialRequest { public Guid FinishedProductMaterialId { get; set; } public Guid CompanyId { get; set; } public string FinishedProductName { get; set; } = string.Empty; public string ProductComponentName { get; set; } = string.Empty; public string MaterialItemName { get; set; } = string.Empty; }
public class ProductConsumptionProfileRequest { public Guid FinishedProductId { get; set; } public Guid ProductComponentId { get; set; } public string SizeCode { get; set; } = string.Empty; public int Pieces { get; set; } public decimal Consumption { get; set; } public string Status { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductConsumptionProfileDto : ProductConsumptionProfileRequest { public Guid ProductConsumptionProfileId { get; set; } public Guid CompanyId { get; set; } public string FinishedProductName { get; set; } = string.Empty; public string ProductComponentName { get; set; } = string.Empty; }

public sealed class FinishedProductSupplySizeDto { public Guid FinishedProductSupplySizeId { get; set; } public Guid FinishedProductSupplyId { get; set; } public Guid ProductSizeRunSizeId { get; set; } public string SizeLabel { get; set; } = string.Empty; public Guid? MaterialItemId { get; set; } public string MaterialItemName { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; }
public sealed class FinishedProductSupplyDto { public Guid FinishedProductSupplyId { get; set; } public Guid FinishedProductId { get; set; } public Guid ProductComponentId { get; set; } public string ProductComponentCode { get; set; } = string.Empty; public string ProductComponentName { get; set; } = string.Empty; public bool IsAuthorized { get; set; } public DateTime? AuthorizedAt { get; set; } public string AuthorizedBy { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } public List<FinishedProductSupplySizeDto> Sizes { get; set; } = new(); }
public sealed class FinishedProductSupplySizeRequest { public Guid ProductSizeRunSizeId { get; set; } public Guid? MaterialItemId { get; set; } public string? Notes { get; set; } }

public sealed class MaterialSizeDistributionDetailDto { public Guid MaterialSizeDistributionDetailId { get; set; } public Guid MaterialSizeDistributionId { get; set; } public Guid ProductSizeRunSizeId { get; set; } public string SizeLabel { get; set; } = string.Empty; public Guid? MaterialItemId { get; set; } public string MaterialItemName { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; }
public sealed class MaterialSizeDistributionDto { public Guid MaterialSizeDistributionId { get; set; } public Guid CompanyId { get; set; } public Guid MaterialSubfamilyId { get; set; } public string MaterialSubfamilyName { get; set; } = string.Empty; public Guid ProductSizeRunId { get; set; } public string ProductSizeRunName { get; set; } = string.Empty; public Guid? ProductLastId { get; set; } public string ProductLastName { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } public List<MaterialSizeDistributionDetailDto> Details { get; set; } = new(); }
public sealed class MaterialSizeDistributionRequest { public Guid MaterialSubfamilyId { get; set; } public Guid ProductSizeRunId { get; set; } public Guid? ProductLastId { get; set; } public string? Notes { get; set; } public bool IsActive { get; set; } = true; }
public sealed class MaterialSizeDistributionDetailRequest { public Guid ProductSizeRunSizeId { get; set; } public Guid? MaterialItemId { get; set; } public string? Notes { get; set; } }
