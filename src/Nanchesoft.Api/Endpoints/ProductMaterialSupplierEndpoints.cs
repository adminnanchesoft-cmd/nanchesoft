using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ProductMaterialSupplierEndpoints
{
    public static void MapProductMaterialSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        MapAssignments(app);
        MapCostHistory(app);
        MapLookups(app);
    }

    private static void MapAssignments(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/material-suppliers").WithTags("ProductMaterialSuppliers");

        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.MaterialSupplierAssignments
                .AsNoTracking()
                .Include(x => x.MaterialItem)
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseUnit)
                .Include(x => x.Currency)
                .OrderBy(x => x.MaterialItem!.Code)
                .ThenByDescending(x => x.IsPreferred)
                .ThenBy(x => x.Supplier!.Name)
                .Select(x => new MaterialSupplierAssignmentDto
                {
                    MaterialSupplierAssignmentId = x.Id,
                    CompanyId = x.CompanyId,
                    MaterialItemId = x.MaterialItemId,
                    MaterialItemCode = x.MaterialItem != null ? x.MaterialItem.Code : string.Empty,
                    MaterialItemName = x.MaterialItem != null ? x.MaterialItem.Name : string.Empty,
                    SupplierId = x.SupplierId,
                    SupplierCode = x.Supplier != null ? x.Supplier.Code : string.Empty,
                    SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                    PurchaseUnitId = x.PurchaseUnitId,
                    PurchaseUnitName = x.PurchaseUnit != null ? x.PurchaseUnit.Name : string.Empty,
                    CurrencyId = x.CurrencyId,
                    CurrencyCode = x.Currency != null ? x.Currency.Code : string.Empty,
                    SupplierItemCode = x.SupplierItemCode,
                    SupplierItemName = x.SupplierItemName,
                    ConversionFactor = x.ConversionFactor,
                    AuthorizedCost = x.AuthorizedCost,
                    LastCost = x.LastCost,
                    LeadTimeDays = x.LeadTimeDays,
                    MinimumOrderQuantity = x.MinimumOrderQuantity,
                    IsPreferred = x.IsPreferred,
                    ValidFrom = x.ValidFrom,
                    ValidTo = x.ValidTo,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return Results.Ok(rows);
        });

        group.MapPost("/", async (MaterialSupplierAssignmentRequest request, NanchesoftDbContext db)
            => await UpsertAssignmentAsync(null, request, db));

        group.MapPut("/{id:guid}", async (Guid id, MaterialSupplierAssignmentRequest request, NanchesoftDbContext db)
            => await UpsertAssignmentAsync(id, request, db));

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.MaterialSupplierAssignments.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound();

            var materialId = entity.MaterialItemId;
            db.MaterialSupplierAssignments.Remove(entity);
            await db.SaveChangesAsync();
            await SyncMaterialSummaryAsync(db, materialId);
            return Results.Ok(new { success = true });
        });
    }

    private static void MapCostHistory(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/material-supplier-cost-history").WithTags("ProductMaterialSuppliers");

        group.MapGet("/", async (NanchesoftDbContext db) =>
        {
            var rows = await db.MaterialSupplierCostHistory
                .AsNoTracking()
                .Include(x => x.MaterialSupplierAssignment)
                    .ThenInclude(x => x!.MaterialItem)
                .Include(x => x.MaterialSupplierAssignment)
                    .ThenInclude(x => x!.Supplier)
                .Include(x => x.Currency)
                .OrderByDescending(x => x.CostDate)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new MaterialSupplierCostHistoryDto
                {
                    MaterialSupplierCostHistoryId = x.Id,
                    CompanyId = x.CompanyId,
                    MaterialSupplierAssignmentId = x.MaterialSupplierAssignmentId,
                    MaterialItemId = x.MaterialSupplierAssignment != null ? x.MaterialSupplierAssignment.MaterialItemId : Guid.Empty,
                    MaterialItemCode = x.MaterialSupplierAssignment != null && x.MaterialSupplierAssignment.MaterialItem != null ? x.MaterialSupplierAssignment.MaterialItem.Code : string.Empty,
                    MaterialItemName = x.MaterialSupplierAssignment != null && x.MaterialSupplierAssignment.MaterialItem != null ? x.MaterialSupplierAssignment.MaterialItem.Name : string.Empty,
                    SupplierId = x.MaterialSupplierAssignment != null ? x.MaterialSupplierAssignment.SupplierId : Guid.Empty,
                    SupplierCode = x.MaterialSupplierAssignment != null && x.MaterialSupplierAssignment.Supplier != null ? x.MaterialSupplierAssignment.Supplier.Code : string.Empty,
                    SupplierName = x.MaterialSupplierAssignment != null && x.MaterialSupplierAssignment.Supplier != null ? x.MaterialSupplierAssignment.Supplier.Name : string.Empty,
                    CurrencyId = x.CurrencyId,
                    CurrencyCode = x.Currency != null ? x.Currency.Code : string.Empty,
                    CostDate = x.CostDate,
                    Cost = x.Cost,
                    ExchangeRate = x.ExchangeRate,
                    SourceDocumentType = x.SourceDocumentType,
                    SourceDocumentId = x.SourceDocumentId,
                    SourceDocumentNumber = x.SourceDocumentNumber,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return Results.Ok(rows);
        });

        group.MapPost("/", async (MaterialSupplierCostHistoryRequest request, NanchesoftDbContext db)
            => await UpsertHistoryAsync(null, request, db));

        group.MapPut("/{id:guid}", async (Guid id, MaterialSupplierCostHistoryRequest request, NanchesoftDbContext db)
            => await UpsertHistoryAsync(id, request, db));

        group.MapDelete("/{id:guid}", async (Guid id, NanchesoftDbContext db) =>
        {
            var entity = await db.MaterialSupplierCostHistory.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
                return Results.NotFound();

            var assignmentId = entity.MaterialSupplierAssignmentId;
            db.MaterialSupplierCostHistory.Remove(entity);
            await db.SaveChangesAsync();
            await RecalculateLastCostAsync(db, assignmentId);
            return Results.Ok(new { success = true });
        });
    }

    private static void MapLookups(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/material-suppliers/options", async (NanchesoftDbContext db) =>
            Results.Ok(await db.MaterialSupplierAssignments
                .AsNoTracking()
                .Include(x => x.MaterialItem)
                .Include(x => x.Supplier)
                .OrderBy(x => x.MaterialItem!.Code)
                .ThenBy(x => x.Supplier!.Code)
                .Select(x => new MaterialSupplierAssignmentOptionDto
                {
                    MaterialSupplierAssignmentId = x.Id,
                    MaterialItemId = x.MaterialItemId,
                    SupplierId = x.SupplierId,
                    Code = (x.MaterialItem != null ? x.MaterialItem.Code : string.Empty) + " / " + (x.Supplier != null ? x.Supplier.Code : string.Empty),
                    Name = (x.MaterialItem != null ? x.MaterialItem.Name : string.Empty) + " - " + (x.Supplier != null ? x.Supplier.Name : string.Empty)
                })
                .ToListAsync()))
            .WithTags("ProductMaterialSuppliers");
    }

    private static async Task<IResult> UpsertAssignmentAsync(Guid? id, MaterialSupplierAssignmentRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue)
            return Results.BadRequest(new { message = "No se encontró el contexto base de tenant/empresa." });

        if (request.MaterialItemId == Guid.Empty || request.SupplierId == Guid.Empty)
            return Results.BadRequest(new { message = "MaterialItemId y SupplierId son obligatorios." });

        var exists = await db.MaterialItems.AnyAsync(x => x.Id == request.MaterialItemId && x.CompanyId == ctx.CompanyId.Value);
        if (!exists)
            return Results.BadRequest(new { message = "El material no existe en la empresa activa." });

        var duplicate = await db.MaterialSupplierAssignments.AnyAsync(x => x.CompanyId == ctx.CompanyId.Value && x.MaterialItemId == request.MaterialItemId && x.SupplierId == request.SupplierId && (!id.HasValue || x.Id != id.Value));
        if (duplicate)
            return Results.BadRequest(new { message = "Ya existe esa relación material-proveedor." });

        var entity = id.HasValue ? await db.MaterialSupplierAssignments.FirstOrDefaultAsync(x => x.Id == id.Value) : null;
        if (id.HasValue && entity is null)
            return Results.NotFound();

        entity ??= new MaterialSupplierAssignment
        {
            TenantId = ctx.TenantId.Value,
            CompanyId = ctx.CompanyId.Value,
            CreatedBy = "web-api"
        };

        entity.MaterialItemId = request.MaterialItemId;
        entity.SupplierId = request.SupplierId;
        entity.PurchaseUnitId = request.PurchaseUnitId;
        entity.CurrencyId = request.CurrencyId;
        entity.SupplierItemCode = N(request.SupplierItemCode, true);
        entity.SupplierItemName = N(request.SupplierItemName);
        entity.ConversionFactor = request.ConversionFactor <= 0 ? 1m : request.ConversionFactor;
        entity.AuthorizedCost = request.AuthorizedCost;
        entity.LastCost = request.LastCost;
        entity.LeadTimeDays = Math.Max(0, request.LeadTimeDays);
        entity.MinimumOrderQuantity = request.MinimumOrderQuantity < 0 ? 0 : request.MinimumOrderQuantity;
        entity.IsPreferred = request.IsPreferred;
        entity.ValidFrom = request.ValidFrom?.Date;
        entity.ValidTo = request.ValidTo?.Date;
        entity.Notes = N(request.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        if (!id.HasValue)
            db.MaterialSupplierAssignments.Add(entity);

        await db.SaveChangesAsync();

        if (entity.IsPreferred)
        {
            await db.MaterialSupplierAssignments
                .Where(x => x.CompanyId == entity.CompanyId && x.MaterialItemId == entity.MaterialItemId && x.Id != entity.Id && x.IsPreferred)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsPreferred, false).SetProperty(x => x.UpdatedAt, DateTime.UtcNow).SetProperty(x => x.UpdatedBy, "web-api"));
        }

        await SyncMaterialSummaryAsync(db, entity.MaterialItemId);
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpsertHistoryAsync(Guid? id, MaterialSupplierCostHistoryRequest request, NanchesoftDbContext db)
    {
        var ctx = await ResolveDefaultContextAsync(db);
        if (!ctx.TenantId.HasValue || !ctx.CompanyId.HasValue)
            return Results.BadRequest(new { message = "No se encontró el contexto base de tenant/empresa." });

        if (request.MaterialSupplierAssignmentId == Guid.Empty)
            return Results.BadRequest(new { message = "MaterialSupplierAssignmentId es obligatorio." });

        var assignment = await db.MaterialSupplierAssignments.FirstOrDefaultAsync(x => x.Id == request.MaterialSupplierAssignmentId && x.CompanyId == ctx.CompanyId.Value);
        if (assignment is null)
            return Results.BadRequest(new { message = "La relación material-proveedor no existe." });

        var entity = id.HasValue ? await db.MaterialSupplierCostHistory.FirstOrDefaultAsync(x => x.Id == id.Value) : null;
        if (id.HasValue && entity is null)
            return Results.NotFound();

        entity ??= new MaterialSupplierCostHistory
        {
            TenantId = ctx.TenantId.Value,
            CompanyId = ctx.CompanyId.Value,
            CreatedBy = "web-api"
        };

        entity.MaterialSupplierAssignmentId = request.MaterialSupplierAssignmentId;
        entity.CurrencyId = request.CurrencyId ?? assignment.CurrencyId;
        entity.CostDate = request.CostDate == default ? DateTime.UtcNow.Date : request.CostDate.Date;
        entity.Cost = request.Cost;
        entity.ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate;
        entity.SourceDocumentType = N(request.SourceDocumentType, true);
        entity.SourceDocumentId = request.SourceDocumentId;
        entity.SourceDocumentNumber = N(request.SourceDocumentNumber, true);
        entity.Notes = N(request.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        if (!id.HasValue)
            db.MaterialSupplierCostHistory.Add(entity);

        await db.SaveChangesAsync();
        await RecalculateLastCostAsync(db, assignment.Id);
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task RecalculateLastCostAsync(NanchesoftDbContext db, Guid assignmentId)
    {
        var assignment = await db.MaterialSupplierAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId);
        if (assignment is null)
            return;

        var lastCost = await db.MaterialSupplierCostHistory
            .Where(x => x.MaterialSupplierAssignmentId == assignmentId && x.IsActive)
            .OrderByDescending(x => x.CostDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => (decimal?)x.Cost)
            .FirstOrDefaultAsync();

        if (lastCost.HasValue)
        {
            assignment.LastCost = lastCost.Value;
            assignment.UpdatedAt = DateTime.UtcNow;
            assignment.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
        }

        await SyncMaterialSummaryAsync(db, assignment.MaterialItemId);
    }

    private static async Task SyncMaterialSummaryAsync(NanchesoftDbContext db, Guid materialItemId)
    {
        var material = await db.MaterialItems.FirstOrDefaultAsync(x => x.Id == materialItemId);
        if (material is null)
            return;

        var preferred = await db.MaterialSupplierAssignments
            .Where(x => x.MaterialItemId == materialItemId && x.IsActive)
            .OrderByDescending(x => x.IsPreferred)
            .ThenByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (preferred is null)
        {
            material.SupplierId = null;
            material.UpdatedAt = DateTime.UtcNow;
            material.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return;
        }

        material.SupplierId = preferred.SupplierId;
        material.PurchaseUnitId = preferred.PurchaseUnitId ?? material.PurchaseUnitId;
        material.AuthorizedCost = preferred.AuthorizedCost;
        material.LastPurchaseCost = preferred.LastCost;
        material.StandardCost = preferred.LastCost > 0 ? preferred.LastCost : preferred.AuthorizedCost;
        material.CostStatus = preferred.AuthorizedCost > 0 ? MaterialItem.AuthorizedCostStatus : material.CostStatus;
        material.UpdatedAt = DateTime.UtcNow;
        material.UpdatedBy = "web-api";
        await db.SaveChangesAsync();
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return company is null ? (null, null) : (company.TenantId, company.Id);
    }

    private static string N(string? value, bool upper = false) => string.IsNullOrWhiteSpace(value) ? string.Empty : (upper ? value.Trim().ToUpperInvariant() : value.Trim());
}

