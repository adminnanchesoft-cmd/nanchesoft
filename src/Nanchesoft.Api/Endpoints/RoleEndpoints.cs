using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security/roles").WithTags("Roles");

        group.MapGet("/", GetRolesAsync);
        group.MapGet("/tenants", GetTenantsAsync);
        group.MapPost("/", CreateRoleAsync);
        group.MapPut("/{id:guid}", UpdateRoleAsync);
        group.MapDelete("/{id:guid}", DeleteRoleAsync);

        return app;
    }

    private static async Task<IResult> GetRolesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Roles.AsNoTracking().Include(x => x.Tenant).AsQueryable();
        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var roles = await query.OrderBy(x => x.Name).Select(x => new RoleListItemDto
        {
            RoleId = x.Id,
            TenantId = x.TenantId,
            TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description ?? string.Empty,
            IsSystemRole = x.IsSystemRole,
            IsActive = x.IsActive
        }).ToListAsync();

        return Results.Ok(roles);
    }

    private static async Task<IResult> GetTenantsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Tenants.AsNoTracking().Where(x => x.IsActive).AsQueryable();
        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.Id == tenantId.Value);

        var tenants = await query.OrderBy(x => x.Name).Select(x => new RoleTenantLookupDto
        {
            TenantId = x.Id,
            TenantName = x.Name
        }).ToListAsync();

        return Results.Ok(tenants);
    }

    private static async Task<IResult> CreateRoleAsync(HttpContext httpContext, CreateOrUpdateRoleRequest request, NanchesoftDbContext db)
    {
        var tenantScopeId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantScopeId.HasValue)
            request.TenantId = tenantScopeId;

        if (!request.TenantId.HasValue || request.TenantId.Value == Guid.Empty)
            return Results.BadRequest(new { message = "El tenant es obligatorio." });

        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == request.TenantId.Value);
        if (tenant is null)
            return Results.BadRequest(new { message = "No se encontró el tenant enviado." });

        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        var name = (request.Name ?? string.Empty).Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre del rol son obligatorios." });

        var exists = await db.Roles.AnyAsync(x => x.TenantId == tenant.Id && x.Code == code);
        if (exists)
            return Results.BadRequest(new { message = "Ya existe un rol con ese código dentro del tenant." });

        var role = new Role
        {
            TenantId = tenant.Id,
            Code = code,
            Name = name,
            Description = description,
            IsSystemRole = request.IsSystemRole,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        return Results.Ok(new RoleListItemDto
        {
            RoleId = role.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description ?? string.Empty,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive
        });
    }

    private static async Task<IResult> UpdateRoleAsync(HttpContext httpContext, Guid id, CreateOrUpdateRoleRequest request, NanchesoftDbContext db)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id);
        if (role is null)
            return Results.NotFound(new { message = "No se encontró el rol." });

        var tenantScopeId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantScopeId.HasValue)
        {
            if (role.TenantId != tenantScopeId.Value)
                return Results.Forbid();
            request.TenantId = tenantScopeId;
        }

        var tenantId = request.TenantId.HasValue && request.TenantId.Value != Guid.Empty ? request.TenantId.Value : role.TenantId;
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant is null)
            return Results.BadRequest(new { message = "No se encontró el tenant del rol." });

        var code = string.IsNullOrWhiteSpace(request.Code) ? role.Code : request.Code.Trim().ToUpperInvariant();
        var name = string.IsNullOrWhiteSpace(request.Name) ? role.Name : request.Name.Trim();
        var description = request.Description is null ? role.Description : (string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim());
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre del rol son obligatorios." });

        var duplicate = await db.Roles.AnyAsync(x => x.Id != id && x.TenantId == tenant.Id && x.Code == code);
        if (duplicate)
            return Results.BadRequest(new { message = "Ya existe otro rol con ese código dentro del tenant." });

        role.TenantId = tenant.Id;
        role.Code = code;
        role.Name = name;
        role.Description = description;
        role.IsSystemRole = request.IsSystemRole;
        role.IsActive = request.IsActive;
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = "web-api";
        await db.SaveChangesAsync();

        return Results.Ok(new RoleListItemDto
        {
            RoleId = role.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description ?? string.Empty,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive
        });
    }

    private static async Task<IResult> DeleteRoleAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var role = await db.Roles.Include(x => x.UserRoles).Include(x => x.RolePermissions).FirstOrDefaultAsync(x => x.Id == id);
        if (role is null)
            return Results.NotFound(new { message = "No se encontró el rol." });

        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue && role.TenantId != tenantId.Value)
            return Results.Forbid();

        if (role.IsSystemRole)
            return Results.BadRequest(new { message = "No puedes eliminar un rol de sistema." });
        if (role.UserRoles.Any())
            return Results.BadRequest(new { message = "No puedes eliminar un rol que ya está asignado a usuarios." });

        db.RolePermissions.RemoveRange(role.RolePermissions);
        db.Roles.Remove(role);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class RoleListItemDto
{
    public Guid RoleId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
}

public sealed class RoleTenantLookupDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class CreateOrUpdateRoleRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;
}
