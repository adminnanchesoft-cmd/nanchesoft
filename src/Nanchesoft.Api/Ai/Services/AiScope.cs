using Microsoft.AspNetCore.Http;
using Nanchesoft.Api.Endpoints;

namespace Nanchesoft.Api.Ai.Services;

public sealed record AiScope(Guid? TenantId, Guid? CompanyId, Guid? BranchId, Guid? UserId, bool IsPlatformOwner)
{
    public static AiScope FromHttp(HttpContext http)
    {
        Guid? userId = null;
        var raw = http.Request.Headers["X-User-Id"].ToString();
        if (Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty)
        {
            userId = parsed;
        }

        return new AiScope(
            ApiTenantScope.ResolveTenantId(http),
            ApiTenantScope.ResolveCompanyId(http),
            ApiTenantScope.ResolveBranchId(http),
            userId,
            ApiTenantScope.IsPlatformOwner(http));
    }
}
