using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Domain.Enums;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/core/tenants").WithTags("Tenants");

        group.MapGet("/", GetTenantsAsync);
        group.MapGet("/plans", GetPlansAsync);
        group.MapGet("/selector", GetSelectorAsync);
        group.MapPost("/", CreateTenantAsync);
        group.MapPost("/{id:guid}/seed-roles", SeedRolesAsync);
        group.MapPut("/{id:guid}", UpdateTenantAsync);
        group.MapDelete("/{id:guid}", DeleteTenantAsync);

        return app;
    }

    private static async Task<IResult> GetTenantsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var tenants = await db.Tenants
            .AsNoTracking()
            .Include(x => x.Plan)
            .OrderBy(x => x.Name)
            .Select(x => new TenantListItemDto
            {
                TenantId = x.Id,
                Code = x.Code,
                Name = x.Name,
                LegalName = x.LegalName,
                PlanId = x.PlanId,
                PlanName = x.Plan != null ? x.Plan.Name : string.Empty,
                Status = x.Status.ToString(),
                IsActive = x.IsActive,
                CompaniesCount = db.Companies.Count(y => y.TenantId == x.Id),
                UsersCount = db.Users.Count(y => y.TenantId == x.Id)
            })
            .ToListAsync();

        return Results.Ok(tenants);
    }

    private static async Task<IResult> GetPlansAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var plans = await db.Plans
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new TenantPlanLookupDto
            {
                PlanId = x.Id,
                PlanName = x.Name
            })
            .ToListAsync();

        return Results.Ok(plans);
    }

    private static async Task<IResult> GetSelectorAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var tenants = await db.Tenants
            .AsNoTracking()
            .Where(x => x.IsActive && x.Status == TenantStatus.Active)
            .OrderBy(x => x.Name)
            .Select(x => new TenantSelectorDto
            {
                TenantId = x.Id,
                TenantCode = x.Code,
                TenantName = x.Name,
                CompanyName = db.Companies
                    .Where(y => y.TenantId == x.Id && y.IsActive)
                    .OrderBy(y => y.CreatedAt)
                    .Select(y => y.Name)
                    .FirstOrDefault() ?? string.Empty,
                BranchName = (
                    from company in db.Companies
                    join branch in db.Branches on company.Id equals branch.CompanyId
                    where company.TenantId == x.Id && company.IsActive && branch.IsActive
                    orderby company.CreatedAt, branch.CreatedAt
                    select branch.Name
                ).FirstOrDefault() ?? string.Empty,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(tenants);
    }

    private static async Task<IResult> CreateTenantAsync(HttpContext httpContext, CreateOrUpdateTenantRequest request, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var code = NormalizeCode(request.Code);
        var name = (request.Name ?? string.Empty).Trim();
        var legalName = (request.LegalName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(legalName))
        {
            return Results.BadRequest(new { message = "Código, tenant y razón social son obligatorios." });
        }

        if (!request.PlanId.HasValue || request.PlanId.Value == Guid.Empty)
        {
            return Results.BadRequest(new { message = "El plan es obligatorio." });
        }

        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == request.PlanId.Value && x.IsActive);
        if (plan is null)
        {
            return Results.BadRequest(new { message = "No se encontró el plan enviado." });
        }

        if (await db.Tenants.AnyAsync(x => x.Code == code))
        {
            return Results.BadRequest(new { message = "Ya existe un tenant con ese código." });
        }

        var tenant = new Tenant
        {
            Code = code,
            Name = name,
            LegalName = legalName,
            PlanId = plan.Id,
            Status = ParseStatus(request.Status),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var seeded = await SeedTenantRolesAsync(db, tenant.Id);
        return Results.Ok(new { success = true, id = tenant.Id, seededRoles = seeded });
    }

    /// <summary>
    /// Endpoint para sembrar roles base en un tenant que ya existe pero no tiene roles
    /// (caso de tenants creados antes del auto-seed). Idempotente: si ya tiene roles
    /// con esos códigos, no los duplica.
    /// </summary>
    private static async Task<IResult> SeedRolesAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }

        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (tenant is null)
        {
            return Results.NotFound(new { message = "No se encontró el tenant." });
        }

        var added = await SeedTenantRolesAsync(db, tenant.Id);
        return Results.Ok(new { success = true, tenantId = tenant.Id, addedRoles = added });
    }

    /// <summary>
    /// Crea los roles base para un tenant. Idempotente: si ya existe un rol con
    /// el mismo código en ese tenant, lo respeta y no lo duplica. Devuelve cuántos
    /// roles nuevos se agregaron.
    /// </summary>
    private static async Task<int> SeedTenantRolesAsync(NanchesoftDbContext db, Guid tenantId)
    {
        var baseRoles = new[]
        {
            ("TENANT_ADMIN", "Tenant Admin", "Control total del tenant: usuarios, roles, configuración y operación."),
            ("OPERATOR",     "Operador",     "Captura y operación diaria de ventas, compras, inventario y servicios."),
            ("VIEWER",       "Consulta",     "Solo lectura de catálogos, operación y reportes."),
            ("ACCOUNTING",   "Contabilidad", "Acceso a contabilidad, tesorería, cuentas por pagar/cobrar y CFDI.")
        };

        var existingCodes = await db.Roles
            .Where(r => r.TenantId == tenantId)
            .Select(r => r.Code)
            .ToListAsync();

        var toAdd = baseRoles
            .Where(r => !existingCodes.Contains(r.Item1))
            .Select(r => new Role
            {
                TenantId = tenantId,
                Code = r.Item1,
                Name = r.Item2,
                Description = r.Item3,
                IsSystemRole = true,
                CreatedBy = "web-api"
            })
            .ToList();

        if (toAdd.Count == 0) return 0;

        db.Roles.AddRange(toAdd);
        await db.SaveChangesAsync();
        return toAdd.Count;
    }

    private static async Task<IResult> UpdateTenantAsync(HttpContext httpContext, Guid id, CreateOrUpdateTenantRequest request, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (tenant is null)
        {
            return Results.NotFound(new { message = "No se encontró el tenant." });
        }

        var code = string.IsNullOrWhiteSpace(request.Code) ? tenant.Code : NormalizeCode(request.Code);
        var name = string.IsNullOrWhiteSpace(request.Name) ? tenant.Name : request.Name.Trim();
        var legalName = string.IsNullOrWhiteSpace(request.LegalName) ? tenant.LegalName : request.LegalName.Trim();
        var planId = request.PlanId.HasValue && request.PlanId.Value != Guid.Empty ? request.PlanId.Value : tenant.PlanId;

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(legalName))
        {
            return Results.BadRequest(new { message = "Código, tenant y razón social son obligatorios." });
        }

        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == planId && x.IsActive);
        if (plan is null)
        {
            return Results.BadRequest(new { message = "No se encontró el plan enviado." });
        }

        if (await db.Tenants.AnyAsync(x => x.Id != id && x.Code == code))
        {
            return Results.BadRequest(new { message = "Ya existe otro tenant con ese código." });
        }

        tenant.Code = code;
        tenant.Name = name;
        tenant.LegalName = legalName;
        tenant.PlanId = plan.Id;
        tenant.Status = ParseStatus(request.Status, tenant.Status);
        tenant.IsActive = request.IsActive;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteTenantAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.Forbid();
        }
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (tenant is null)
        {
            return Results.NotFound(new { message = "No se encontró el tenant." });
        }

        var hasCompanies = await db.Companies.AnyAsync(x => x.TenantId == id);
        var hasUsers = await db.Users.AnyAsync(x => x.TenantId == id);

        if (hasCompanies || hasUsers)
        {
            return Results.BadRequest(new { message = "No puedes eliminar un tenant que ya tiene empresas o usuarios. Desactívalo en lugar de borrarlo." });
        }

        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }


    private static bool IsPlatformOwner(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Nanchesoft-Platform-Owner", out var values)
            && bool.TryParse(values.ToString(), out var parsed))
        {
            return parsed;
        }

        return false;
    }

    private static string NormalizeCode(string? code)
        => (code ?? string.Empty).Trim().ToUpperInvariant();

    private static TenantStatus ParseStatus(string? status, TenantStatus fallback = TenantStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return fallback;
        }

        return Enum.TryParse<TenantStatus>(status.Trim(), true, out var parsed)
            ? parsed
            : fallback;
    }
}

public sealed class TenantListItemDto
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CompaniesCount { get; set; }
    public int UsersCount { get; set; }
}

public sealed class TenantPlanLookupDto
{
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
}

public sealed class TenantSelectorDto
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CreateOrUpdateTenantRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public Guid? PlanId { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
}
