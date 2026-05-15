using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductCatalogOperationsEndpoints
{
    public static void MapProductCatalogOperations(IEndpointRouteBuilder app)
    {
        MapMaterialFamilies(app);
        MapMaterialSubfamilies(app);
        MapMaterialItems(app);
        MapFinishedProducts(app);
        MapProductComponents(app);
        MapFinishedProductMaterials(app);
        MapProductConsumptionProfiles(app);
        MapLookups(app);
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return company is null ? (null, null) : (company.TenantId, company.Id);
    }

    private static string N(string? value, bool upper = false) => string.IsNullOrWhiteSpace(value) ? string.Empty : (upper ? value.Trim().ToUpperInvariant() : value.Trim());

    private static void MapMaterialFamilies(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-families").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (NanchesoftDbContext db) => Results.Ok(await db.MaterialFamilies.AsNoTracking().OrderBy(x => x.Code).Select(x => new MaterialFamilyDto { MaterialFamilyId = x.Id, CompanyId = x.CompanyId, Code = x.Code, Name = x.Name, InventoryGroup = x.InventoryGroup, Notes = x.Notes, IsActive = x.IsActive }).ToListAsync()));
        g.MapPost("/", async (MaterialFamilyRequest request, NanchesoftDbContext db) => await UpsertMaterialFamilyAsync(null, request, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialFamilyRequest request, NanchesoftDbContext db) => await UpsertMaterialFamilyAsync(id, request, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<MaterialFamily>(db, id, x => db.MaterialSubfamilies.AnyAsync(y => y.MaterialFamilyId == x.Id), "The material family has related subfamilies."));
    }

    private static void MapMaterialSubfamilies(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-subfamilies").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.MaterialSubfamilies.AsNoTracking().Include(x => x.MaterialFamily).OrderBy(x => x.Code).Select(x => new MaterialSubfamilyDto { MaterialSubfamilyId = x.Id, CompanyId = x.CompanyId, MaterialFamilyId = x.MaterialFamilyId, MaterialFamilyName = x.MaterialFamily != null ? x.MaterialFamily.Name : string.Empty, Code = x.Code, Name = x.Name, MaterialType = x.MaterialType, IsDirectMaterial = x.IsDirectMaterial, Notes = x.Notes, IsActive = x.IsActive }).ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (MaterialSubfamilyRequest request, NanchesoftDbContext db) => await UpsertMaterialSubfamilyAsync(null, request, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialSubfamilyRequest request, NanchesoftDbContext db) => await UpsertMaterialSubfamilyAsync(id, request, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<MaterialSubfamily>(db, id, x => db.MaterialItems.AnyAsync(y => y.MaterialSubfamilyId == x.Id), "The material subfamily has related materials."));
    }

    private static void MapMaterialItems(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/material-items").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.MaterialItems.AsNoTracking().Include(x => x.MaterialSubfamily).Include(x => x.PurchaseUnit).Include(x => x.IssueUnit).Include(x => x.Supplier).OrderBy(x => x.Code)
                .Select(x => new MaterialItemDto { MaterialItemId = x.Id, CompanyId = x.CompanyId, MaterialSubfamilyId = x.MaterialSubfamilyId, MaterialSubfamilyName = x.MaterialSubfamily != null ? x.MaterialSubfamily.Name : string.Empty, PurchaseUnitId = x.PurchaseUnitId, PurchaseUnitName = x.PurchaseUnit != null ? x.PurchaseUnit.Name : string.Empty, IssueUnitId = x.IssueUnitId, IssueUnitName = x.IssueUnit != null ? x.IssueUnit.Name : string.Empty, SupplierId = x.SupplierId, SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty, Code = x.Code, Name = x.Name, Description = x.Description, LegacyMaterialName = x.LegacyMaterialName, AuthorizedCost = x.AuthorizedCost, LastPurchaseCost = x.LastPurchaseCost, StandardCost = x.StandardCost, CostStatus = x.CostStatus, IsServiceItem = x.IsServiceItem, Notes = x.Notes, IsActive = x.IsActive }).ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (MaterialItemRequest request, NanchesoftDbContext db) => await UpsertMaterialItemAsync(null, request, db));
        g.MapPut("/{id:guid}", async (Guid id, MaterialItemRequest request, NanchesoftDbContext db) => await UpsertMaterialItemAsync(id, request, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<MaterialItem>(db, id, async x => await db.FinishedProducts.AnyAsync(y => y.MainMaterialItemId == x.Id) || await db.FinishedProductMaterials.AnyAsync(y => y.MaterialItemId == x.Id), "The material item is used by products."));
    }

    private static void MapFinishedProducts(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/finished-products").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.FinishedProducts.AsNoTracking()
                .Include(x => x.ProductStyle).Include(x => x.ItemModel).Include(x => x.ItemBrand)
                .Include(x => x.ProductLeatherType).Include(x => x.ProductColor).Include(x => x.ProductToeCap)
                .Include(x => x.ProductSole).Include(x => x.ProductSoleColor).Include(x => x.ProductFolioPattern)
                .Include(x => x.ProductSizeRun).Include(x => x.MainMaterialItem)
                .OrderBy(x => x.Code)
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
                    ProductLineId = x.ProductLineId, ProductLastId = x.ProductLastId,
                    MainMaterialItemId = x.MainMaterialItemId, MainMaterialItemName = x.MainMaterialItem != null ? x.MainMaterialItem.Name : string.Empty,
                    Code = x.Code, Name = x.Name, BillingName = x.BillingName,
                    HasPhoto = x.HasPhoto, HasConsumptionDefinition = x.HasConsumptionDefinition,
                    HasMaterialAssignments = x.HasMaterialAssignments, IsAuthorizedForExplosion = x.IsAuthorizedForExplosion,
                    Notes = x.Notes, IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (FinishedProductRequest request, NanchesoftDbContext db) => await UpsertFinishedProductAsync(null, request, db));
        g.MapPut("/{id:guid}", async (Guid id, FinishedProductRequest request, NanchesoftDbContext db) => await UpsertFinishedProductAsync(id, request, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<FinishedProduct>(db, id, async x => await db.FinishedProductMaterials.AnyAsync(y => y.FinishedProductId == x.Id) || await db.ProductConsumptionProfiles.AnyAsync(y => y.FinishedProductId == x.Id), "The finished product has materials or consumptions."));
    }

    private static void MapProductComponents(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/product-components").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (NanchesoftDbContext db) => Results.Ok(await db.ProductComponents.AsNoTracking().Include(x => x.ConsumptionUnit).OrderBy(x => x.Code).Select(x => new ProductComponentDto { ProductComponentId = x.Id, CompanyId = x.CompanyId, ConsumptionUnitId = x.ConsumptionUnitId, ConsumptionUnitName = x.ConsumptionUnit != null ? x.ConsumptionUnit.Name : string.Empty, Code = x.Code, Name = x.Name, ProductionPhase = x.ProductionPhase, WarehouseDeliveryRole = x.WarehouseDeliveryRole, DefaultConsumption = x.DefaultConsumption, ActivateForAllProducts = x.ActivateForAllProducts, ShowOnProductionCard = x.ShowOnProductionCard, Notes = x.Notes, IsActive = x.IsActive }).ToListAsync()));
        g.MapPost("/", async (ProductComponentRequest request, NanchesoftDbContext db) => await UpsertProductComponentAsync(null, request, db));
        g.MapPut("/{id:guid}", async (Guid id, ProductComponentRequest request, NanchesoftDbContext db) => await UpsertProductComponentAsync(id, request, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ProductComponent>(db, id, async x => await db.FinishedProductMaterials.AnyAsync(y => y.ProductComponentId == x.Id) || await db.ProductConsumptionProfiles.AnyAsync(y => y.ProductComponentId == x.Id), "The component has material or consumption rows."));
    }

    private static void MapFinishedProductMaterials(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/finished-product-materials").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.FinishedProductMaterials.AsNoTracking().Include(x => x.FinishedProduct).Include(x => x.ProductComponent).Include(x => x.MaterialItem).OrderBy(x => x.CreatedAt)
                .Select(x => new FinishedProductMaterialDto { FinishedProductMaterialId = x.Id, CompanyId = x.CompanyId, FinishedProductId = x.FinishedProductId, FinishedProductName = x.FinishedProduct != null ? x.FinishedProduct.Name : string.Empty, ProductComponentId = x.ProductComponentId, ProductComponentName = x.ProductComponent != null ? x.ProductComponent.Name : string.Empty, MaterialItemId = x.MaterialItemId, MaterialItemName = x.MaterialItem != null ? x.MaterialItem.Name : string.Empty, SizeCode = x.SizeCode, Quantity = x.Quantity, IsRequired = x.IsRequired, Notes = x.Notes, IsActive = x.IsActive }).ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (FinishedProductMaterialRequest request, NanchesoftDbContext db) => await UpsertFinishedProductMaterialAsync(null, request, db));
        g.MapPut("/{id:guid}", async (Guid id, FinishedProductMaterialRequest request, NanchesoftDbContext db) => await UpsertFinishedProductMaterialAsync(id, request, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<FinishedProductMaterial>(db, id, x => Task.FromResult(false), string.Empty));
    }

    private static void MapProductConsumptionProfiles(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/product-consumption-profiles").WithTags("ProductCatalogOperations");
        g.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.ProductConsumptionProfiles.AsNoTracking().Include(x => x.FinishedProduct).Include(x => x.ProductComponent).OrderBy(x => x.CreatedAt)
                .Select(x => new ProductConsumptionProfileDto { ProductConsumptionProfileId = x.Id, CompanyId = x.CompanyId, FinishedProductId = x.FinishedProductId, FinishedProductName = x.FinishedProduct != null ? x.FinishedProduct.Name : string.Empty, ProductComponentId = x.ProductComponentId, ProductComponentName = x.ProductComponent != null ? x.ProductComponent.Name : string.Empty, SizeCode = x.SizeCode, Pieces = x.Pieces, Consumption = x.Consumption, Status = x.Status, Notes = x.Notes, IsActive = x.IsActive }).ToListAsync();
            return Results.Ok(rows);
        });
        g.MapPost("/", async (ProductConsumptionProfileRequest request, NanchesoftDbContext db) => await UpsertProductConsumptionProfileAsync(null, request, db));
        g.MapPut("/{id:guid}", async (Guid id, ProductConsumptionProfileRequest request, NanchesoftDbContext db) => await UpsertProductConsumptionProfileAsync(id, request, db));
        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ProductConsumptionProfile>(db, id, x => Task.FromResult(false), string.Empty));
    }

    private static void MapLookups(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/material-families/options", async (NanchesoftDbContext db) => Results.Ok(await db.MaterialFamilies.AsNoTracking().OrderBy(x => x.Code).Select(x => new MaterialFamilyOptionDto { MaterialFamilyId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync())).WithTags("ProductCatalogOperations");
        app.MapGet("/api/products/material-subfamilies/options", async (NanchesoftDbContext db) => Results.Ok(await db.MaterialSubfamilies.AsNoTracking().OrderBy(x => x.Code).Select(x => new MaterialSubfamilyOptionDto { MaterialSubfamilyId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync())).WithTags("ProductCatalogOperations");
        app.MapGet("/api/products/material-items/options", async (NanchesoftDbContext db) => Results.Ok(await db.MaterialItems.AsNoTracking().OrderBy(x => x.Code).Select(x => new MaterialItemOptionDto { MaterialItemId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync())).WithTags("ProductCatalogOperations");
        app.MapGet("/api/products/finished-products/options", async (NanchesoftDbContext db) => Results.Ok(await db.FinishedProducts.AsNoTracking().OrderBy(x => x.Code).Select(x => new FinishedProductOptionDto { FinishedProductId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync())).WithTags("ProductCatalogOperations");
        app.MapGet("/api/products/product-components/options", async (NanchesoftDbContext db) => Results.Ok(await db.ProductComponents.AsNoTracking().OrderBy(x => x.Code).Select(x => new ProductComponentOptionDto { ProductComponentId = x.Id, Code = x.Code, Name = x.Name }).ToListAsync())).WithTags("ProductCatalogOperations");
    }

    // upserts
    private static async Task<IResult> UpsertMaterialFamilyAsync(Guid? id, MaterialFamilyRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = N(request.Code, true); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Code and Name are required." });
        if (await db.MaterialFamilies.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Material family code already exists." });
        var entity = id.HasValue ? await db.MaterialFamilies.FirstOrDefaultAsync(x => x.Id == id.Value) : null;
        if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialFamily { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.Code = code; entity.Name = N(request.Name); entity.InventoryGroup = N(request.InventoryGroup); entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialFamilies.Add(entity);
        await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertMaterialSubfamilyAsync(Guid? id, MaterialSubfamilyRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        if (request.MaterialFamilyId == Guid.Empty) return Results.BadRequest(new { message = "MaterialFamilyId is required." });
        var code = N(request.Code, true); if (await db.MaterialSubfamilies.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.MaterialFamilyId == request.MaterialFamilyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Subfamily code already exists." });
        var entity = id.HasValue ? await db.MaterialSubfamilies.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialSubfamily { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.MaterialFamilyId = request.MaterialFamilyId; entity.Code = code; entity.Name = N(request.Name); entity.MaterialType = N(request.MaterialType); entity.IsDirectMaterial = request.IsDirectMaterial; entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialSubfamilies.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertMaterialItemAsync(Guid? id, MaterialItemRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        if (request.MaterialSubfamilyId == Guid.Empty) return Results.BadRequest(new { message = "MaterialSubfamilyId is required." });
        var code = N(request.Code, true); if (await db.MaterialItems.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Material item code already exists." });
        var entity = id.HasValue ? await db.MaterialItems.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new MaterialItem { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.MaterialSubfamilyId = request.MaterialSubfamilyId; entity.PurchaseUnitId = request.PurchaseUnitId; entity.IssueUnitId = request.IssueUnitId; entity.SupplierId = request.SupplierId; entity.Code = code; entity.Name = N(request.Name); entity.Description = N(request.Description); entity.LegacyMaterialName = N(request.LegacyMaterialName); entity.AuthorizedCost = request.AuthorizedCost; entity.LastPurchaseCost = request.LastPurchaseCost; entity.StandardCost = request.StandardCost; entity.CostStatus = N(request.CostStatus); entity.IsServiceItem = request.IsServiceItem; entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.MaterialItems.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertFinishedProductAsync(Guid? id, FinishedProductRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = N(request.Code, true); if (await db.FinishedProducts.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Finished product code already exists." });
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

    private static async Task<IResult> UpsertProductComponentAsync(Guid? id, ProductComponentRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = N(request.Code, true); if (await db.ProductComponents.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code && (!id.HasValue || x.Id != id.Value))) return Results.BadRequest(new { message = "Component code already exists." });
        var entity = id.HasValue ? await db.ProductComponents.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new ProductComponent { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.ConsumptionUnitId = request.ConsumptionUnitId; entity.Code = code; entity.Name = N(request.Name); entity.ProductionPhase = N(request.ProductionPhase); entity.WarehouseDeliveryRole = N(request.WarehouseDeliveryRole); entity.DefaultConsumption = request.DefaultConsumption; entity.ActivateForAllProducts = request.ActivateForAllProducts; entity.ShowOnProductionCard = request.ShowOnProductionCard; entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.ProductComponents.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertFinishedProductMaterialAsync(Guid? id, FinishedProductMaterialRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var entity = id.HasValue ? await db.FinishedProductMaterials.FirstOrDefaultAsync(x => x.Id == id.Value) : null; if (id.HasValue && entity is null) return Results.NotFound();
        entity ??= new FinishedProductMaterial { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
        entity.FinishedProductId = request.FinishedProductId; entity.ProductComponentId = request.ProductComponentId; entity.MaterialItemId = request.MaterialItemId; entity.SizeCode = N(request.SizeCode); entity.Quantity = request.Quantity; entity.IsRequired = request.IsRequired; entity.Notes = N(request.Notes); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        if (!id.HasValue) db.FinishedProductMaterials.Add(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }
    private static async Task<IResult> UpsertProductConsumptionProfileAsync(Guid? id, ProductConsumptionProfileRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db); if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
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

public sealed class MaterialFamilyOptionDto { public Guid MaterialFamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class MaterialSubfamilyOptionDto { public Guid MaterialSubfamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class MaterialItemOptionDto { public Guid MaterialItemId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class FinishedProductOptionDto { public Guid FinishedProductId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class ProductComponentOptionDto { public Guid ProductComponentId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }

public class MaterialFamilyRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string InventoryGroup { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialFamilyDto : MaterialFamilyRequest { public Guid MaterialFamilyId { get; set; } public Guid CompanyId { get; set; } }
public class MaterialSubfamilyRequest { public Guid MaterialFamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string MaterialType { get; set; } = string.Empty; public bool IsDirectMaterial { get; set; } = true; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialSubfamilyDto : MaterialSubfamilyRequest { public Guid MaterialSubfamilyId { get; set; } public Guid CompanyId { get; set; } public string MaterialFamilyName { get; set; } = string.Empty; }
public class MaterialItemRequest { public Guid MaterialSubfamilyId { get; set; } public Guid? PurchaseUnitId { get; set; } public Guid? IssueUnitId { get; set; } public Guid? SupplierId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string LegacyMaterialName { get; set; } = string.Empty; public decimal AuthorizedCost { get; set; } public decimal LastPurchaseCost { get; set; } public decimal StandardCost { get; set; } public string CostStatus { get; set; } = string.Empty; public bool IsServiceItem { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialItemDto : MaterialItemRequest { public Guid MaterialItemId { get; set; } public Guid CompanyId { get; set; } public string MaterialSubfamilyName { get; set; } = string.Empty; public string PurchaseUnitName { get; set; } = string.Empty; public string IssueUnitName { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; }
public class FinishedProductRequest { public Guid? ProductStyleId { get; set; } public Guid? ItemModelId { get; set; } public Guid? ItemBrandId { get; set; } public Guid? ProductLeatherTypeId { get; set; } public Guid? ProductColorId { get; set; } public Guid? ProductToeCapId { get; set; } public Guid? ProductSoleId { get; set; } public Guid? ProductSoleColorId { get; set; } public Guid? ProductFolioPatternId { get; set; } public Guid? ProductSizeRunId { get; set; } public Guid? ProductLineId { get; set; } public Guid? ProductLastId { get; set; } public Guid? MainMaterialItemId { get; set; } public string Code { get; set; } = string.Empty; public string? Name { get; set; } public string BillingName { get; set; } = string.Empty; public bool HasPhoto { get; set; } public bool HasConsumptionDefinition { get; set; } public bool HasMaterialAssignments { get; set; } public bool IsAuthorizedForExplosion { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class FinishedProductDto : FinishedProductRequest { public Guid FinishedProductId { get; set; } public Guid CompanyId { get; set; } public string ProductStyleName { get; set; } = string.Empty; public string ItemModelName { get; set; } = string.Empty; public string ItemBrandName { get; set; } = string.Empty; public string ProductLeatherTypeName { get; set; } = string.Empty; public string ProductColorName { get; set; } = string.Empty; public string ProductToeCapName { get; set; } = string.Empty; public string ProductSoleName { get; set; } = string.Empty; public string ProductSoleColorName { get; set; } = string.Empty; public string ProductFolioPatternName { get; set; } = string.Empty; public string ProductSizeRunName { get; set; } = string.Empty; public string MainMaterialItemName { get; set; } = string.Empty; }
public class ProductComponentRequest { public Guid? ConsumptionUnitId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string ProductionPhase { get; set; } = string.Empty; public string WarehouseDeliveryRole { get; set; } = string.Empty; public decimal DefaultConsumption { get; set; } public bool ActivateForAllProducts { get; set; } public bool ShowOnProductionCard { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductComponentDto : ProductComponentRequest { public Guid ProductComponentId { get; set; } public Guid CompanyId { get; set; } public string ConsumptionUnitName { get; set; } = string.Empty; }
public class FinishedProductMaterialRequest { public Guid FinishedProductId { get; set; } public Guid ProductComponentId { get; set; } public Guid MaterialItemId { get; set; } public string SizeCode { get; set; } = string.Empty; public decimal Quantity { get; set; } public bool IsRequired { get; set; } = true; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class FinishedProductMaterialDto : FinishedProductMaterialRequest { public Guid FinishedProductMaterialId { get; set; } public Guid CompanyId { get; set; } public string FinishedProductName { get; set; } = string.Empty; public string ProductComponentName { get; set; } = string.Empty; public string MaterialItemName { get; set; } = string.Empty; }
public class ProductConsumptionProfileRequest { public Guid FinishedProductId { get; set; } public Guid ProductComponentId { get; set; } public string SizeCode { get; set; } = string.Empty; public int Pieces { get; set; } public decimal Consumption { get; set; } public string Status { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductConsumptionProfileDto : ProductConsumptionProfileRequest { public Guid ProductConsumptionProfileId { get; set; } public Guid CompanyId { get; set; } public string FinishedProductName { get; set; } = string.Empty; public string ProductComponentName { get; set; } = string.Empty; }