public class MaterialSupplierAssignmentRequest
{
    public Guid MaterialItemId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string SupplierItemCode { get; set; } = string.Empty;
    public string SupplierItemName { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1m;
    public decimal AuthorizedCost { get; set; }
    public decimal LastCost { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public bool IsPreferred { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class MaterialSupplierAssignmentDto : MaterialSupplierAssignmentRequest
{
    public Guid MaterialSupplierAssignmentId { get; set; }
    public Guid CompanyId { get; set; }
    public string MaterialItemCode { get; set; } = string.Empty;
    public string MaterialItemName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string PurchaseUnitName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
}

public sealed class MaterialSupplierAssignmentOptionDto
{
    public Guid MaterialSupplierAssignmentId { get; set; }
    public Guid MaterialItemId { get; set; }
    public Guid SupplierId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class MaterialSupplierCostHistoryRequest
{
    public Guid MaterialSupplierAssignmentId { get; set; }
    public Guid? CurrencyId { get; set; }
    public DateTime CostDate { get; set; } = DateTime.UtcNow.Date;
    public decimal Cost { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public string SourceDocumentType { get; set; } = string.Empty;
    public Guid? SourceDocumentId { get; set; }
    public string SourceDocumentNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class MaterialSupplierCostHistoryDto : MaterialSupplierCostHistoryRequest
{
    public Guid MaterialSupplierCostHistoryId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid MaterialItemId { get; set; }
    public string MaterialItemCode { get; set; } = string.Empty;
    public string MaterialItemName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
}
