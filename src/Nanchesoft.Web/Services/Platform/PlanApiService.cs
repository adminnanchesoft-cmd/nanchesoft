using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;

using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Platform;

public sealed class PlanApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthState _authState;

    public PlanApiService(IHttpClientFactory httpClientFactory, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _authState = authState;
    }

    public async Task<CatalogViewDefinition> GetCatalogAsync()
    {
        var client = CreatePlatformClient();
        var plans = await client.GetFromJsonAsync<List<PlanRowDto>>("/api/core/plans") ?? [];

        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "PlanId", Caption = "Plan ID", DataType = "string", AllowEditing = false, Visible = false, Width = 220 },
            new() { DataField = "Code", Caption = "Código", DataType = "string", Required = true, Width = 120 },
            new() { DataField = "Name", Caption = "Plan", DataType = "string", Required = true, Width = 220 },
            new() { DataField = "PriceMonthly", Caption = "Mensualidad", DataType = "currency", Required = true, Width = 140 },
            new() { DataField = "MaxUsers", Caption = "Usuarios máx.", DataType = "number", Required = true, Width = 120 },
            new() { DataField = "MaxCompanies", Caption = "Empresas máx.", DataType = "number", Required = true, Width = 120 },
            new() { DataField = "MaxBranches", Caption = "Sucursales máx.", DataType = "number", Required = true, Width = 130 },
            new() { DataField = "IsActive", Caption = "Activo", DataType = "boolean", Width = 90 },
            new() { DataField = "TenantsCount", Caption = "Tenants", DataType = "number", AllowEditing = false, Width = 100 }
        };

        var rows = plans.Select(x => new Dictionary<string, object?>
        {
            ["PlanId"] = x.PlanId.ToString("D"),
            ["Code"] = x.Code,
            ["Name"] = x.Name,
            ["PriceMonthly"] = x.PriceMonthly,
            ["MaxUsers"] = x.MaxUsers,
            ["MaxCompanies"] = x.MaxCompanies,
            ["MaxBranches"] = x.MaxBranches,
            ["IsActive"] = x.IsActive,
            ["TenantsCount"] = x.TenantsCount
        }).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = "plans",
            Title = "Planes SaaS",
            Subtitle = "Catálogo maestro de planes para asignar suscripciones a tenants como Silvasoft.",
            KeyExpr = "PlanId",
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
        var response = await client.PostAsJsonAsync("/api/core/plans", request);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string key, JsonElement payload)
    {
        var request = MapRequest(payload);
        var client = CreatePlatformClient();
        var response = await client.PutAsJsonAsync($"/api/core/plans/{key}", request);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string key)
    {
        var client = CreatePlatformClient();
        var response = await client.DeleteAsync($"/api/core/plans/{key}");
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    private static CreateOrUpdatePlanRequest MapRequest(JsonElement payload)
    {
        return new CreateOrUpdatePlanRequest
        {
            Code = ReadString(payload, "Code"),
            Name = ReadString(payload, "Name"),
            PriceMonthly = ReadDecimal(payload, "PriceMonthly"),
            MaxUsers = ReadInt(payload, "MaxUsers"),
            MaxCompanies = ReadInt(payload, "MaxCompanies"),
            MaxBranches = ReadInt(payload, "MaxBranches"),
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

    private static int ReadInt(JsonElement payload, string name, int fallback = 0)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
        {
            return fallback;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static decimal ReadDecimal(JsonElement payload, string name, decimal fallback = 0m)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
        {
            return fallback;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return fallback;
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

public sealed class PlanRowDto
{
    public Guid PlanId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxCompanies { get; set; }
    public int MaxBranches { get; set; }
    public decimal PriceMonthly { get; set; }
    public bool IsActive { get; set; }
    public int TenantsCount { get; set; }
}

public sealed class CreateOrUpdatePlanRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxCompanies { get; set; }
    public int MaxBranches { get; set; }
    public decimal PriceMonthly { get; set; }
    public bool IsActive { get; set; } = true;
}
