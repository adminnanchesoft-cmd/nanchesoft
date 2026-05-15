using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization/companies").WithTags("Companies");

        group.MapGet("/", GetCompaniesAsync);
        group.MapGet("/tenants", GetTenantsAsync);
        group.MapPost("/", CreateCompanyAsync);
        group.MapPut("/{id:guid}", UpdateCompanyAsync);
        group.MapDelete("/{id:guid}", DeleteCompanyAsync);

        return app;
    }

    private static async Task<IResult> GetCompaniesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Companies
            .AsNoTracking()
            .Include(x => x.Tenant)
            .AsQueryable();

        if (!isPlatformOwner && tenantId.HasValue)
        {
            query = query.Where(x => x.TenantId == tenantId.Value);
        }

        var companies = await query
            .OrderBy(x => x.Name)
            .Select(x => new CompanyListItemDto
            {
                CompanyId = x.Id,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                LegalName = x.LegalName,
                Rfc = x.TaxId,
                TimeZone = x.Timezone,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(companies);
    }

    private static async Task<IResult> GetTenantsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Tenants
            .AsNoTracking()
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!isPlatformOwner && tenantId.HasValue)
        {
            query = query.Where(x => x.Id == tenantId.Value);
        }

        var tenants = await query
            .OrderBy(x => x.Name)
            .Select(x => new TenantLookupDto
            {
                TenantId = x.Id,
                TenantName = x.Name
            })
            .ToListAsync();

        return Results.Ok(tenants);
    }

    private static async Task<IResult> CreateCompanyAsync(HttpContext httpContext, CreateOrUpdateCompanyRequest request, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        if (!isPlatformOwner && tenantId.HasValue)
        {
            request.TenantId = tenantId;
        }

        var tenant = await ResolveTenantAsync(request, db);
        if (tenant is null)
        {
            return Results.BadRequest(new { message = "No se encontró el tenant enviado." });
        }

        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        var name = (request.Name ?? string.Empty).Trim();
        var legalName = (request.LegalName ?? string.Empty).Trim();
        var rfc = (request.Rfc ?? string.Empty).Trim().ToUpperInvariant();
        var timeZone = string.IsNullOrWhiteSpace(request.TimeZone)
            ? "America/Mexico_City"
            : request.TimeZone.Trim();

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(legalName) ||
            string.IsNullOrWhiteSpace(rfc))
        {
            return Results.BadRequest(new { message = "Código, empresa, razón social y RFC son obligatorios." });
        }

        var exists = await db.Companies.AnyAsync(x => x.TenantId == tenant.Id && x.Code == code);
        if (exists)
        {
            return Results.BadRequest(new { message = "Ya existe una empresa con ese código dentro del tenant." });
        }

        var company = new Company
        {
            TenantId = tenant.Id,
            Code = code,
            Name = name,
            LegalName = legalName,
            TaxId = rfc,
            Timezone = timeZone,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();

        return Results.Ok(new CompanyListItemDto
        {
            CompanyId = company.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Code = company.Code,
            Name = company.Name,
            LegalName = company.LegalName,
            Rfc = company.TaxId,
            TimeZone = company.Timezone,
            IsActive = company.IsActive
        });
    }

    private static async Task<IResult> UpdateCompanyAsync(HttpContext httpContext, Guid id, CreateOrUpdateCompanyRequest request, NanchesoftDbContext db)
    {
        var company = await db.Companies.Include(x => x.Tenant).FirstOrDefaultAsync(x => x.Id == id);
        if (company is null)
        {
            return Results.NotFound(new { message = "No se encontró la empresa." });
        }

        var tenantScopeId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        if (!isPlatformOwner && tenantScopeId.HasValue)
        {
            if (company.TenantId != tenantScopeId.Value)
                return Results.Forbid();
            request.TenantId = tenantScopeId;
        }

        Tenant? tenant = null;
        if (request.TenantId.HasValue && request.TenantId.Value != Guid.Empty)
        {
            tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == request.TenantId.Value);
        }
        tenant ??= await db.Tenants.FirstOrDefaultAsync(x => x.Id == company.TenantId);
        if (tenant is null)
        {
            return Results.BadRequest(new { message = "No se encontró el tenant de la empresa." });
        }

        var code = string.IsNullOrWhiteSpace(request.Code) ? company.Code : request.Code.Trim().ToUpperInvariant();
        var name = string.IsNullOrWhiteSpace(request.Name) ? company.Name : request.Name.Trim();
        var legalName = string.IsNullOrWhiteSpace(request.LegalName) ? company.LegalName : request.LegalName.Trim();
        var rfc = string.IsNullOrWhiteSpace(request.Rfc) ? company.TaxId : request.Rfc.Trim().ToUpperInvariant();
        var timeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? company.Timezone : request.TimeZone.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(legalName) || string.IsNullOrWhiteSpace(rfc))
        {
            return Results.BadRequest(new { message = "Código, empresa, razón social y RFC son obligatorios." });
        }

        var duplicate = await db.Companies.AnyAsync(x => x.Id != id && x.TenantId == tenant.Id && x.Code == code);
        if (duplicate)
        {
            return Results.BadRequest(new { message = "Ya existe otra empresa con ese código dentro del tenant." });
        }

        company.TenantId = tenant.Id;
        company.Code = code;
        company.Name = name;
        company.LegalName = legalName;
        company.TaxId = rfc;
        company.Timezone = timeZone;
        company.IsActive = request.IsActive;
        company.UpdatedAt = DateTime.UtcNow;
        company.UpdatedBy = "web-api";

        await db.SaveChangesAsync();

        return Results.Ok(new CompanyListItemDto
        {
            CompanyId = company.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Code = company.Code,
            Name = company.Name,
            LegalName = company.LegalName,
            Rfc = company.TaxId,
            TimeZone = company.Timezone,
            IsActive = company.IsActive
        });
    }

    private static async Task<IResult> DeleteCompanyAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var company = await db.Companies.Include(x => x.Branches).FirstOrDefaultAsync(x => x.Id == id);
        if (company is null)
        {
            return Results.NotFound(new { message = "No se encontró la empresa." });
        }

        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue && company.TenantId != tenantId.Value)
        {
            return Results.Forbid();
        }

        if (company.Branches.Any())
        {
            return Results.BadRequest(new { message = "No puedes eliminar una empresa que ya tiene sucursales relacionadas." });
        }

        db.Companies.Remove(company);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<Tenant?> ResolveTenantAsync(CreateOrUpdateCompanyRequest request, NanchesoftDbContext db)
    {
        if (request.TenantId.HasValue && request.TenantId.Value != Guid.Empty)
            return await db.Tenants.FirstOrDefaultAsync(x => x.Id == request.TenantId.Value);
        if (!string.IsNullOrWhiteSpace(request.TenantName))
            return await db.Tenants.FirstOrDefaultAsync(x => x.Name == request.TenantName.Trim());
        return null;
    }
}

public sealed class CompanyListItemDto
{
    public Guid CompanyId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class TenantLookupDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class CreateOrUpdateCompanyRequest
{
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}