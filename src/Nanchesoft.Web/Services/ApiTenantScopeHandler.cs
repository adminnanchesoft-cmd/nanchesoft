using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services;

public sealed class ApiTenantScopeHandler : DelegatingHandler
{
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public ApiTenantScopeHandler(AppState appState, AuthState authState)
    {
        _appState = appState;
        _authState = authState;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ApplyHeader(request, "X-Tenant-Id", _appState.CurrentTenantId ?? _authState.TenantId);
        ApplyHeader(request, "X-Company-Id", _appState.CurrentCompanyId ?? _authState.CompanyId);
        ApplyHeader(request, "X-Branch-Id", _appState.CurrentBranchId ?? _authState.BranchId);
        request.Headers.Remove("X-Is-Platform-Owner");
        request.Headers.Add("X-Is-Platform-Owner", _authState.IsPlatformOwner ? "true" : "false");
        return base.SendAsync(request, cancellationToken);
    }

    private static void ApplyHeader(HttpRequestMessage request, string headerName, Guid? value)
    {
        request.Headers.Remove(headerName);
        if (value.HasValue && value.Value != Guid.Empty)
        {
            request.Headers.Add(headerName, value.Value.ToString("D"));
        }
    }
}
