using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services;

public sealed class ContextService
{
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public ContextService(AppState appState, AuthState authState)
    {
        _appState = appState;
        _authState = authState;
    }

    public Task<List<TenantOption>> GetAvailableTenantsAsync()
    {
        var result = new List<TenantOption>();

        if (_authState.AccessibleTenantIds.Count > 0)
        {
            foreach (var tenantId in _authState.AccessibleTenantIds)
            {
                result.Add(new TenantOption
                {
                    Id = tenantId,
                    Name = ResolveTenantName(tenantId)
                });
            }

            return Task.FromResult(result);
        }

        if (_appState.CurrentTenantId.HasValue)
        {
            result.Add(new TenantOption
            {
                Id = _appState.CurrentTenantId.Value,
                Name = string.IsNullOrWhiteSpace(_appState.CurrentTenantName)
                    ? ResolveTenantName(_appState.CurrentTenantId.Value)
                    : _appState.CurrentTenantName
            });

            return Task.FromResult(result);
        }

        result.Add(new TenantOption
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "NANCHESOFT_DEMO"
        });

        return Task.FromResult(result);
    }

    public Task SetTenantAsync(Guid tenantId)
    {
        if (_authState.AccessibleTenantIds.Count > 0 && !_authState.AccessibleTenantIds.Contains(tenantId))
        {
            return Task.CompletedTask;
        }

        _authState.TenantId = tenantId;
        _appState.CurrentTenantId = tenantId;
        _appState.CurrentTenantName = ResolveTenantName(tenantId);

        if (_authState.AccessibleTenantIds.Count == 1)
        {
            return Task.CompletedTask;
        }

        if (tenantId == Guid.Parse("11111111-1111-1111-1111-111111111111"))
        {
            _appState.CurrentCompanyName = "Nanchesoft Demo Company";
            _appState.CurrentBranchName = "Matriz";
        }

        return Task.CompletedTask;
    }

    private string ResolveTenantName(Guid tenantId)
    {
        if (_appState.CurrentTenantId == tenantId && !string.IsNullOrWhiteSpace(_appState.CurrentTenantName))
        {
            return _appState.CurrentTenantName;
        }

        return tenantId.ToString() switch
        {
            "11111111-1111-1111-1111-111111111111" => "NANCHESOFT_DEMO",
            "22222222-2222-2222-2222-222222222222" => "SILVASOFT",
            "33333333-3333-3333-3333-333333333333" => "WORKERTERRA",
            _ => $"Tenant {tenantId.ToString()[..8]}"
        };
    }
}

public sealed class TenantOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
