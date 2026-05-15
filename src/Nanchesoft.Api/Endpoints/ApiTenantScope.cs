using Microsoft.AspNetCore.Http;

namespace Nanchesoft.Api.Endpoints;

internal static class ApiTenantScope
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string CompanyHeader = "X-Company-Id";
    private const string BranchHeader = "X-Branch-Id";
    private const string PlatformOwnerHeader = "X-Is-Platform-Owner";

    public static bool IsPlatformOwner(HttpContext httpContext)
        => bool.TryParse(httpContext.Request.Headers[PlatformOwnerHeader].ToString(), out var value) && value;

    public static Guid? ResolveTenantId(HttpContext httpContext)
        => TryReadGuid(httpContext, TenantHeader);

    public static Guid? ResolveCompanyId(HttpContext httpContext)
        => TryReadGuid(httpContext, CompanyHeader);

    public static Guid? ResolveBranchId(HttpContext httpContext)
        => TryReadGuid(httpContext, BranchHeader);

    private static Guid? TryReadGuid(HttpContext httpContext, string headerName)
    {
        var raw = httpContext.Request.Headers[headerName].ToString();
        return Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty ? parsed : null;
    }
}
