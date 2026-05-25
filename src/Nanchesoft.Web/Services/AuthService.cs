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
    private bool _jsReady;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        AuthState authState,
        AppState appState,
        IJSRuntime jsRuntime)
    {
        _httpClientFactory = httpClientFactory;
        _authState = authState;
        _appState = appState;
        _jsRuntime = jsRuntime;

        // Auto-persist whenever auth state changes (e.g., company/branch switch)
        _authState.OnChange += () =>
        {
            if (_jsReady && _authState.IsAuthenticated)
                _ = PersistSessionAsync();
        };
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

        _jsReady = true;
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
        _jsReady = true;

        if (_authState.IsAuthenticated)
            return true;

        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SessionStorageKey);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            var payload = JsonSerializer.Deserialize<LoginResponse>(json, JsonOptions);
            if (payload is null || !payload.UserId.HasValue)
                return false;

            // If context is incomplete, recover it from the API using the stored userId
            bool contextMissing = !payload.IsPlatformOwner
                && (!payload.TenantId.HasValue
                    || !payload.CompanyId.HasValue
                    || string.IsNullOrWhiteSpace(payload.TenantName)
                    || payload.TenantName is "Sin tenant"
                    || string.IsNullOrWhiteSpace(payload.CompanyName)
                    || payload.CompanyName is "Sin empresa");

            if (contextMissing && payload.UserId.HasValue)
            {
                var recovered = await FetchContextByUserIdAsync(payload.UserId.Value);
                if (recovered is not null)
                {
                    // Preserve user identity fields from localStorage, update context from API
                    recovered.Token = payload.Token;
                    recovered.RefreshToken = payload.RefreshToken;
                    payload = recovered;
                }
                else if (!payload.TenantId.HasValue && !payload.IsPlatformOwner)
                {
                    return false;
                }
            }

            ApplyPayload(payload);
            await PersistSessionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<LoginResponse?> FetchContextByUserIdAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
            var response = await client.GetAsync($"/api/auth/context/{userId}");
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        }
        catch
        {
            return null;
        }
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

    public async Task PersistSessionAsync()
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
