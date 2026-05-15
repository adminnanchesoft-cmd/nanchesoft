using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Administration;

public sealed class AccessLogApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public AccessLogApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    public async Task<CatalogViewDefinition> GetCatalogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var logs = await client.GetFromJsonAsync<List<AccessLogRowDto>>("/api/administration/access-logs") ?? [];
        if (!_authState.IsPlatformOwner && _appState.CurrentTenantId.HasValue)
        {
            logs = logs.Where(x => x.TenantId == _appState.CurrentTenantId.Value).ToList();
        }

        var showTenantColumn = _authState.IsPlatformOwner;
        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "AccessLogId", Caption = "AccessLog ID", DataType = "string", AllowEditing = false, Width = 220, Visible = false },
            new() { DataField = "CreatedAt", Caption = "Fecha", DataType = "datetime", AllowEditing = false, Width = 180 },
            new() { DataField = "TenantName", Caption = "Tenant", DataType = "string", AllowEditing = false, Width = 180, Visible = showTenantColumn },
            new() { DataField = "UserDisplayName", Caption = "Usuario", DataType = "string", AllowEditing = false, Width = 220 },
            new() { DataField = "EventType", Caption = "Evento", DataType = "string", AllowEditing = false, Width = 150 },
            new() { DataField = "EventResult", Caption = "Resultado", DataType = "string", AllowEditing = false, Width = 120 },
            new() { DataField = "IpAddress", Caption = "IP", DataType = "string", AllowEditing = false, Width = 130 },
            new() { DataField = "UserAgent", Caption = "User Agent", DataType = "string", AllowEditing = false, Width = 260 },
            new() { DataField = "Details", Caption = "Detalle", DataType = "string", AllowEditing = false, Width = 260 }
        };

        var rows = logs.Select(x => new Dictionary<string, object?>
        {
            ["AccessLogId"] = x.AccessLogId.ToString("D"),
            ["CreatedAt"] = x.CreatedAt,
            ["TenantName"] = x.TenantName,
            ["UserDisplayName"] = x.UserDisplayName,
            ["EventType"] = x.EventType,
            ["EventResult"] = x.EventResult,
            ["IpAddress"] = x.IpAddress,
            ["UserAgent"] = x.UserAgent,
            ["Details"] = x.Details
        }).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = "accesslogs",
            Title = "Bitácora de acceso",
            Subtitle = _authState.IsPlatformOwner ? "Consulta real de accesos desde API + PostgreSQL." : $"Bitácora del tenant activo: {_appState.CurrentTenantName}.",
            KeyExpr = "AccessLogId",
            AllowCreate = false,
            AllowUpdate = false,
            AllowDelete = false,
            Columns = columns,
            Rows = rows,
            TotalCount = rows.Count,
            ActiveCount = rows.Count,
            InactiveCount = 0
        };
    }

    public Task<CatalogViewDefinition> InsertAsync(JsonElement payload) => throw new InvalidOperationException("La bitácora de acceso es solo lectura.");
    public Task<CatalogViewDefinition> UpdateAsync(string key, JsonElement payload) => throw new InvalidOperationException("La bitácora de acceso es solo lectura.");
    public Task<CatalogViewDefinition> DeleteAsync(string key) => throw new InvalidOperationException("La bitácora de acceso es solo lectura.");
}

public sealed class AccessLogRowDto
{
    public Guid AccessLogId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventResult { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
