using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductSizeRunEnterpriseEndpoints
{
    public static IEndpointRouteBuilder MapProductSizeRunEnterpriseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/size-runs-enterprise").WithTags("Product Size Runs Enterprise");

        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.ProductSizeRuns.AsNoTracking()
                .Include(x => x.Sizes)
                .OrderBy(x => x.Code)
                .Select(x => new ProductSizeRunEnterpriseDto
                {
                    ProductSizeRunId = x.Id,
                    CompanyId = x.CompanyId,
                    Code = x.Code,
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                    LegacyKey = x.LegacyKey,
                    SecondaryKey = x.SecondaryKey,
                    ConsumptionMode = x.ConsumptionMode,
                    IsUniqueSizeRun = x.IsUniqueSizeRun,
                    SizeCount = x.SizeCount,
                    MiddlePoint = x.MiddlePoint,
                    SizesPreview = string.Join(", ", x.Sizes.OrderBy(s => s.Sequence).Select(s => s.DisplayLabel)),
                    IsActive = x.IsActive,
                    Sizes = x.Sizes.OrderBy(s => s.Sequence).Select(s => new ProductSizeRunSizeDto
                    {
                        ProductSizeRunSizeId = s.Id,
                        Sequence = s.Sequence,
                        SizeCode = s.SizeCode,
                        DisplayLabel = s.DisplayLabel,
                        BarcodeLabel = s.BarcodeLabel,
                        FactorLabel = s.FactorLabel,
                        Proportion = s.Proportion,
                        IsVisible = s.IsVisible,
                        IsActive = s.IsActive
                    }).ToList()
                }).ToListAsync();

            return Results.Ok(rows);
        });

        group.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var item = await db.ProductSizeRuns.AsNoTracking()
                .Include(x => x.Sizes)
                .Where(x => x.Id == id)
                .Select(x => new ProductSizeRunEnterpriseDto
                {
                    ProductSizeRunId = x.Id,
                    CompanyId = x.CompanyId,
                    Code = x.Code,
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                    LegacyKey = x.LegacyKey,
                    SecondaryKey = x.SecondaryKey,
                    ConsumptionMode = x.ConsumptionMode,
                    IsUniqueSizeRun = x.IsUniqueSizeRun,
                    SizeCount = x.SizeCount,
                    MiddlePoint = x.MiddlePoint,
                    SizesPreview = string.Join(", ", x.Sizes.OrderBy(s => s.Sequence).Select(s => s.DisplayLabel)),
                    IsActive = x.IsActive,
                    Sizes = x.Sizes.OrderBy(s => s.Sequence).Select(s => new ProductSizeRunSizeDto
                    {
                        ProductSizeRunSizeId = s.Id,
                        Sequence = s.Sequence,
                        SizeCode = s.SizeCode,
                        DisplayLabel = s.DisplayLabel,
                        BarcodeLabel = s.BarcodeLabel,
                        FactorLabel = s.FactorLabel,
                        Proportion = s.Proportion,
                        IsVisible = s.IsVisible,
                        IsActive = s.IsActive
                    }).ToList()
                }).FirstOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/", async (ProductSizeRunEnterpriseRequest request, NanchesoftDbContext db) =>
        {
            var ctx = await ResolveDefaultContextAsync(db);
            if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue)
                return Results.BadRequest(new { message = "No existe una empresa para ligar la corrida." });

            var code = NormalizeUpper(request.Code);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

            if (await db.ProductSizeRuns.AnyAsync(x => x.CompanyId == ctx.CompanyId && x.Code == code))
                return Results.BadRequest(new { message = "Ya existe una corrida con ese código." });

            var entity = new ProductSizeRun
            {
                TenantId = ctx.TenantId.Value,
                CompanyId = ctx.CompanyId.Value,
                CreatedBy = "web-api"
            };

            ApplyHeader(entity, request);
            db.ProductSizeRuns.Add(entity);
            await db.SaveChangesAsync();
            await ReplaceSizesAsync(db, entity, request.Sizes, "web-api");
            return Results.Ok(new { success = true, id = entity.Id });
        });

        group.MapPut("/{id:guid}", async (Guid id, ProductSizeRunEnterpriseRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.ProductSizeRuns.Include(x => x.Sizes).FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();

            var code = NormalizeUpper(request.Code);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

            if (await db.ProductSizeRuns.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
                return Results.BadRequest(new { message = "Ya existe otra corrida con ese código." });

            ApplyHeader(entity, request);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            await ReplaceSizesAsync(db, entity, request.Sizes, "web-api");
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.ProductSizeRuns.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return Results.NotFound();

            var isUsed = await db.FinishedProducts.AnyAsync(x => x.ProductSizeRunId == id)
                || await db.ItemEngineeringProfiles.AnyAsync(x => x.ProductSizeRunId == id)
                || await db.ProductVariants.AnyAsync(x => x.ProductSizeRunId == id);

            if (isUsed)
                return Results.BadRequest(new { message = "La corrida ya está ligada a productos, perfiles o variantes. Desactívala en lugar de borrarla." });

            db.ProductSizeRuns.Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        group.MapPost("/{id:guid}/generate-variants", async (Guid id, NanchesoftDbContext db) =>
        {
            var run = await db.ProductSizeRuns.Include(x => x.Sizes).FirstOrDefaultAsync(x => x.Id == id);
            if (run is null) return Results.NotFound(new { message = "Corrida no encontrada." });

            var products = await db.FinishedProducts
                .Where(x => x.ProductSizeRunId == id)
                .ToListAsync();

            var created = 0;
            foreach (var product in products)
            {
                foreach (var size in run.Sizes.OrderBy(x => x.Sequence).Where(x => x.IsActive))
                {
                    var exists = await db.ProductVariants.AnyAsync(x => x.FinishedProductId == product.Id && x.ProductSizeRunSizeId == size.Id);
                    if (exists) continue;

                    var suffix = string.IsNullOrWhiteSpace(size.BarcodeLabel) ? size.SizeCode : size.BarcodeLabel;
                    suffix = suffix.Replace(".", string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
                    var sku = $"{product.Code}-{suffix}";

                    db.ProductVariants.Add(new ProductVariant
                    {
                        TenantId = product.TenantId,
                        CompanyId = product.CompanyId,
                        FinishedProductId = product.Id,
                        ProductSizeRunId = run.Id,
                        ProductSizeRunSizeId = size.Id,
                        Sequence = size.Sequence,
                        SizeCode = size.SizeCode,
                        DisplayLabel = size.DisplayLabel,
                        Sku = sku,
                        Barcode = sku,
                        CreatedBy = "web-api"
                    });
                    created++;
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, created });
        });

        return app;
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return company is null ? (null, null) : (company.TenantId, company.Id);
    }

    private static void ApplyHeader(ProductSizeRun entity, ProductSizeRunEnterpriseRequest request)
    {
        entity.Code = NormalizeUpper(request.Code);
        entity.Name = NormalizeText(request.Name);
        entity.DisplayName = NormalizeText(request.DisplayName);
        entity.LegacyKey = NormalizeUpper(request.LegacyKey);
        entity.SecondaryKey = NormalizeUpper(request.SecondaryKey);
        entity.ConsumptionMode = string.IsNullOrWhiteSpace(request.ConsumptionMode) ? "I" : request.ConsumptionMode.Trim().Substring(0, 1).ToUpperInvariant();
        entity.IsUniqueSizeRun = request.IsUniqueSizeRun;
        entity.MiddlePoint = request.MiddlePoint;
        entity.IsActive = request.IsActive;
    }

    private static async Task ReplaceSizesAsync(NanchesoftDbContext db, ProductSizeRun run, List<ProductSizeRunSizeRequest> sizes, string user)
    {
        var existing = await db.ProductSizeRunSizes.Where(x => x.ProductSizeRunId == run.Id).ToListAsync();
        if (existing.Count > 0)
            db.ProductSizeRunSizes.RemoveRange(existing);

        var clean = sizes
            .Where(x => !string.IsNullOrWhiteSpace(x.SizeCode))
            .OrderBy(x => x.Sequence <= 0 ? int.MaxValue : x.Sequence)
            .ToList();

        if (clean.Count == 0)
            clean.Add(new ProductSizeRunSizeRequest { Sequence = 1, SizeCode = "U", DisplayLabel = "U", BarcodeLabel = "U", IsVisible = true, IsActive = true });

        var rows = clean.Select((x, index) => new ProductSizeRunSize
        {
            ProductSizeRunId = run.Id,
            Sequence = x.Sequence <= 0 ? index + 1 : x.Sequence,
            SizeCode = NormalizeText(x.SizeCode),
            DisplayLabel = string.IsNullOrWhiteSpace(x.DisplayLabel) ? NormalizeText(x.SizeCode) : NormalizeText(x.DisplayLabel),
            BarcodeLabel = string.IsNullOrWhiteSpace(x.BarcodeLabel) ? NormalizeText(x.SizeCode) : NormalizeText(x.BarcodeLabel),
            FactorLabel = NormalizeText(x.FactorLabel),
            Proportion = x.Proportion,
            IsVisible = x.IsVisible,
            IsActive = x.IsActive,
            CreatedBy = user
        }).ToList();

        db.ProductSizeRunSizes.AddRange(rows);
        run.SizeCount = rows.Count;
        run.UpdatedAt = DateTime.UtcNow;
        run.UpdatedBy = user;
        await db.SaveChangesAsync();
    }

    private static string NormalizeUpper(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    private static string NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}

public class ProductSizeRunEnterpriseRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LegacyKey { get; set; } = string.Empty;
    public string SecondaryKey { get; set; } = string.Empty;
    public string ConsumptionMode { get; set; } = "I";
    public bool IsUniqueSizeRun { get; set; }
    public int? MiddlePoint { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductSizeRunSizeRequest> Sizes { get; set; } = new();
}

public class ProductSizeRunSizeRequest
{
    public int Sequence { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string BarcodeLabel { get; set; } = string.Empty;
    public string FactorLabel { get; set; } = string.Empty;
    public decimal Proportion { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class ProductSizeRunEnterpriseDto : ProductSizeRunEnterpriseRequest
{
    public Guid ProductSizeRunId { get; set; }
    public Guid CompanyId { get; set; }
    public int SizeCount { get; set; }
    public string SizesPreview { get; set; } = string.Empty;
    public List<ProductSizeRunSizeDto> Sizes { get; set; } = new();
}

public sealed class ProductSizeRunSizeDto : ProductSizeRunSizeRequest
{
    public Guid ProductSizeRunSizeId { get; set; }
}
