using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Companies;

public sealed class CompanyApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public CompanyApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    public async Task<CatalogViewDefinition> GetCatalogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        var companiesTask = client.GetFromJsonAsync<List<CompanyRowDto>>("/api/organization/companies");
        var tenantsTask = client.GetFromJsonAsync<List<CompanyTenantLookupDto>>("/api/organization/companies/tenants");

        await Task.WhenAll(companiesTask!, tenantsTask!);

        var companies = FilterCompanies(companiesTask.Result ?? []);
        var tenants = FilterTenants(tenantsTask.Result ?? []);

        var showTenantColumn = _authState.IsPlatformOwner;

        var columns = new List<CatalogColumnDefinition>
        {
            new()
            {
                DataField = "CompanyId",
                Caption = "Company ID",
                DataType = "string",
                AllowEditing = false,
                Visible = false,
                Width = 220
            },
            new()
            {
                DataField = "TenantId",
                Caption = "Tenant",
                DataType = "string",
                Required = showTenantColumn,
                Visible = showTenantColumn,
                Width = 220,
                UseLookup = true,
                LookupItems = tenants
                    .Select(x => new CatalogLookupItem
                    {
                        Id = x.TenantId.ToString("D"),
                        Name = x.TenantName
                    })
                    .ToList()
            },
            new()
            {
                DataField = "Code",
                Caption = "Código",
                DataType = "string",
                Required = true,
                Width = 110
            },
            new()
            {
                DataField = "Name",
                Caption = "Empresa",
                DataType = "string",
                Required = true,
                Width = 220
            },
            new()
            {
                DataField = "LegalName",
                Caption = "Razón social",
                DataType = "string",
                Required = true,
                Width = 250
            },
            new()
            {
                DataField = "Rfc",
                Caption = "RFC",
                DataType = "string",
                Required = true,
                Width = 150
            },
            new()
            {
                DataField = "TimeZone",
                Caption = "Zona horaria",
                DataType = "string",
                Required = true,
                Width = 180
            },
            new()
            {
                DataField = "IsActive",
                Caption = "Activo",
                DataType = "boolean",
                Width = 90
            }
        };

        var rows = companies
            .Select(x => new Dictionary<string, object?>
            {
                ["CompanyId"] = x.CompanyId.ToString("D"),
                ["TenantId"] = x.TenantId.ToString("D"),
                ["Code"] = x.Code,
                ["Name"] = x.Name,
                ["LegalName"] = x.LegalName,
                ["Rfc"] = x.Rfc,
                ["TimeZone"] = x.TimeZone,
                ["IsActive"] = x.IsActive
            })
            .ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = "companies",
            Title = "Empresas",
            Subtitle = _authState.IsPlatformOwner
                ? "Administración empresarial real desde API + PostgreSQL."
                : $"Empresas del tenant activo: {_appState.CurrentTenantName}.",
            KeyExpr = "CompanyId",
            AllowCreate = true,
            AllowUpdate = true,
            AllowDelete = true,
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

        var response = await client.PostAsJsonAsync("/api/organization/companies", request);
        await EnsureSuccessAsync(response);

        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string key, JsonElement payload)
    {
        var request = MapRequest(payload);
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        var response = await client.PutAsJsonAsync($"/api/organization/companies/{key}", request);
        await EnsureSuccessAsync(response);

        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string key)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        var response = await client.DeleteAsync($"/api/organization/companies/{key}");
        await EnsureSuccessAsync(response);

        return await GetCatalogAsync();
    }

    private CreateOrUpdateCompanyRequest MapRequest(JsonElement payload)
    {
        var request = new CreateOrUpdateCompanyRequest
        {
            TenantId = ReadGuid(payload, "TenantId"),
            Code = ReadString(payload, "Code"),
            Name = ReadString(payload, "Name"),
            LegalName = ReadString(payload, "LegalName"),
            Rfc = ReadString(payload, "Rfc"),
            TimeZone = ReadString(payload, "TimeZone"),
            IsActive = ReadBool(payload, "IsActive", true)
        };

        if (!_authState.IsPlatformOwner && _appState.CurrentTenantId.HasValue)
        {
            request.TenantId = _appState.CurrentTenantId;
        }

        return request;
    }

    private List<CompanyRowDto> FilterCompanies(List<CompanyRowDto> rows)
    {
        if (_authState.IsPlatformOwner || !_appState.CurrentTenantId.HasValue)
            return rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();

        return rows
            .Where(x => x.TenantId == _appState.CurrentTenantId.Value)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<CompanyTenantLookupDto> FilterTenants(List<CompanyTenantLookupDto> rows)
    {
        if (_authState.IsPlatformOwner || !_appState.CurrentTenantId.HasValue)
            return rows.OrderBy(x => x.TenantName, StringComparer.OrdinalIgnoreCase).ToList();

        return rows
            .Where(x => x.TenantId == _appState.CurrentTenantId.Value)
            .OrderBy(x => x.TenantName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ReadString(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static Guid? ReadGuid(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            Guid.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool ReadBool(JsonElement payload, string name, bool fallback = true)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
        {
            return fallback;
        }

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
        if (payload.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (payload.TryGetProperty(name, out value))
        {
            return true;
        }

        var camel = char.ToLowerInvariant(name[0]) + name[1..];
        if (payload.TryGetProperty(camel, out value))
        {
            return true;
        }

        foreach (var property in payload.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("La API devolvió un error sin detalle.");

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                throw new InvalidOperationException(message.GetString() ?? "La API devolvió un error.");
            }
        }
        catch (JsonException)
        {
        }

        throw new InvalidOperationException(content);
    }
}

public sealed class CompanyRowDto
{
    public Guid CompanyId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CompanyTenantLookupDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class CreateOrUpdateCompanyRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
