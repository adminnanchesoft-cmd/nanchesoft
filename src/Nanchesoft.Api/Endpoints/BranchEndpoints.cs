using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class BranchEndpoints
{
    public static IEndpointRouteBuilder MapBranchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization/branches").WithTags("Branches");

        group.MapGet("/", GetBranchesAsync);
        group.MapGet("/companies", GetCompaniesAsync);
        group.MapPost("/", CreateBranchAsync);
        group.MapPut("/{id:guid}", UpdateBranchAsync);
        group.MapDelete("/{id:guid}", DeleteBranchAsync);

        return app;
    }

    private static async Task<IResult> GetBranchesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Branches
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Company)
            .AsQueryable();

        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);
        if (!isPlatformOwner && companyId.HasValue)
            query = query.Where(x => x.CompanyId == companyId.Value);

        var branches = await query.OrderBy(x => x.Name).Select(x => new BranchListItemDto
        {
            BranchId = x.Id,
            TenantId = x.TenantId,
            TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
            CompanyId = x.CompanyId,
            CompanyName = x.Company != null ? x.Company.Name : string.Empty,
            Code = x.Code,
            Name = x.Name,
            Address = x.Address ?? string.Empty,
            Phone = x.Phone ?? string.Empty,
            Email = x.Email ?? string.Empty,
            IsActive = x.IsActive
        }).ToListAsync();

        return Results.Ok(branches);
    }

    private static async Task<IResult> GetCompaniesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Companies
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);
        if (!isPlatformOwner && companyId.HasValue)
            query = query.Where(x => x.Id == companyId.Value);

        var companies = await query.OrderBy(x => x.Name).Select(x => new BranchCompanyLookupDto
        {
            CompanyId = x.Id,
            CompanyName = x.Name,
            TenantId = x.TenantId,
            TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty
        }).ToListAsync();

        return Results.Ok(companies);
    }

    private static async Task<IResult> CreateBranchAsync(HttpContext httpContext, CreateOrUpdateBranchRequest request, NanchesoftDbContext db)
    {
        if (!request.CompanyId.HasValue || request.CompanyId.Value == Guid.Empty)
            return Results.BadRequest(new { message = "La empresa es obligatoria." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId.Value);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa enviada." });

        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue && company.TenantId != tenantId.Value)
            return Results.StatusCode(403);

        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        var name = (request.Name ?? string.Empty).Trim();
        var address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y sucursal son obligatorios." });

        var exists = await db.Branches.AnyAsync(x => x.CompanyId == company.Id && x.Code == code);
        if (exists)
            return Results.BadRequest(new { message = "Ya existe una sucursal con ese código dentro de la empresa." });

        var branch = new Branch
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            Code = code,
            Name = name,
            Address = address,
            Phone = phone,
            Email = email,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var tenantName = await db.Tenants.Where(x => x.Id == company.TenantId).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty;
        return Results.Ok(new BranchListItemDto
        {
            BranchId = branch.Id,
            TenantId = branch.TenantId,
            TenantName = tenantName,
            CompanyId = branch.CompanyId,
            CompanyName = company.Name,
            Code = branch.Code,
            Name = branch.Name,
            Address = branch.Address ?? string.Empty,
            Phone = branch.Phone ?? string.Empty,
            Email = branch.Email ?? string.Empty,
            IsActive = branch.IsActive
        });
    }

    private static async Task<IResult> UpdateBranchAsync(HttpContext httpContext, Guid id, CreateOrUpdateBranchRequest request, NanchesoftDbContext db)
    {
        var branch = await db.Branches.Include(x => x.Company).Include(x => x.Tenant).FirstOrDefaultAsync(x => x.Id == id);
        if (branch is null)
            return Results.NotFound(new { message = "No se encontró la sucursal." });

        var tenantScopeId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantScopeId.HasValue && branch.TenantId != tenantScopeId.Value)
            return Results.StatusCode(403);

        Company? company = null;
        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
            company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId.Value);
        company ??= await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == branch.CompanyId);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa de la sucursal." });
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantScopeId.HasValue && company.TenantId != tenantScopeId.Value)
            return Results.StatusCode(403);

        var code = string.IsNullOrWhiteSpace(request.Code) ? branch.Code : request.Code.Trim().ToUpperInvariant();
        var name = string.IsNullOrWhiteSpace(request.Name) ? branch.Name : request.Name.Trim();
        var address = request.Address is null ? branch.Address : (string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim());
        var phone = request.Phone is null ? branch.Phone : (string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim());
        var email = request.Email is null ? branch.Email : (string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim());

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y sucursal son obligatorios." });

        var duplicate = await db.Branches.AnyAsync(x => x.Id != id && x.CompanyId == company.Id && x.Code == code);
        if (duplicate)
            return Results.BadRequest(new { message = "Ya existe otra sucursal con ese código dentro de la empresa." });

        branch.TenantId = company.TenantId;
        branch.CompanyId = company.Id;
        branch.Code = code;
        branch.Name = name;
        branch.Address = address;
        branch.Phone = phone;
        branch.Email = email;
        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;
        branch.UpdatedBy = "web-api";
        await db.SaveChangesAsync();

        var tenantName = await db.Tenants.Where(x => x.Id == company.TenantId).Select(x => x.Name).FirstOrDefaultAsync() ?? string.Empty;
        return Results.Ok(new BranchListItemDto
        {
            BranchId = branch.Id,
            TenantId = branch.TenantId,
            TenantName = tenantName,
            CompanyId = branch.CompanyId,
            CompanyName = company.Name,
            Code = branch.Code,
            Name = branch.Name,
            Address = branch.Address ?? string.Empty,
            Phone = branch.Phone ?? string.Empty,
            Email = branch.Email ?? string.Empty,
            IsActive = branch.IsActive
        });
    }

    private static async Task<IResult> DeleteBranchAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var branch = await db.Branches.Include(x => x.Warehouses).FirstOrDefaultAsync(x => x.Id == id);
        if (branch is null)
            return Results.NotFound(new { message = "No se encontró la sucursal." });

        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue && branch.TenantId != tenantId.Value)
            return Results.StatusCode(403);

        if (branch.Warehouses.Any())
            return Results.BadRequest(new { message = "No puedes eliminar una sucursal que ya tiene almacenes relacionados." });

        db.Branches.Remove(branch);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class BranchListItemDto
{
    public Guid BranchId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class BranchCompanyLookupDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class CreateOrUpdateBranchRequest
{
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}