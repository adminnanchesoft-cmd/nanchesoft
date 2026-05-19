using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization/warehouses").WithTags("Warehouses");

        group.MapGet("/", GetWarehousesAsync);
        group.MapGet("/branches", GetBranchesAsync);
        group.MapPost("/", CreateWarehouseAsync);
        group.MapPut("/{id:guid}", UpdateWarehouseAsync);
        group.MapDelete("/{id:guid}", DeleteWarehouseAsync);

        return app;
    }

    private static async Task<IResult> GetWarehousesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Warehouses
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Branch)
            .AsQueryable();

        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);
        if (!isPlatformOwner && companyId.HasValue)
            query = query.Where(x => x.Branch != null && x.Branch.CompanyId == companyId.Value);

        var warehouses = await query.OrderBy(x => x.Name).Select(x => new WarehouseListItemDto
        {
            WarehouseId = x.Id,
            TenantId = x.TenantId,
            TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
            BranchId = x.BranchId,
            BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
            CompanyId = x.Branch != null ? x.Branch.CompanyId : Guid.Empty,
            Code = x.Code,
            Name = x.Name,
            IsActive = x.IsActive
        }).ToListAsync();

        return Results.Ok(warehouses);
    }

    private static async Task<IResult> GetBranchesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Branches
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);
        if (!isPlatformOwner && companyId.HasValue)
            query = query.Where(x => x.CompanyId == companyId.Value);

        var branches = await query.OrderBy(x => x.Name).Select(x => new WarehouseBranchLookupDto
        {
            BranchId = x.Id,
            BranchName = x.Name,
            TenantId = x.TenantId,
            TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
            CompanyId = x.CompanyId
        }).ToListAsync();

        return Results.Ok(branches);
    }

    private static async Task<IResult> CreateWarehouseAsync(HttpContext httpContext, CreateOrUpdateWarehouseRequest request, NanchesoftDbContext db)
    {
        if (!request.BranchId.HasValue || request.BranchId.Value == Guid.Empty)
            return Results.BadRequest(new { message = "La sucursal es obligatoria." });

        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.BranchId.Value);
        if (branch is null)
            return Results.BadRequest(new { message = "No se encontró la sucursal enviada." });

        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext))
        {
            if (tenantId.HasValue && branch.TenantId != tenantId.Value)
                return Results.StatusCode(403);
            if (companyId.HasValue && branch.CompanyId != companyId.Value)
                return Results.StatusCode(403);
        }

        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y almacén son obligatorios." });

        var exists = await db.Warehouses.AnyAsync(x => x.BranchId == branch.Id && x.Code == code);
        if (exists)
            return Results.BadRequest(new { message = "Ya existe un almacén con ese código dentro de la sucursal." });

        var warehouse = new Warehouse
        {
            TenantId = branch.TenantId,
            BranchId = branch.Id,
            Code = code,
            Name = name,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var tenantName = await db.Tenants.Where(x => x.Id == branch.TenantId).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty;
        return Results.Ok(new WarehouseListItemDto
        {
            WarehouseId = warehouse.Id,
            TenantId = warehouse.TenantId,
            TenantName = tenantName,
            BranchId = warehouse.BranchId,
            BranchName = branch.Name,
            CompanyId = branch.CompanyId,
            Code = warehouse.Code,
            Name = warehouse.Name,
            IsActive = warehouse.IsActive
        });
    }

    private static async Task<IResult> UpdateWarehouseAsync(HttpContext httpContext, Guid id, CreateOrUpdateWarehouseRequest request, NanchesoftDbContext db)
    {
        var warehouse = await db.Warehouses.Include(x => x.Tenant).Include(x => x.Branch).FirstOrDefaultAsync(x => x.Id == id);
        if (warehouse is null)
            return Results.NotFound(new { message = "No se encontró el almacén." });

        var tenantScopeId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyScopeId = ApiTenantScope.ResolveCompanyId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantScopeId.HasValue && warehouse.TenantId != tenantScopeId.Value)
            return Results.StatusCode(403);

        Branch? branch = null;
        if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
            branch = await db.Branches.FirstOrDefaultAsync(x => x.Id == request.BranchId.Value);
        branch ??= await db.Branches.FirstOrDefaultAsync(x => x.Id == warehouse.BranchId);
        if (branch is null)
            return Results.BadRequest(new { message = "No se encontró la sucursal del almacén." });
        if (!ApiTenantScope.IsPlatformOwner(httpContext))
        {
            if (tenantScopeId.HasValue && branch.TenantId != tenantScopeId.Value)
                return Results.StatusCode(403);
            if (companyScopeId.HasValue && branch.CompanyId != companyScopeId.Value)
                return Results.StatusCode(403);
        }

        var code = string.IsNullOrWhiteSpace(request.Code) ? warehouse.Code : request.Code.Trim().ToUpperInvariant();
        var name = string.IsNullOrWhiteSpace(request.Name) ? warehouse.Name : request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y almacén son obligatorios." });

        var duplicate = await db.Warehouses.AnyAsync(x => x.Id != id && x.BranchId == branch.Id && x.Code == code);
        if (duplicate)
            return Results.BadRequest(new { message = "Ya existe otro almacén con ese código dentro de la sucursal." });

        warehouse.TenantId = branch.TenantId;
        warehouse.BranchId = branch.Id;
        warehouse.Code = code;
        warehouse.Name = name;
        warehouse.IsActive = request.IsActive;
        warehouse.UpdatedAt = DateTime.UtcNow;
        warehouse.UpdatedBy = "web-api";
        await db.SaveChangesAsync();

        var tenantName = await db.Tenants.Where(x => x.Id == branch.TenantId).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty;
        return Results.Ok(new WarehouseListItemDto
        {
            WarehouseId = warehouse.Id,
            TenantId = warehouse.TenantId,
            TenantName = tenantName,
            BranchId = warehouse.BranchId,
            BranchName = branch.Name,
            CompanyId = branch.CompanyId,
            Code = warehouse.Code,
            Name = warehouse.Name,
            IsActive = warehouse.IsActive
        });
    }

    private static async Task<IResult> DeleteWarehouseAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var warehouse = await db.Warehouses.Include(x => x.Branch).FirstOrDefaultAsync(x => x.Id == id);
        if (warehouse is null)
            return Results.NotFound(new { message = "No se encontró el almacén." });

        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext))
        {
            if (tenantId.HasValue && warehouse.TenantId != tenantId.Value)
                return Results.StatusCode(403);
            if (companyId.HasValue && warehouse.Branch is not null && warehouse.Branch.CompanyId != companyId.Value)
                return Results.StatusCode(403);
        }

        db.Warehouses.Remove(warehouse);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class WarehouseListItemDto
{
    public Guid WarehouseId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class WarehouseBranchLookupDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
}

public sealed class CreateOrUpdateWarehouseRequest
{
    public Guid? BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
