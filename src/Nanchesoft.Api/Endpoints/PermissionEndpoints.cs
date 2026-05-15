using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security/permissions").WithTags("Permissions");

        group.MapGet("/", GetPermissionsAsync);

        return app;
    }

    private static async Task<IResult> GetPermissionsAsync(NanchesoftDbContext db)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Resource)
            .ThenBy(x => x.Action)
            .Select(x => new PermissionListItemDto
            {
                PermissionId = x.Id,
                Code = x.Code,
                Module = x.Module,
                Resource = x.Resource,
                Action = x.Action,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(permissions);
    }
}

public sealed class PermissionListItemDto
{
    public Guid PermissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
