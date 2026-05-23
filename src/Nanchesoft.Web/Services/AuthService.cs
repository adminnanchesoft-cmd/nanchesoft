using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services;

public sealed class AuthService
{
    private const string SessionStorageKey = "nanchesoft.auth.session";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthState _authState;
    private readonly AppState _appState;
    private readonly IJSRuntime _jsRuntime;
    private readonly TenantContextAccessor _tenantAccessor;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        AuthState authState,
        AppState appState,
        IJSRuntime jsRuntime,
        TenantContextAccessor tenantAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _authState = authState;
        _appState = appState;
        _jsRuntime = jsRuntime;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<LoginResult> LoginAsync(string usernameOrEmail, string password)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail,
            password
        });

        if (!response.IsSuccessStatusCode)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Credenciales inválidas o acceso no permitido."
            };
        }

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (payload is null)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Respuesta inválida del servidor."
            };
        }

        ApplyPayload(payload);
        await PersistSessionAsync();

        return new LoginResult
        {
            Success = true,
            RequiresTenantSelection = payload.RequiresTenantSelection,
            IsPlatformOwner = payload.IsPlatformOwner,
            MustChangePassword = payload.MustChangePassword
        };
    }


    public async Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string confirmPassword)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            userId,
            currentPassword,
            newPassword,
            confirmPassword
        });

        if (!response.IsSuccessStatusCode)
        {
            var error = await TryReadErrorMessageAsync(response);
            return new ChangePasswordResult
            {
                Success = false,
                ErrorMessage = error ?? "No fue posible cambiar la contraseña."
            };
        }

        _authState.MustChangePassword = false;
        await PersistSessionAsync();

        return new ChangePasswordResult { Success = true };
    }

    public async Task<bool> RestoreSessionAsync()
    {
        if (_authState.IsAuthenticated)
            return true;

        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SessionStorageKey);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            var payload = JsonSerializer.Deserialize<LoginResponse>(json, JsonOptions);
            if (payload is null || !payload.UserId.HasValue || (!payload.TenantId.HasValue && !payload.IsPlatformOwner))
                return false;

            ApplyPayload(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task SetTenantContextAsync(Nanchesoft.Web.State.TenantOption option)
    {
        if (option is null)
            return;

        _authState.SetTenantContext(option);
        _appState.CurrentTenantId = option.TenantId == Guid.Empty ? null : option.TenantId;
        _appState.CurrentTenantName = string.IsNullOrWhiteSpace(option.Name)
            ? (_authState.IsPlatformOwner ? "Plataforma" : "Sin tenant")
            : option.Name;
        _appState.CurrentCompanyId = option.CompanyId == Guid.Empty ? null : option.CompanyId;
        _appState.CurrentCompanyName = string.IsNullOrWhiteSpace(option.CompanyName) ? "Sin empresa" : option.CompanyName;
        _appState.CurrentBranchId = option.BranchId == Guid.Empty ? null : option.BranchId;
        _appState.CurrentBranchName = string.IsNullOrWhiteSpace(option.BranchName) ? "Sin sucursal" : option.BranchName;

        _tenantAccessor.Set(
            _appState.CurrentTenantId,
            _appState.CurrentCompanyId,
            _appState.CurrentBranchId,
            _authState.UserId,
            _authState.IsPlatformOwner);

        await PersistSessionAsync();
    }

    public async Task LogoutAsync()
    {
        _authState.Clear();
        _appState.CurrentTenantId = null;
        _appState.CurrentCompanyId = null;
        _appState.CurrentBranchId = null;
        _appState.CurrentTenantName = "Sin tenant";
        _appState.CurrentCompanyName = "Sin empresa";
        _appState.CurrentBranchName = "Sin sucursal";
        _tenantAccessor.Clear();

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SessionStorageKey);
        }
        catch
        {
        }
    }

    private void ApplyPayload(LoginResponse payload)
    {
        _authState.IsAuthenticated = true;
        _authState.UserId = payload.UserId;
        _authState.TenantId = payload.TenantId;
        _authState.TenantCode = payload.TenantCode ?? string.Empty;
        _authState.TenantName = payload.TenantName ?? string.Empty;
        _authState.CompanyId = payload.CompanyId;
        _authState.CompanyName = payload.CompanyName ?? string.Empty;
        _authState.BranchId = payload.BranchId;
        _authState.BranchName = payload.BranchName ?? string.Empty;
        _authState.Token = payload.Token;
        _authState.RefreshToken = payload.RefreshToken;
        _authState.DisplayName = string.IsNullOrWhiteSpace(payload.DisplayName) ? payload.Username : payload.DisplayName;
        _authState.Username = payload.Username;
        _authState.Email = payload.Email;
        _authState.FirstName = payload.FirstName;
        _authState.LastName = payload.LastName;
        _authState.RoleName = string.IsNullOrWhiteSpace(payload.RoleName) ? "Tenant admin" : payload.RoleName;
        _authState.IsPlatformOwner = payload.IsPlatformOwner;
        _authState.MustChangePassword = payload.MustChangePassword;

        _appState.CurrentTenantId = payload.TenantId;
        _appState.CurrentCompanyId = payload.CompanyId;
        _appState.CurrentBranchId = payload.BranchId;
        _appState.CurrentTenantName = string.IsNullOrWhiteSpace(payload.TenantName)
            ? (payload.IsPlatformOwner ? "Plataforma" : "Sin tenant")
            : payload.TenantName;
        _appState.CurrentCompanyName = string.IsNullOrWhiteSpace(payload.CompanyName) ? "Sin empresa" : payload.CompanyName;
        _appState.CurrentBranchName = string.IsNullOrWhiteSpace(payload.BranchName) ? "Sin sucursal" : payload.BranchName;

        _tenantAccessor.Set(
            payload.TenantId,
            payload.CompanyId,
            payload.BranchId,
            payload.UserId,
            payload.IsPlatformOwner);

        _authState.NotifyStateChanged();
    }


    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString();
        }
        catch
        {
        }

        return null;
    }

    private async Task PersistSessionAsync()
    {
        try
        {
            var snapshot = new LoginResponse
            {
                Token = _authState.Token,
                RefreshToken = _authState.RefreshToken,
                DisplayName = _authState.DisplayName,
                Username = _authState.Username,
                Email = _authState.Email,
                FirstName = _authState.FirstName,
                LastName = _authState.LastName,
                RoleName = _authState.RoleName,
                UserId = _authState.UserId,
                TenantId = _authState.TenantId,
                TenantCode = _authState.TenantCode,
                TenantName = _appState.CurrentTenantName,
                CompanyId = _appState.CurrentCompanyId,
                CompanyName = _appState.CurrentCompanyName,
                BranchId = _appState.CurrentBranchId,
                BranchName = _appState.CurrentBranchName,
                IsPlatformOwner = _authState.IsPlatformOwner,
                RequiresTenantSelection = false,
                MustChangePassword = _authState.MustChangePassword
            };

            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SessionStorageKey, json);
        }
        catch
        {
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string TenantCode { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public bool IsPlatformOwner { get; set; }
        public bool RequiresTenantSelection { get; set; }
        public bool MustChangePassword { get; set; }
    }
}

public sealed class LoginResult
{
    public bool Success { get; set; }
    public bool RequiresTenantSelection { get; set; }
    public bool IsPlatformOwner { get; set; }
    public bool MustChangePassword { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class ChangePasswordResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
