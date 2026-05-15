using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services;

public sealed class DashboardService
{
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public DashboardService(AppState appState, AuthState authState)
    {
        _appState = appState;
        _authState = authState;
    }

    public Task<DashboardSummary> GetSummaryAsync()
    {
        return Task.FromResult(new DashboardSummary
        {
            TenantName = Resolve(_appState.CurrentTenantName, _authState.TenantName, "Sin tenant"),
            CompanyName = Resolve(_appState.CurrentCompanyName, _authState.CompanyName, "Sin empresa"),
            BranchName = Resolve(_appState.CurrentBranchName, _authState.BranchName, "Sin sucursal"),
            UserName = Resolve(_authState.DisplayName, _authState.Username, "Invitado")
        });
    }

    private static string Resolve(string? primary, string? fallback, string emptyText)
    {
        if (!string.IsNullOrWhiteSpace(primary) && !primary.Equals(emptyText, StringComparison.OrdinalIgnoreCase))
        {
            return primary;
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        return emptyText;
    }
}

public sealed class DashboardSummary
{
    public string TenantName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
