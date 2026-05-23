using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services;

public sealed class ApiTenantScopeHandler : DelegatingHandler
{
    private readonly AppState _appState;
    private readonly AuthState _authState;
    private readonly TenantContextAccessor _tenantAccessor;

    public ApiTenantScopeHandler(AppState appState, AuthState authState, TenantContextAccessor tenantAccessor)
    {
        _appState = appState;
        _authState = authState;
        _tenantAccessor = tenantAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // AsyncLocal (TenantContextAccessor) cruza scopes de DI; el _appState/_authState
        // inyectados aquí provienen del scope del IHttpClientFactory, no del circuito Blazor.
        var ctx = _tenantAccessor.Current;

        var tenantId = ctx?.TenantId ?? _appState.CurrentTenantId ?? _authState.TenantId;
        var companyId = ctx?.CompanyId ?? _appState.CurrentCompanyId ?? _authState.CompanyId;
        var branchId = ctx?.BranchId ?? _appState.CurrentBranchId ?? _authState.BranchId;
        var userId = ctx?.UserId ?? _authState.UserId;
        var isPlatformOwner = ctx?.IsPlatformOwner ?? _authState.IsPlatformOwner;

        ApplyHeader(request, "X-Tenant-Id", tenantId);
        ApplyHeader(request, "X-Company-Id", companyId);
        ApplyHeader(request, "X-Branch-Id", branchId);
        ApplyHeader(request, "X-User-Id", userId);
        if (!request.Headers.Contains("X-Is-Platform-Owner"))
        {
            request.Headers.Add("X-Is-Platform-Owner", isPlatformOwner ? "true" : "false");
        }
        return base.SendAsync(request, cancellationToken);
    }

    private static void ApplyHeader(HttpRequestMessage request, string headerName, Guid? value)
    {
        // Aditivo: nunca sobreescribir un header que el caller ya puso con un valor real.
        if (request.Headers.Contains(headerName))
        {
            return;
        }
        if (value.HasValue && value.Value != Guid.Empty)
        {
            request.Headers.Add(headerName, value.Value.ToString("D"));
        }
    }
}
