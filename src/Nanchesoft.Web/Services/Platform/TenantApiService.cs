using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;

using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Platform;

public sealed class TenantApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthState _authState;

    public TenantApiService(IHttpClientFactory httpClientFactory, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _authState = authState;
    }

    public async Task<CatalogViewDefinition> GetCatalogAsync()
    {
        var client = CreatePlatformClient();

        var tenantsTask = client.GetFromJsonAsync<List<TenantRowDto>>("/api/core/tenants");
        var plansTask = client.GetFromJsonAsync<List<TenantPlanLookupDto>>("/api/core/tenants/plans");

        await Task.WhenAll(tenantsTask!, plansTask!);

        var tenants = tenantsTask.Result ?? [];
        var plans = plansTask.Result ?? [];

        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "TenantId", Caption = "Tenant ID", DataType = "string", AllowEditing = false, Width = 220, Visible = false },
            new() { DataField = "Code", Caption = "Código", DataType = "string", Required = true, Width = 120 },
            new() { DataField = "Name", Caption = "Tenant", DataType = "string", Required = true, Width = 180 },
            new() { DataField = "LegalName", Caption = "Razón social", DataType = "string", Required = true, Width = 260 },
            new()
            {
                DataField = "PlanId",
                Caption = "Plan",
                DataType = "string",
                Required = true,
                Width = 180,
                UseLookup = true,
                LookupItems = plans.Select(x => new CatalogLookupItem { Id = x.PlanId.ToString("D"), Name = x.PlanName }).ToList()
            },
            new()
            {
                DataField = "Status",
                Caption = "Estatus",
                DataType = "string",
                Required = true,
                Width = 140,
                UseLookup = true,
                LookupItems =
                [
                    new CatalogLookupItem { Id = "Draft", Name = "Borrador" },
                    new CatalogLookupItem { Id = "Active", Name = "Activo" },
                    new CatalogLookupItem { Id = "Suspended", Name = "Suspendido" },
                    new CatalogLookupItem { Id = "Cancelled", Name = "Cancelado" }
                ]
            },
            new() { DataField = "IsActive", Caption = "Activo", DataType = "boolean", Width = 90 },
            new() { DataField = "CompaniesCount", Caption = "Empresas", DataType = "number", AllowEditing = false, Width = 100 },
            new() { DataField = "UsersCount", Caption = "Usuarios", DataType = "number", AllowEditing = false, Width = 100 }
        };

        var rows = tenants.Select(x => new Dictionary<string, object?>
        {
            ["TenantId"] = x.TenantId.ToString("D"),
            ["Code"] = x.Code,
            ["Name"] = x.Name,
            ["LegalName"] = x.LegalName,
            ["PlanId"] = x.PlanId.ToString("D"),
            ["Status"] = x.Status,
            ["IsActive"] = x.IsActive,
            ["CompaniesCount"] = x.CompaniesCount,
            ["UsersCount"] = x.UsersCount
        }).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = "tenants",
            Title = "Tenants",
            Subtitle = "Administración multitenant real para DEMO, SILVASOFT y WORKERTERRA.",
            KeyExpr = "TenantId",
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
        var client = CreatePlatformClient();
        var response = await client.PostAsJsonAsync("/api/core/tenants", request);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string key, JsonElement payload)
    {
        var request = MapRequest(payload);
        var client = CreatePlatformClient();
        var response = await client.PutAsJsonAsync($"/api/core/tenants/{key}", request);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string key)
    {
        var client = CreatePlatformClient();
        var response = await client.DeleteAsync($"/api/core/tenants/{key}");
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    private static CreateOrUpdateTenantRequest MapRequest(JsonElement payload)
    {
        return new CreateOrUpdateTenantRequest
        {
            Code = ReadString(payload, "Code"),
            Name = ReadString(payload, "Name"),
            LegalName = ReadString(payload, "LegalName"),
            PlanId = ReadGuid(payload, "PlanId"),
            Status = ReadString(payload, "Status"),
            IsActive = ReadBool(payload, "IsActive", true)
        };
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
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
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

    private HttpClient CreatePlatformClient()
    {
        EnsurePlatformAccess();

        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        client.DefaultRequestHeaders.Remove("X-Nanchesoft-Platform-Owner");
        client.DefaultRequestHeaders.Add("X-Nanchesoft-Platform-Owner", _authState.IsPlatformOwner ? "true" : "false");
        return client;
    }

    private void EnsurePlatformAccess()
    {
        if (!_authState.IsPlatformOwner)
        {
            throw new InvalidOperationException("Solo el propietario de la plataforma puede administrar tenants, planes y suscripciones SaaS.");
        }
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

public sealed class TenantRowDto
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CompaniesCount { get; set; }
    public int UsersCount { get; set; }
}

public sealed class TenantPlanLookupDto
{
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
}

public sealed class CreateOrUpdateTenantRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public Guid? PlanId { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
}
