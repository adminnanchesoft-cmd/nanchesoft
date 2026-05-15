using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Branches;

public sealed class BranchApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public BranchApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    public async Task<CatalogViewDefinition> GetCatalogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var branchesTask = client.GetFromJsonAsync<List<BranchRowDto>>("/api/organization/branches");
        var companiesTask = client.GetFromJsonAsync<List<BranchCompanyLookupDto>>("/api/organization/branches/companies");
        await Task.WhenAll(branchesTask!, companiesTask!);

        var branches = FilterBranches(branchesTask.Result ?? []);
        var companies = FilterCompanies(companiesTask.Result ?? []);
        var showTenantColumn = _authState.IsPlatformOwner;

        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "BranchId", Caption = "Branch ID", DataType = "string", AllowEditing = false, Width = 220, Visible = false },
            new() { DataField = "TenantName", Caption = "Tenant", DataType = "string", AllowEditing = false, Width = 180, Visible = showTenantColumn },
            new() { DataField = "CompanyId", Caption = "Empresa", DataType = "string", Required = true, Width = 220, UseLookup = true, LookupItems = companies.Select(x => new CatalogLookupItem { Id = x.CompanyId.ToString("D"), Name = x.CompanyName }).ToList() },
            new() { DataField = "Code", Caption = "Código", DataType = "string", Required = true, Width = 110 },
            new() { DataField = "Name", Caption = "Sucursal", DataType = "string", Required = true, Width = 220 },
            new() { DataField = "Address", Caption = "Dirección", DataType = "string", Width = 260 },
            new() { DataField = "Phone", Caption = "Teléfono", DataType = "string", Width = 150 },
            new() { DataField = "Email", Caption = "Correo", DataType = "string", Width = 220 },
            new() { DataField = "IsActive", Caption = "Activo", DataType = "boolean", Width = 90 }
        };

        var rows = branches.Select(x => new Dictionary<string, object?>
        {
            ["BranchId"] = x.BranchId.ToString("D"),
            ["TenantName"] = x.TenantName,
            ["CompanyId"] = x.CompanyId.ToString("D"),
            ["Code"] = x.Code,
            ["Name"] = x.Name,
            ["Address"] = x.Address,
            ["Phone"] = x.Phone,
            ["Email"] = x.Email,
            ["IsActive"] = x.IsActive
        }).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = "branches",
            Title = "Sucursales",
            Subtitle = _authState.IsPlatformOwner ? "Administración real de sucursales desde API + PostgreSQL." : $"Sucursales del tenant activo: {_appState.CurrentTenantName}.",
            KeyExpr = "BranchId",
            Columns = columns,
            Rows = rows,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => x.TryGetValue("IsActive", out var value) && value is bool active && active),
            InactiveCount = rows.Count(x => x.TryGetValue("IsActive", out var value) && value is bool active && !active)
        };
    }

    public async Task<CatalogViewDefinition> InsertAsync(JsonElement payload)
    {
        var request = MapRequest(payload);
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsJsonAsync("/api/organization/branches", request);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string key, JsonElement payload)
    {
        var request = MapRequest(payload);
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PutAsJsonAsync($"/api/organization/branches/{key}", request);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string key)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.DeleteAsync($"/api/organization/branches/{key}");
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    private List<BranchRowDto> FilterBranches(List<BranchRowDto> rows)
    {
        if (_authState.IsPlatformOwner || !_appState.CurrentTenantId.HasValue)
            return rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return rows.Where(x => x.TenantId == _appState.CurrentTenantId.Value)
                   .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<BranchCompanyLookupDto> FilterCompanies(List<BranchCompanyLookupDto> rows)
    {
        var filtered = rows.AsEnumerable();
        if (!_authState.IsPlatformOwner && _appState.CurrentTenantId.HasValue)
            filtered = filtered.Where(x => x.TenantId == _appState.CurrentTenantId.Value);
        if (!_authState.IsPlatformOwner && _appState.CurrentCompanyId.HasValue)
            filtered = filtered.Where(x => x.CompanyId == _appState.CurrentCompanyId.Value);
        return filtered.OrderBy(x => x.CompanyName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static CreateOrUpdateBranchRequest MapRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Address = ReadString(payload, "Address"),
        Phone = ReadString(payload, "Phone"),
        Email = ReadString(payload, "Email"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static string ReadString(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;
        return string.Empty;
    }

    private static Guid? ReadGuid(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static bool ReadBool(JsonElement payload, string name, bool fallback = true)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value)) return fallback;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static bool TryGetPropertyInsensitive(JsonElement payload, string name, out JsonElement value)
    {
        if (payload.ValueKind != JsonValueKind.Object) { value = default; return false; }
        if (payload.TryGetProperty(name, out value)) return true;
        var camel = char.ToLowerInvariant(name[0]) + name[1..];
        if (payload.TryGetProperty(camel, out value)) return true;
        foreach (var property in payload.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) { value = property.Value; return true; }
        value = default; return false;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) throw new InvalidOperationException("La API devolvió un error sin detalle.");
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var message))
                throw new InvalidOperationException(message.GetString() ?? "La API devolvió un error.");
        }
        catch (JsonException) { }
        throw new InvalidOperationException(content);
    }
}

public sealed class BranchRowDto
{
    public Guid BranchId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class BranchCompanyLookupDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class CreateOrUpdateBranchRequest
{
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}
