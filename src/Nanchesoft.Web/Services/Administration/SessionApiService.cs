using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Administration;

public sealed class SessionApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public SessionApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    public async Task<CatalogViewDefinition> GetCatalogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var sessions = await client.GetFromJsonAsync<List<SessionRowDto>>("/api/administration/sessions") ?? [];

        if (!_authState.IsPlatformOwner && _appState.CurrentTenantId.HasValue)
        {
            sessions = sessions.Where(x => x.TenantId == _appState.CurrentTenantId.Value).ToList();
        }

        var showTenantColumn = _authState.IsPlatformOwner;
        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "SessionId", Caption = "Session ID", DataType = "string", AllowEditing = false, Width = 220, Visible = false },
            new() { DataField = "TenantName", Caption = "Tenant", DataType = "string", AllowEditing = false, Width = 180, Visible = showTenantColumn },
            new() { DataField = "UserDisplayName", Caption = "Usuario", DataType = "string", AllowEditing = false, Width = 220 },
            new() { DataField = "RefreshTokenPreview", Caption = "Refresh token", DataType = "string", AllowEditing = false, Width = 170 },
            new() { DataField = "ExpiresAt", Caption = "Expira", DataType = "datetime", AllowEditing = false, Width = 180 },
            new() { DataField = "RevokedAt", Caption = "Revocada", DataType = "datetime", AllowEditing = false, Width = 180 },
            new() { DataField = "IpAddress", Caption = "IP", DataType = "string", AllowEditing = false, Width = 130 },
            new() { DataField = "UserAgent", Caption = "User Agent", DataType = "string", AllowEditing = false, Width = 260 },
            new() { DataField = "IsActive", Caption = "Activa", DataType = "boolean", AllowEditing = false, Width = 90 }
        };

        var rows = sessions.Select(x => new Dictionary<string, object?>
        {
            ["SessionId"] = x.SessionId.ToString("D"),
            ["TenantName"] = x.TenantName,
            ["UserDisplayName"] = x.UserDisplayName,
            ["RefreshTokenPreview"] = x.RefreshTokenPreview,
            ["ExpiresAt"] = x.ExpiresAt,
            ["RevokedAt"] = x.RevokedAt,
            ["IpAddress"] = x.IpAddress,
            ["UserAgent"] = x.UserAgent,
            ["IsActive"] = x.IsActive
        }).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = "sessions",
            Title = "Sesiones activas",
            Subtitle = _authState.IsPlatformOwner ? "Consulta real de sesiones desde API + PostgreSQL." : $"Sesiones del tenant activo: {_appState.CurrentTenantName}.",
            KeyExpr = "SessionId",
            AllowCreate = false,
            AllowUpdate = false,
            AllowDelete = false,
            Columns = columns,
            Rows = rows,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => x.TryGetValue("IsActive", out var value) && value is bool active && active),
            InactiveCount = rows.Count(x => x.TryGetValue("IsActive", out var value) && value is bool active && !active)
        };
    }

    public Task<CatalogViewDefinition> InsertAsync(JsonElement payload) => throw new InvalidOperationException("El catálogo de sesiones es solo lectura en esta implementación.");
    public Task<CatalogViewDefinition> UpdateAsync(string key, JsonElement payload) => throw new InvalidOperationException("El catálogo de sesiones es solo lectura en esta implementación.");
    public Task<CatalogViewDefinition> DeleteAsync(string key) => throw new InvalidOperationException("El catálogo de sesiones es solo lectura en esta implementación.");
}

public sealed class SessionRowDto
{
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string RefreshTokenPreview { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
