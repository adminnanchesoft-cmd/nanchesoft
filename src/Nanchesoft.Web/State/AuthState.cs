using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanchesoft.Web.State;

public sealed class AuthState
{
    private readonly HashSet<Guid> _accessibleTenantIds = new();
    private List<TenantOption> _tenantOptions = new();
    private string _displayName = "Invitado";
    private string _accessToken = string.Empty;

    public event Action? OnChange;

    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    public string FullName
    {
        get
        {
            var fullName = string.Join(" ", new[] { FirstName, LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            return string.IsNullOrWhiteSpace(fullName) ? _displayName : fullName;
        }
    }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_displayName))
            {
                return _displayName;
            }

            if (!string.IsNullOrWhiteSpace(FullName))
            {
                return FullName;
            }

            if (!string.IsNullOrWhiteSpace(Username))
            {
                return Username;
            }

            return "Invitado";
        }
        set => _displayName = string.IsNullOrWhiteSpace(value) ? "Invitado" : value.Trim();
    }

    public string AccessToken
    {
        get => _accessToken;
        set => _accessToken = value ?? string.Empty;
    }

    public string Token
    {
        get => _accessToken;
        set => _accessToken = value ?? string.Empty;
    }

    public string RefreshToken { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsPlatformOwner { get; set; }
    public bool MustChangePassword { get; set; }

    public Guid? TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public IReadOnlyCollection<Guid> AccessibleTenantIds => _accessibleTenantIds;
    public IReadOnlyList<TenantOption> TenantOptions => _tenantOptions;

    public void SetAccessibleTenants(IEnumerable<Guid>? tenantIds)
    {
        _accessibleTenantIds.Clear();

        if (tenantIds is not null)
        {
            foreach (var tenantId in tenantIds.Where(x => x != Guid.Empty))
            {
                _accessibleTenantIds.Add(tenantId);
            }
        }

        NotifyStateChanged();
    }

    public void SetTenantOptions(IEnumerable<TenantOption>? tenantOptions)
    {
        _tenantOptions = tenantOptions?
            .Where(x => x is not null)
            .GroupBy(x => x.TenantId)
            .Select(x => x.First())
            .OrderBy(x => x.DisplayLabel)
            .ToList()
            ?? new List<TenantOption>();

        if (_tenantOptions.Count > 0 && _accessibleTenantIds.Count == 0)
        {
            foreach (var option in _tenantOptions.Where(x => x.TenantId != Guid.Empty))
            {
                _accessibleTenantIds.Add(option.TenantId);
            }
        }

        NotifyStateChanged();
    }

    public void SetTenantContext(TenantOption option)
    {
        if (option is null)
        {
            return;
        }

        TenantId = option.TenantId == Guid.Empty ? null : option.TenantId;
        TenantCode = option.Code ?? string.Empty;
        TenantName = option.Name ?? string.Empty;
        CompanyId = option.CompanyId == Guid.Empty ? null : option.CompanyId;
        CompanyName = option.CompanyName ?? string.Empty;
        BranchId = option.BranchId == Guid.Empty ? null : option.BranchId;
        BranchName = option.BranchName ?? string.Empty;

        if (option.TenantId != Guid.Empty)
        {
            _accessibleTenantIds.Add(option.TenantId);
        }

        NotifyStateChanged();
    }

    public void Clear()
    {
        IsAuthenticated = false;
        UserId = null;
        Username = string.Empty;
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        AvatarUrl = string.Empty;
        _displayName = "Invitado";
        _accessToken = string.Empty;
        RefreshToken = string.Empty;
        RoleName = string.Empty;
        IsPlatformOwner = false;
        MustChangePassword = false;
        TenantId = null;
        TenantCode = string.Empty;
        TenantName = string.Empty;
        CompanyId = null;
        CompanyName = string.Empty;
        BranchId = null;
        BranchName = string.Empty;
        _accessibleTenantIds.Clear();
        _tenantOptions.Clear();
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnChange?.Invoke();
}

public sealed class TenantOption
{
    public Guid TenantId { get; set; }
    public Guid Id
    {
        get => TenantId;
        set => TenantId = value;
    }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public string DisplayLabel
    {
        get
        {
            var segments = new List<string>();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                segments.Add(Name.Trim());
            }

            if (!string.IsNullOrWhiteSpace(CompanyName))
            {
                segments.Add(CompanyName.Trim());
            }

            if (!string.IsNullOrWhiteSpace(BranchName))
            {
                segments.Add(BranchName.Trim());
            }

            if (!string.IsNullOrWhiteSpace(Code))
            {
                segments.Add(Code.Trim());
            }

            return segments.Count == 0 ? "Tenant" : string.Join(" • ", segments.Distinct());
        }
    }
}
