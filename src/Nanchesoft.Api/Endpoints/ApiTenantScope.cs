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

    /// <summary>
    /// Devuelve el alcance del usuario: TenantId obligatorio, CompanyId/BranchId opcionales.
    /// Si no hay tenant en el header, devuelve un error 400 listo para retornar como IResult.
    /// </summary>
    public static TenantScopeResult RequireScope(HttpContext httpContext)
    {
        var tenantId = ResolveTenantId(httpContext);
        if (!tenantId.HasValue)
            return TenantScopeResult.Failure(Results.BadRequest(new
            {
                message = "Sin contexto multitenant: falta header X-Tenant-Id. Selecciona empresa activa en el menú."
            }));

        return TenantScopeResult.Success(tenantId.Value, ResolveCompanyId(httpContext), ResolveBranchId(httpContext));
    }

    private static Guid? TryReadGuid(HttpContext httpContext, string headerName)
    {
        var raw = httpContext.Request.Headers[headerName].ToString();
        return Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty ? parsed : null;
    }
}

internal readonly struct TenantScopeResult
{
    public Guid TenantId { get; }
    public Guid? CompanyId { get; }
    public Guid? BranchId { get; }
    public IResult? Error { get; }
    public bool IsValid => Error is null;

    private TenantScopeResult(Guid tenantId, Guid? companyId, Guid? branchId, IResult? error)
    {
        TenantId = tenantId;
        CompanyId = companyId;
        BranchId = branchId;
        Error = error;
    }

    public static TenantScopeResult Success(Guid tenantId, Guid? companyId, Guid? branchId)
        => new(tenantId, companyId, branchId, null);

    public static TenantScopeResult Failure(IResult error)
        => new(Guid.Empty, null, null, error);
}
