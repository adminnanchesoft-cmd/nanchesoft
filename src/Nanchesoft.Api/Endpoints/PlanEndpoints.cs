using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PlanEndpoints
{
    public static IEndpointRouteBuilder MapPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/core/plans").WithTags("Plans");

        group.MapGet("/", GetPlansAsync);
        group.MapPost("/", CreatePlanAsync);
        group.MapPut("/{id:guid}", UpdatePlanAsync);
        group.MapDelete("/{id:guid}", DeletePlanAsync);

        return app;
    }

    private static async Task<IResult> GetPlansAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.StatusCode(403);
        }
        var plans = await db.Plans
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new PlanListItemDto
            {
                PlanId = x.Id,
                Code = x.Code,
                Name = x.Name,
                MaxUsers = x.MaxUsers,
                MaxCompanies = x.MaxCompanies,
                MaxBranches = x.MaxBranches,
                PriceMonthly = x.PriceMonthly,
                IsActive = x.IsActive,
                TenantsCount = db.Tenants.Count(y => y.PlanId == x.Id)
            })
            .ToListAsync();

        return Results.Ok(plans);
    }

    private static async Task<IResult> CreatePlanAsync(HttpContext httpContext, CreateOrUpdatePlanRequest request, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.StatusCode(403);
        }
        var code = NormalizeCode(request.Code);
        var name = (request.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest(new { message = "Código y nombre del plan son obligatorios." });
        }

        if (request.MaxUsers < 0 || request.MaxCompanies < 0 || request.MaxBranches < 0)
        {
            return Results.BadRequest(new { message = "Los límites no pueden ser negativos." });
        }

        if (request.PriceMonthly < 0)
        {
            return Results.BadRequest(new { message = "El precio mensual no puede ser negativo." });
        }

        if (await db.Plans.AnyAsync(x => x.Code == code))
        {
            return Results.BadRequest(new { message = "Ya existe un plan con ese código." });
        }

        var plan = new Plan
        {
            Code = code,
            Name = name,
            MaxUsers = request.MaxUsers,
            MaxCompanies = request.MaxCompanies,
            MaxBranches = request.MaxBranches,
            PriceMonthly = decimal.Round(request.PriceMonthly, 2),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Plans.Add(plan);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, id = plan.Id });
    }

    private static async Task<IResult> UpdatePlanAsync(HttpContext httpContext, Guid id, CreateOrUpdatePlanRequest request, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.StatusCode(403);
        }
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == id);
        if (plan is null)
        {
            return Results.NotFound(new { message = "No se encontró el plan." });
        }

        var code = string.IsNullOrWhiteSpace(request.Code) ? plan.Code : NormalizeCode(request.Code);
        var name = string.IsNullOrWhiteSpace(request.Name) ? plan.Name : request.Name.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest(new { message = "Código y nombre del plan son obligatorios." });
        }

        if (request.MaxUsers < 0 || request.MaxCompanies < 0 || request.MaxBranches < 0)
        {
            return Results.BadRequest(new { message = "Los límites no pueden ser negativos." });
        }

        if (request.PriceMonthly < 0)
        {
            return Results.BadRequest(new { message = "El precio mensual no puede ser negativo." });
        }

        if (await db.Plans.AnyAsync(x => x.Id != id && x.Code == code))
        {
            return Results.BadRequest(new { message = "Ya existe otro plan con ese código." });
        }

        plan.Code = code;
        plan.Name = name;
        plan.MaxUsers = request.MaxUsers;
        plan.MaxCompanies = request.MaxCompanies;
        plan.MaxBranches = request.MaxBranches;
        plan.PriceMonthly = decimal.Round(request.PriceMonthly, 2);
        plan.IsActive = request.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;
        plan.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePlanAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        if (!IsPlatformOwner(httpContext))
        {
            return Results.StatusCode(403);
        }
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == id);
        if (plan is null)
        {
            return Results.NotFound(new { message = "No se encontró el plan." });
        }

        var isInUse = await db.Tenants.AnyAsync(x => x.PlanId == id);
        if (isInUse)
        {
            return Results.BadRequest(new { message = "No puedes eliminar un plan asignado a uno o más tenants. Desactívalo en lugar de borrarlo." });
        }

        db.Plans.Remove(plan);
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
}

public sealed class PlanListItemDto
{
    public Guid PlanId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxCompanies { get; set; }
    public int MaxBranches { get; set; }
    public decimal PriceMonthly { get; set; }
    public bool IsActive { get; set; }
    public int TenantsCount { get; set; }
}

public sealed class CreateOrUpdatePlanRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxCompanies { get; set; }
    public int MaxBranches { get; set; }
    public decimal PriceMonthly { get; set; }
    public bool IsActive { get; set; } = true;
}
