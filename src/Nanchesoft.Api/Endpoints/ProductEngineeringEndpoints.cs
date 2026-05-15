using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductEngineeringEndpoints
{
    public static IEndpointRouteBuilder MapProductEngineeringEndpoints(this IEndpointRouteBuilder app)
    {
        MapUnitConversions(app);
        MapSizeRuns(app);
        MapFamilies(app);
        MapLasts(app);
        MapLines(app);
        MapStyles(app);
        MapEmbroideryPatterns(app);
        MapEngineeringProfiles(app);
        MapItemOptions(app);
        ProductCatalogOperationsEndpoints.MapProductCatalogOperations(app);
        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return company is null ? (null, null) : (company.TenantId, company.Id);
    }

    private static string NormalizeUpper(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    private static string NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static void MapUnitConversions(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/unit-conversions").WithTags("ProductEngineering");
        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.UnitConversions.AsNoTracking()
                .Include(x => x.FromUnit)
                .Include(x => x.ToUnit)
                .OrderBy(x => x.FromUnit!.Code)
                .ThenBy(x => x.ToUnit!.Code)
                .Select(x => new UnitConversionDto
                {
                    UnitConversionId = x.Id,
                    CompanyId = x.CompanyId,
                    FromUnitId = x.FromUnitId,
                    FromUnitCode = x.FromUnit != null ? x.FromUnit.Code : string.Empty,
                    ToUnitId = x.ToUnitId,
                    ToUnitCode = x.ToUnit != null ? x.ToUnit.Code : string.Empty,
                    ConversionFactor = x.ConversionFactor,
                    IsBidirectional = x.IsBidirectional,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });
        group.MapPost("/", async (UnitConversionRequest request, NanchesoftDbContext db) =>
        {
            var ctx = await ResolveDefaultContextAsync(db);
            if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue)
                return Results.BadRequest(new { message = "No existe contexto para conversiones." });
            if (request.FromUnitId == Guid.Empty || request.ToUnitId == Guid.Empty)
                return Results.BadRequest(new { message = "FromUnitId y ToUnitId son obligatorios." });
            if (await db.UnitConversions.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.FromUnitId == request.FromUnitId && x.ToUnitId == request.ToUnitId))
                return Results.BadRequest(new { message = "Ya existe esa conversión." });
            var entity = new UnitConversion
            {
                TenantId = ctx.TenantId.Value,
                CompanyId = ctx.CompanyId.Value,
                FromUnitId = request.FromUnitId,
                ToUnitId = request.ToUnitId,
                ConversionFactor = request.ConversionFactor <= 0 ? 1m : request.ConversionFactor,
                IsBidirectional = request.IsBidirectional,
                Notes = NormalizeText(request.Notes),
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };
            db.UnitConversions.Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        });
        group.MapPut("/{id:guid}", async (Guid id, UnitConversionRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.UnitConversions.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            entity.FromUnitId = request.FromUnitId;
            entity.ToUnitId = request.ToUnitId;
            entity.ConversionFactor = request.ConversionFactor <= 0 ? entity.ConversionFactor : request.ConversionFactor;
            entity.IsBidirectional = request.IsBidirectional;
            entity.Notes = NormalizeText(request.Notes);
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.UnitConversions.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            db.UnitConversions.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static void MapSizeRuns(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/size-runs").WithTags("ProductEngineering");
        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.ProductSizeRuns.AsNoTracking()
                .Include(x => x.Sizes)
                .OrderBy(x => x.Code)
                .Select(x => new ProductSizeRunDto
                {
                    ProductSizeRunId = x.Id,
                    CompanyId = x.CompanyId,
                    Code = x.Code,
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                    IsUniqueSizeRun = x.IsUniqueSizeRun,
                    SizeCount = x.SizeCount,
                    SizesPreview = string.Join(", ", x.Sizes.OrderBy(s => s.Sequence).Select(s => s.DisplayLabel)),
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });
        group.MapPost("/", async (ProductSizeRunRequest request, NanchesoftDbContext db) =>
        {
            var ctx = await ResolveDefaultContextAsync(db);
            if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
            var code = NormalizeUpper(request.Code);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Code y Name son obligatorios." });
            if (await db.ProductSizeRuns.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code)) return Results.BadRequest(new { message = "Ya existe la corrida." });
            var entity = new ProductSizeRun
            {
                TenantId = ctx.TenantId.Value,
                CompanyId = ctx.CompanyId.Value,
                Code = code,
                Name = NormalizeText(request.Name),
                DisplayName = NormalizeText(request.DisplayName),
                IsUniqueSizeRun = request.IsUniqueSizeRun,
                SizeCount = request.SizeCount,
                IsActive = request.IsActive,
                CreatedBy = "web-api"
            };
            db.ProductSizeRuns.Add(entity);
            await db.SaveChangesAsync();
            await ReplaceRunSizesAsync(db, entity.Id, request.SizeDefinitions, "web-api");
            return Results.Ok(new { success = true, id = entity.Id });
        });
        group.MapPut("/{id:guid}", async (Guid id, ProductSizeRunRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.ProductSizeRuns.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            entity.Code = NormalizeUpper(request.Code);
            entity.Name = NormalizeText(request.Name);
            entity.DisplayName = NormalizeText(request.DisplayName);
            entity.IsUniqueSizeRun = request.IsUniqueSizeRun;
            entity.SizeCount = request.SizeCount;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            await ReplaceRunSizesAsync(db, entity.Id, request.SizeDefinitions, "web-api");
            return Results.Ok(new { success = true });
        });
        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.ProductSizeRuns.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();
            if (await db.ItemEngineeringProfiles.AnyAsync(x => x.ProductSizeRunId == id)) return Results.BadRequest(new { message = "La corrida está ligada a perfiles de ingeniería." });
            db.ProductSizeRuns.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static async Task ReplaceRunSizesAsync(NanchesoftDbContext db, Guid runId, string? definitions, string user)
    {
        var existing = await db.ProductSizeRunSizes.Where(x => x.ProductSizeRunId == runId).ToListAsync();
        if (existing.Count > 0)
        {
            db.ProductSizeRunSizes.RemoveRange(existing);
            await db.SaveChangesAsync();
        }
        var parts = (definitions ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (parts.Count == 0) parts.Add("U");
        var rows = parts.Select((value, index) => new ProductSizeRunSize
        {
            ProductSizeRunId = runId,
            Sequence = index + 1,
            SizeCode = value.Trim(),
            DisplayLabel = value.Trim(),
            BarcodeLabel = value.Trim(),
            CreatedBy = user
        }).ToList();
        db.ProductSizeRunSizes.AddRange(rows);
        await db.SaveChangesAsync();
        var parent = await db.ProductSizeRuns.FirstOrDefaultAsync(x => x.Id == runId);
        if (parent is not null)
        {
            parent.SizeCount = rows.Count;
            parent.UpdatedAt = DateTime.UtcNow;
            parent.UpdatedBy = user;
            await db.SaveChangesAsync();
        }
    }

    private static void MapFamilies(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/families").WithTags("ProductEngineering");
        group.MapGet("/", async (NanchesoftDbContext db) => Results.Ok(await db.ProductFamilies.AsNoTracking().OrderBy(x => x.Code).Select(x => new ProductFamilyDto { ProductFamilyId = x.Id, CompanyId = x.CompanyId, Code = x.Code, Name = x.Name, StatisticsGroup = x.StatisticsGroup, IsFinishedProductFamily = x.IsFinishedProductFamily, IsActive = x.IsActive }).ToListAsync()));
        group.MapPost("/", async (ProductFamilyRequest request, NanchesoftDbContext db) => await UpsertProductFamilyAsync(null, request, db));
        group.MapPut("/{id:guid}", async (Guid id, ProductFamilyRequest request, NanchesoftDbContext db) => await UpsertProductFamilyAsync(id, request, db));
        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ProductFamily>(db, id, x => db.ProductLines.AnyAsync(y => y.ProductFamilyId == x.Id), "La familia tiene líneas relacionadas."));
    }

    private static void MapLasts(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/lasts").WithTags("ProductEngineering");
        group.MapGet("/", async (NanchesoftDbContext db) => Results.Ok(await db.ProductLasts.AsNoTracking().OrderBy(x => x.Code).Select(x => new ProductLastDto { ProductLastId = x.Id, CompanyId = x.CompanyId, Code = x.Code, Name = x.Name, WidthReference = x.WidthReference, IsActive = x.IsActive }).ToListAsync()));
        group.MapPost("/", async (ProductLastRequest request, NanchesoftDbContext db) => await UpsertProductLastAsync(null, request, db));
        group.MapPut("/{id:guid}", async (Guid id, ProductLastRequest request, NanchesoftDbContext db) => await UpsertProductLastAsync(id, request, db));
        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ProductLast>(db, id, async x => await db.ProductLines.AnyAsync(y => y.ProductLastId == x.Id) || await db.ProductStyles.AnyAsync(y => y.ProductLastId == x.Id), "La horma tiene líneas o estilos relacionados."));
    }

    private static void MapLines(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/lines").WithTags("ProductEngineering");
        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.ProductLines.AsNoTracking().Include(x => x.ProductFamily).Include(x => x.ProductLast).OrderBy(x => x.Code)
                .Select(x => new ProductLineDto { ProductLineId = x.Id, CompanyId = x.CompanyId, ProductFamilyId = x.ProductFamilyId, ProductFamilyName = x.ProductFamily != null ? x.ProductFamily.Name : string.Empty, ProductLastId = x.ProductLastId, ProductLastName = x.ProductLast != null ? x.ProductLast.Name : string.Empty, Code = x.Code, Name = x.Name, ShortName = x.ShortName, AllowsDiscount = x.AllowsDiscount, IsActive = x.IsActive }).ToListAsync();
            return Results.Ok(rows);
        });
        group.MapPost("/", async (ProductLineRequest request, NanchesoftDbContext db) => await UpsertProductLineAsync(null, request, db));
        group.MapPut("/{id:guid}", async (Guid id, ProductLineRequest request, NanchesoftDbContext db) => await UpsertProductLineAsync(id, request, db));
        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ProductLine>(db, id, x => db.ProductStyles.AnyAsync(y => y.ProductLineId == x.Id), "La línea tiene estilos relacionados."));
    }

    private static void MapStyles(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/styles").WithTags("ProductEngineering");
        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.ProductStyles.AsNoTracking().Include(x => x.ProductLine).Include(x => x.ProductLast).OrderBy(x => x.Code)
                .Select(x => new ProductStyleDto { ProductStyleId = x.Id, CompanyId = x.CompanyId, ProductLineId = x.ProductLineId, ProductLineName = x.ProductLine != null ? x.ProductLine.Name : string.Empty, ProductLastId = x.ProductLastId, ProductLastName = x.ProductLast != null ? x.ProductLast.Name : string.Empty, Code = x.Code, Name = x.Name, CustomerLabel1 = x.CustomerLabel1, CustomerLabel2 = x.CustomerLabel2, ColorLabel = x.ColorLabel, DieCutReference = x.DieCutReference, MaxLotSize = x.MaxLotSize, HasAuthorizedConsumption = x.HasAuthorizedConsumption, HandlesFractionsByStyle = x.HandlesFractionsByStyle, OutsourcedProcessName = x.OutsourcedProcessName, PhotoUrl = x.PhotoUrl, IsActive = x.IsActive }).ToListAsync();
            return Results.Ok(rows);
        });
        group.MapPost("/", async (ProductStyleRequest request, NanchesoftDbContext db) => await UpsertProductStyleAsync(null, request, db));
        group.MapPut("/{id:guid}", async (Guid id, ProductStyleRequest request, NanchesoftDbContext db) => await UpsertProductStyleAsync(id, request, db));
        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ProductStyle>(db, id, x => db.ItemEngineeringProfiles.AnyAsync(y => y.ProductStyleId == x.Id), "El estilo está ligado a perfiles de ingeniería."));
    }

    private static void MapEmbroideryPatterns(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/embroidery-patterns").WithTags("ProductEngineering");
        group.MapGet("/", async (NanchesoftDbContext db) => Results.Ok(await db.EmbroideryPatterns.AsNoTracking().OrderBy(x => x.Sequence).ThenBy(x => x.Code).Select(x => new EmbroideryPatternDto { EmbroideryPatternId = x.Id, CompanyId = x.CompanyId, Code = x.Code, Name = x.Name, Sequence = x.Sequence, IsActive = x.IsActive }).ToListAsync()));
        group.MapPost("/", async (EmbroideryPatternRequest request, NanchesoftDbContext db) => await UpsertEmbroideryPatternAsync(null, request, db));
        group.MapPut("/{id:guid}", async (Guid id, EmbroideryPatternRequest request, NanchesoftDbContext db) => await UpsertEmbroideryPatternAsync(id, request, db));
        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<EmbroideryPattern>(db, id, x => db.ItemEngineeringProfiles.AnyAsync(y => y.EmbroideryPatternId == x.Id), "El bordado está ligado a perfiles de ingeniería."));
    }

    private static void MapEngineeringProfiles(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/item-engineering-profiles").WithTags("ProductEngineering");
        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.ItemEngineeringProfiles.AsNoTracking()
                .Include(x => x.Item)
                .Include(x => x.ProductStyle)
                .Include(x => x.ProductSizeRun)
                .Include(x => x.EmbroideryPattern)
                .Include(x => x.PrimaryMaterialItem)
                .OrderBy(x => x.Item!.Code)
                .Select(x => new ItemEngineeringProfileDto
                {
                    ItemEngineeringProfileId = x.Id,
                    CompanyId = x.CompanyId,
                    ItemId = x.ItemId,
                    ItemCode = x.Item != null ? x.Item.Code : string.Empty,
                    ItemName = x.Item != null ? x.Item.Name : string.Empty,
                    ProductStyleId = x.ProductStyleId,
                    ProductStyleName = x.ProductStyle != null ? x.ProductStyle.Name : string.Empty,
                    ProductSizeRunId = x.ProductSizeRunId,
                    ProductSizeRunName = x.ProductSizeRun != null ? x.ProductSizeRun.Name : string.Empty,
                    EmbroideryPatternId = x.EmbroideryPatternId,
                    EmbroideryPatternName = x.EmbroideryPattern != null ? x.EmbroideryPattern.Name : string.Empty,
                    PrimaryMaterialItemId = x.PrimaryMaterialItemId,
                    PrimaryMaterialItemName = x.PrimaryMaterialItem != null ? x.PrimaryMaterialItem.Name : string.Empty,
                    FolioPattern = x.FolioPattern,
                    TechnicalSheetMode = x.TechnicalSheetMode,
                    ProcessVoucherProfile = x.ProcessVoucherProfile,
                    HasPhoto = x.HasPhoto,
                    HasConsumptionDefinition = x.HasConsumptionDefinition,
                    HasMaterialAssignments = x.HasMaterialAssignments,
                    IsAuthorizedForExplosion = x.IsAuthorizedForExplosion,
                    IsActive = x.IsActive
                }).ToListAsync();
            return Results.Ok(rows);
        });
        group.MapPost("/", async (ItemEngineeringProfileRequest request, NanchesoftDbContext db) => await UpsertEngineeringProfileAsync(null, request, db));
        group.MapPut("/{id:guid}", async (Guid id, ItemEngineeringProfileRequest request, NanchesoftDbContext db) => await UpsertEngineeringProfileAsync(id, request, db));
        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) => await DeleteEntityAsync<ItemEngineeringProfile>(db, id));
    }


    private static void MapItemOptions(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/items/options", async (NanchesoftDbContext db) =>
        {
            var rows = await db.Items.AsNoTracking()
                .OrderBy(x => x.Code)
                .Select(x => new ItemOptionDto
                {
                    ItemId = x.Id,
                    Code = x.Code,
                    Name = x.Name
                }).ToListAsync();
            return Results.Ok(rows);
        }).WithTags("ProductEngineering");
    }

    private static async Task<IResult> UpsertProductFamilyAsync(Guid? id, ProductFamilyRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = NormalizeUpper(request.Code);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Code y Name son obligatorios." });
        if (id.HasValue)
        {
            var entity = await db.ProductFamilies.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (entity is null) return Results.NotFound();
            entity.Code = code; entity.Name = NormalizeText(request.Name); entity.StatisticsGroup = NormalizeText(request.StatisticsGroup); entity.IsFinishedProductFamily = request.IsFinishedProductFamily; entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api"; await db.SaveChangesAsync(); return Results.Ok(new { success = true });
        }
        if (await db.ProductFamilies.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code)) return Results.BadRequest(new { message = "Ya existe la familia." });
        var created = new ProductFamily { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, Code = code, Name = NormalizeText(request.Name), StatisticsGroup = NormalizeText(request.StatisticsGroup), IsFinishedProductFamily = request.IsFinishedProductFamily, IsActive = request.IsActive, CreatedBy = "web-api" };
        db.ProductFamilies.Add(created); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = created.Id });
    }

    private static async Task<IResult> UpsertProductLastAsync(Guid? id, ProductLastRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = NormalizeUpper(request.Code);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Code y Name son obligatorios." });
        if (id.HasValue)
        {
            var entity = await db.ProductLasts.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (entity is null) return Results.NotFound();
            entity.Code = code; entity.Name = NormalizeText(request.Name); entity.WidthReference = NormalizeText(request.WidthReference); entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api"; await db.SaveChangesAsync(); return Results.Ok(new { success = true });
        }
        if (await db.ProductLasts.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code)) return Results.BadRequest(new { message = "Ya existe la horma." });
        var created = new ProductLast { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, Code = code, Name = NormalizeText(request.Name), WidthReference = NormalizeText(request.WidthReference), IsActive = request.IsActive, CreatedBy = "web-api" };
        db.ProductLasts.Add(created); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = created.Id });
    }

    private static async Task<IResult> UpsertProductLineAsync(Guid? id, ProductLineRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = NormalizeUpper(request.Code);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Code y Name son obligatorios." });
        if (id.HasValue)
        {
            var entity = await db.ProductLines.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (entity is null) return Results.NotFound();
            entity.ProductFamilyId = request.ProductFamilyId; entity.ProductLastId = request.ProductLastId; entity.Code = code; entity.Name = NormalizeText(request.Name); entity.ShortName = NormalizeText(request.ShortName); entity.AllowsDiscount = request.AllowsDiscount; entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api"; await db.SaveChangesAsync(); return Results.Ok(new { success = true });
        }
        if (await db.ProductLines.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code)) return Results.BadRequest(new { message = "Ya existe la línea." });
        var created = new ProductLine { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, ProductFamilyId = request.ProductFamilyId, ProductLastId = request.ProductLastId, Code = code, Name = NormalizeText(request.Name), ShortName = NormalizeText(request.ShortName), AllowsDiscount = request.AllowsDiscount, IsActive = request.IsActive, CreatedBy = "web-api" };
        db.ProductLines.Add(created); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = created.Id });
    }

    private static async Task<IResult> UpsertProductStyleAsync(Guid? id, ProductStyleRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = NormalizeUpper(request.Code);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Code y Name son obligatorios." });
        ProductStyle entity;
        if (id.HasValue)
        {
            entity = await db.ProductStyles.FirstOrDefaultAsync(x => x.Id == id.Value) ?? throw new InvalidOperationException();
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        }
        else
        {
            if (await db.ProductStyles.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code)) return Results.BadRequest(new { message = "Ya existe el estilo." });
            entity = new ProductStyle { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
            db.ProductStyles.Add(entity);
        }
        entity.ProductLineId = request.ProductLineId; entity.ProductLastId = request.ProductLastId; entity.Code = code; entity.Name = NormalizeText(request.Name); entity.CustomerLabel1 = NormalizeText(request.CustomerLabel1); entity.CustomerLabel2 = NormalizeText(request.CustomerLabel2); entity.ColorLabel = NormalizeText(request.ColorLabel); entity.DieCutReference = NormalizeText(request.DieCutReference); entity.MaxLotSize = request.MaxLotSize; entity.HasAuthorizedConsumption = request.HasAuthorizedConsumption; entity.HandlesFractionsByStyle = request.HandlesFractionsByStyle; entity.TechnicalNotes = NormalizeText(request.TechnicalNotes); entity.ProductionCardNotes = NormalizeText(request.ProductionCardNotes); entity.OutsourcedProcessName = NormalizeText(request.OutsourcedProcessName); entity.PhotoUrl = NormalizeText(request.PhotoUrl); entity.IsActive = request.IsActive; await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpsertEmbroideryPatternAsync(Guid? id, EmbroideryPatternRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        var code = NormalizeUpper(request.Code);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Code y Name son obligatorios." });
        if (id.HasValue)
        {
            var entity = await db.EmbroideryPatterns.FirstOrDefaultAsync(x => x.Id == id.Value); if (entity is null) return Results.NotFound();
            entity.Code = code; entity.Name = NormalizeText(request.Name); entity.Sequence = request.Sequence; entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api"; await db.SaveChangesAsync(); return Results.Ok(new { success = true });
        }
        if (await db.EmbroideryPatterns.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code)) return Results.BadRequest(new { message = "Ya existe el bordado." });
        var created = new EmbroideryPattern { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, Code = code, Name = NormalizeText(request.Name), Sequence = request.Sequence, IsActive = request.IsActive, CreatedBy = "web-api" }; db.EmbroideryPatterns.Add(created); await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = created.Id });
    }

    private static async Task<IResult> UpsertEngineeringProfileAsync(Guid? id, ItemEngineeringProfileRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue) return Results.BadRequest();
        if (request.ItemId == Guid.Empty) return Results.BadRequest(new { message = "ItemId es obligatorio." });
        ItemEngineeringProfile entity;
        if (id.HasValue)
        {
            entity = await db.ItemEngineeringProfiles.FirstOrDefaultAsync(x => x.Id == id.Value) ?? throw new InvalidOperationException();
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = "web-api";
        }
        else
        {
            if (await db.ItemEngineeringProfiles.AnyAsync(x => x.ItemId == request.ItemId)) return Results.BadRequest(new { message = "El item ya tiene perfil de ingeniería." });
            entity = new ItemEngineeringProfile { TenantId = ctx.TenantId.Value, CompanyId = ctx.CompanyId.Value, CreatedBy = "web-api" };
            db.ItemEngineeringProfiles.Add(entity);
        }
        entity.ItemId = request.ItemId; entity.ProductStyleId = request.ProductStyleId; entity.ProductSizeRunId = request.ProductSizeRunId; entity.EmbroideryPatternId = request.EmbroideryPatternId; entity.PrimaryMaterialItemId = request.PrimaryMaterialItemId; entity.FolioPattern = NormalizeText(request.FolioPattern); entity.TechnicalSheetMode = string.IsNullOrWhiteSpace(request.TechnicalSheetMode) ? "style" : request.TechnicalSheetMode.Trim().ToLowerInvariant(); entity.ProcessVoucherProfile = NormalizeText(request.ProcessVoucherProfile); entity.TechnicalSheetNotes = NormalizeText(request.TechnicalSheetNotes); entity.ProductionCardNotes = NormalizeText(request.ProductionCardNotes); entity.HasPhoto = request.HasPhoto; entity.HasConsumptionDefinition = request.HasConsumptionDefinition; entity.HasMaterialAssignments = request.HasMaterialAssignments; entity.IsAuthorizedForExplosion = request.IsAuthorizedForExplosion; entity.IsActive = request.IsActive; await db.SaveChangesAsync(); return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> DeleteEntityAsync<TEntity>(NanchesoftDbContext db, Guid id, Func<TEntity, Task<bool>>? validate = null, string? blockedMessage = null) where TEntity : class
    {
        var entity = await db.Set<TEntity>().FindAsync(id);
        if (entity is null) return Results.NotFound();
        if (validate is not null && await validate(entity)) return Results.BadRequest(new { message = blockedMessage ?? "No se puede eliminar el registro." });
        db.Set<TEntity>().Remove(entity); await db.SaveChangesAsync(); return Results.Ok(new { success = true });
    }
}

