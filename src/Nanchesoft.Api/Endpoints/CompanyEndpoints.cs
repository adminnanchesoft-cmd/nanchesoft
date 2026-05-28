using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Nanchesoft.Api.Endpoints;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization/companies").WithTags("Companies");

        group.MapGet("/", GetCompaniesAsync);
        group.MapGet("/tenants", GetTenantsAsync);
        group.MapPost("/", CreateCompanyAsync);
        group.MapPost("/{id:guid}/logo", UploadCompanyLogoAsync).DisableAntiforgery();
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

        // Always filter by tenant when context is specified, even for platform owners
        // (mirrors ApiTenantScopeHandler which sends the header regardless of role)
        if (tenantId.HasValue)
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
                LogoUrl = x.LogoUrl,
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

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            return Results.BadRequest(new { message = $"Zona horaria no válida: '{timeZone}'." });

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
                return Results.StatusCode(403);
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

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            return Results.BadRequest(new { message = $"Zona horaria no válida: '{timeZone}'." });

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
            return Results.StatusCode(403);
        }

        if (company.Branches.Any())
        {
            return Results.BadRequest(new { message = "No puedes eliminar una empresa que ya tiene sucursales relacionadas." });
        }

        db.Companies.Remove(company);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> UploadCompanyLogoAsync(
        Guid id,
        IFormFile file,
        HttpContext httpContext,
        NanchesoftDbContext db,
        IConfiguration configuration)
    {
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        if (!isPlatformOwner && tenantId is null)
            return Results.StatusCode(403);

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "No se recibió ninguna imagen." });

        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return Results.BadRequest(new { message = "La imagen no debe superar 5 MB." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return Results.BadRequest(new { message = "Formato no permitido. Usa JPG, PNG o WEBP." });

        var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == id);
        if (company is null)
            return Results.NotFound(new { message = "No se encontró la empresa." });
        if (!isPlatformOwner && tenantId.HasValue && company.TenantId != tenantId.Value)
            return Results.NotFound(new { message = "No se encontró la empresa." });

        var root = configuration["Uploads:RootPath"] ?? "/opt/nanchesoft/uploads";
        var companiesDir = Path.Combine(root, "companies");
        Directory.CreateDirectory(companiesDir);
        var filePath = Path.Combine(companiesDir, $"{id}.jpg");

        using (var image = await Image.LoadAsync(file.OpenReadStream()))
        {
            var size = Math.Min(image.Width, image.Height);
            var x = (image.Width - size) / 2;
            var y = (image.Height - size) / 2;
            image.Mutate(ctx => ctx
                .Crop(new Rectangle(x, y, size, size))
                .Resize(256, 256));
            var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 85 };
            await image.SaveAsJpegAsync(filePath, encoder);
        }

        company.LogoUrl = $"/uploads/companies/{id}.jpg";
        await db.SaveChangesAsync();

        return Results.Ok(new { logoUrl = company.LogoUrl });
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
    public string? LogoUrl { get; set; }
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