using Microsoft.EntityFrameworkCore;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/administration/sessions").WithTags("Sessions");
        group.MapGet("/", GetSessionsAsync);
        return app;
    }

    private static async Task<IResult> GetSessionsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.UserSessions.AsNoTracking().Include(x => x.User).AsQueryable();
        if (!isPlatformOwner && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var sessions = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        var tenantIds = sessions.Select(x => x.TenantId).Distinct().ToList();
        var tenantMap = await db.Tenants.AsNoTracking().Where(x => tenantIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);

        var result = sessions.Select(x => new SessionListItemDto
        {
            SessionId = x.Id,
            TenantId = x.TenantId,
            TenantName = tenantMap.TryGetValue(x.TenantId, out var tenantName) ? tenantName : string.Empty,
            UserId = x.UserId,
            UserDisplayName = x.User?.GetDisplayName() ?? string.Empty,
            RefreshTokenPreview = BuildTokenPreview(x.RefreshToken),
            ExpiresAt = x.ExpiresAt,
            RevokedAt = x.RevokedAt,
            IpAddress = x.IpAddress ?? string.Empty,
            UserAgent = x.UserAgent ?? string.Empty,
            IsActive = x.RevokedAt is null && x.ExpiresAt > DateTime.UtcNow
        }).ToList();

        return Results.Ok(result);
    }

    private static string BuildTokenPreview(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        var trimmed = token.Trim();
        return trimmed.Length <= 12 ? trimmed : $"{trimmed[..6]}...{trimmed[^4..]}";
    }
}

public sealed class SessionListItemDto
{
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string RefreshTokenPreview { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
