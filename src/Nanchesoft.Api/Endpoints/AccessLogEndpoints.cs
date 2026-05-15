using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class AccessLogEndpoints
{
    public static IEndpointRouteBuilder MapAccessLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/administration/access-logs").WithTags("Access Logs");
        group.MapGet("/", GetAccessLogsAsync);
        return app;
    }

    private static async Task<IResult> GetAccessLogsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.AccessLogs.AsNoTracking().AsQueryable();
        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var logs = await query.OrderByDescending(x => x.CreatedAt).Take(500).ToListAsync();
        var tenantIds = logs.Where(x => x.TenantId.HasValue).Select(x => x.TenantId!.Value).Distinct().ToList();
        var userIds = logs.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
        var tenantMap = await db.Tenants.AsNoTracking().Where(x => tenantIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        var userMap = await db.Users.AsNoTracking().Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.GetDisplayName());

        var result = logs.Select(x => new AccessLogListItemDto
        {
            AccessLogId = x.Id,
            TenantId = x.TenantId ?? Guid.Empty,
            TenantName = x.TenantId.HasValue && tenantMap.TryGetValue(x.TenantId.Value, out var tenantName) ? tenantName : string.Empty,
            UserDisplayName = x.UserId.HasValue && userMap.TryGetValue(x.UserId.Value, out var userName) ? userName : string.Empty,
            EventType = x.EventType.ToString(),
            EventResult = x.EventResult,
            IpAddress = x.IpAddress ?? string.Empty,
            UserAgent = x.UserAgent ?? string.Empty,
            Details = x.Details ?? string.Empty,
            CreatedAt = x.CreatedAt
        }).ToList();

        return Results.Ok(result);
    }
}

public sealed class AccessLogListItemDto
{
    public Guid AccessLogId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventResult { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