public class UnitConversionRequest
{
    public Guid FromUnitId { get; set; }
    public Guid ToUnitId { get; set; }
    public decimal ConversionFactor { get; set; }
    public bool IsBidirectional { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class UnitConversionDto : UnitConversionRequest
{
    public Guid UnitConversionId { get; set; }
    public Guid CompanyId { get; set; }
    public string FromUnitCode { get; set; } = string.Empty;
    public string ToUnitCode { get; set; } = string.Empty;
}

public class ProductSizeRunRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsUniqueSizeRun { get; set; }
    public int SizeCount { get; set; }
    public string? SizeDefinitions { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class ProductSizeRunDto : ProductSizeRunRequest
{
    public Guid ProductSizeRunId { get; set; }
    public Guid CompanyId { get; set; }
    public string SizesPreview { get; set; } = string.Empty;
}

public class ProductFamilyRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StatisticsGroup { get; set; } = string.Empty;
    public bool IsFinishedProductFamily { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
public sealed class ProductFamilyDto : ProductFamilyRequest
{
    public Guid ProductFamilyId { get; set; }
    public Guid CompanyId { get; set; }
}

public class ProductLastRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WidthReference { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
public sealed class ProductLastDto : ProductLastRequest
{
    public Guid ProductLastId { get; set; }
    public Guid CompanyId { get; set; }
}

public class ProductLineRequest
{
    public Guid? ProductFamilyId { get; set; }
    public Guid? ProductLastId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public bool AllowsDiscount { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class ProductLineDto : ProductLineRequest
{
    public Guid ProductLineId { get; set; }
    public Guid CompanyId { get; set; }
    public string ProductFamilyName { get; set; } = string.Empty;
    public string ProductLastName { get; set; } = string.Empty;
}

public class ProductStyleRequest
{
    public Guid? ProductLineId { get; set; }
    public Guid? ProductLastId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CustomerLabel1 { get; set; } = string.Empty;
    public string CustomerLabel2 { get; set; } = string.Empty;
    public string ColorLabel { get; set; } = string.Empty;
    public string DieCutReference { get; set; } = string.Empty;
    public decimal MaxLotSize { get; set; }
    public bool HasAuthorizedConsumption { get; set; }
    public bool HandlesFractionsByStyle { get; set; }
    public string TechnicalNotes { get; set; } = string.Empty;
    public string ProductionCardNotes { get; set; } = string.Empty;
    public string OutsourcedProcessName { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
public sealed class ProductStyleDto : ProductStyleRequest
{
    public Guid ProductStyleId { get; set; }
    public Guid CompanyId { get; set; }
    public string ProductLineName { get; set; } = string.Empty;
    public string ProductLastName { get; set; } = string.Empty;
}

public class EmbroideryPatternRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class EmbroideryPatternDto : EmbroideryPatternRequest
{
    public Guid EmbroideryPatternId { get; set; }
    public Guid CompanyId { get; set; }
}

public class ItemEngineeringProfileRequest
{
    public Guid ItemId { get; set; }
    public Guid? ProductStyleId { get; set; }
    public Guid? ProductSizeRunId { get; set; }
    public Guid? EmbroideryPatternId { get; set; }
    public Guid? PrimaryMaterialItemId { get; set; }
    public string FolioPattern { get; set; } = string.Empty;
    public string TechnicalSheetMode { get; set; } = "style";
    public string ProcessVoucherProfile { get; set; } = string.Empty;
    public string TechnicalSheetNotes { get; set; } = string.Empty;
    public string ProductionCardNotes { get; set; } = string.Empty;
    public bool HasPhoto { get; set; }
    public bool HasConsumptionDefinition { get; set; }
    public bool HasMaterialAssignments { get; set; }
    public bool IsAuthorizedForExplosion { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class ItemEngineeringProfileDto : ItemEngineeringProfileRequest
{
    public Guid ItemEngineeringProfileId { get; set; }
    public Guid CompanyId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ProductStyleName { get; set; } = string.Empty;
    public string ProductSizeRunName { get; set; } = string.Empty;
    public string EmbroideryPatternName { get; set; } = string.Empty;
    public string PrimaryMaterialItemName { get; set; } = string.Empty;
}


public sealed class ItemOptionDto { public Guid ItemId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
