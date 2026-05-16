using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Common;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductOrangeCatalogEndpoints
{
    public static void MapProductOrangeCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        MapSimpleCatalog<ProductLeatherType>(app, "/api/products/leather-types", "Product Orange Catalogs");
        MapSimpleCatalog<ProductSole>(app, "/api/products/soles", "Product Orange Catalogs");
        MapSimpleCatalog<ProductColor>(app, "/api/products/colors", "Product Orange Catalogs");
        MapSimpleCatalog<ProductManufacturingType>(app, "/api/products/manufacturing-types", "Product Orange Catalogs");
        MapSimpleCatalog<ProductToeCap>(app, "/api/products/toe-caps", "Product Orange Catalogs");
        MapSimpleCatalog<ProductSoleColor>(app, "/api/products/sole-colors", "Product Orange Catalogs");
        MapSimpleCatalog<ProductDie>(app, "/api/products/dies", "Product Orange Catalogs");
        MapSimpleCatalog<QualityControlDie>(app, "/api/products/quality-control-dies", "Product Orange Catalogs");
        MapSimpleCatalog<ProductFolioPattern>(app, "/api/products/folio-patterns", "Product Orange Catalogs");

        // /options endpoints for orange catalog types (used by finished-product form)
        app.MapGet("/api/products/leather-types/options", async (NanchesoftDbContext db) => Results.Ok(
            await db.ProductLeatherTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new { id = x.Id.ToString(), code = x.Code, name = x.Name }).ToListAsync())).WithTags("Product Orange Catalogs");
        app.MapGet("/api/products/colors/options", async (NanchesoftDbContext db) => Results.Ok(
            await db.ProductColors.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new { id = x.Id.ToString(), code = x.Code, name = x.Name }).ToListAsync())).WithTags("Product Orange Catalogs");
        app.MapGet("/api/products/toe-caps/options", async (NanchesoftDbContext db) => Results.Ok(
            await db.ProductToeCaps.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new { id = x.Id.ToString(), code = x.Code, name = x.Name }).ToListAsync())).WithTags("Product Orange Catalogs");
        app.MapGet("/api/products/soles/options", async (NanchesoftDbContext db) => Results.Ok(
            await db.ProductSoles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new { id = x.Id.ToString(), code = x.Code, name = x.Name }).ToListAsync())).WithTags("Product Orange Catalogs");
        app.MapGet("/api/products/sole-colors/options", async (NanchesoftDbContext db) => Results.Ok(
            await db.ProductSoleColors.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new { id = x.Id.ToString(), code = x.Code, name = x.Name }).ToListAsync())).WithTags("Product Orange Catalogs");
        app.MapGet("/api/products/folio-patterns/options", async (NanchesoftDbContext db) => Results.Ok(
            await db.ProductFolioPatterns.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new { id = x.Id.ToString(), code = x.Code, name = x.Name }).ToListAsync())).WithTags("Product Orange Catalogs");
        app.MapGet("/api/products/manufacturing-types/options", async (NanchesoftDbContext db) => Results.Ok(
            await db.ProductManufacturingTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Sequence).ThenBy(x => x.Code)
                .Select(x => new { id = x.Id.ToString(), code = x.Code, name = x.Name }).ToListAsync())).WithTags("Product Orange Catalogs");
    }

    private static void MapSimpleCatalog<TEntity>(IEndpointRouteBuilder app, string route, string tag)
        where TEntity : BaseEntity, IOrangeSimpleCatalogEntity, new()
    {
        var g = app.MapGroup(route).WithTags(tag);

        g.MapGet("/", async (NanchesoftDbContext db) => Results.Ok(await db.Set<TEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Sequence)
            .ThenBy(x => x.Code)
            .Select(x => new OrangeSimpleCatalogDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                Sequence = x.Sequence,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync()));

        g.MapGet("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var row = await db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return row is null ? Results.NotFound() : Results.Ok(row);
        });

        g.MapPost("/", async (OrangeSimpleCatalogRequest request, NanchesoftDbContext db) =>
        {
            var (tenantId, companyId) = await ResolveDefaultContextAsync(db);
            if (tenantId is null || companyId is null)
                return Results.BadRequest("No existe tenant/empresa para asignar el catálogo.");

            var code = NormalizeCode(request.Code, request.Name);
            var name = NormalizeName(request.Name, code);

            var duplicate = await db.Set<TEntity>().AnyAsync(x => x.CompanyId == companyId.Value && x.Code == code && x.Id != request.Id);
            if (duplicate)
                return Results.BadRequest($"Ya existe un registro con la clave '{code}'.");

            var entity = new TEntity
            {
                Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
                TenantId = tenantId.Value,
                CompanyId = companyId.Value,
                Code = code,
                Name = name,
                Description = (request.Description ?? string.Empty).Trim(),
                Sequence = request.Sequence,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = string.Empty
            };

            db.Set<TEntity>().Add(entity);
            await db.SaveChangesAsync();
            return Results.Ok(entity.Id);
        });

        g.MapPut("/{id:guid}", async (Guid id, OrangeSimpleCatalogRequest request, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound();

            // En edición: si Code viene vacío se conserva el code original; nunca se pisa con Name.
            var code = string.IsNullOrWhiteSpace(request.Code) ? entity.Code : NormalizeCode(request.Code, null);
            var name = NormalizeName(request.Name, entity.Name);
            var duplicate = await db.Set<TEntity>().AnyAsync(x => x.CompanyId == entity.CompanyId && x.Code == code && x.Id != id);
            if (duplicate)
                return Results.BadRequest($"Ya existe un registro con la clave '{code}'.");

            entity.Code = code;
            entity.Name = name;
            entity.Description = (request.Description ?? string.Empty).Trim();
            entity.Sequence = request.Sequence;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "system";
            await db.SaveChangesAsync();
            return Results.Ok(entity.Id);
        });

        g.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound();
            db.Set<TEntity>().Remove(entity);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.AsNoTracking().OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return company is null ? (null, null) : (company.TenantId, company.Id);
    }

    private static string NormalizeCode(string? code, string? name)
    {
        var value = string.IsNullOrWhiteSpace(code) ? name : code;
        value = (value ?? string.Empty).Trim().ToUpperInvariant();
        return value.Length > 40 ? value[..40] : value;
    }

    private static string NormalizeName(string? name, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
        return value.Length > 160 ? value[..160] : value;
    }
}

public class OrangeSimpleCatalogRequest
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class OrangeSimpleCatalogDto : OrangeSimpleCatalogRequest
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
