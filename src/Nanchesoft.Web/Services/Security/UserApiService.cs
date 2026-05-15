using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Security;

public sealed class UserApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public UserApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    public async Task<CatalogViewDefinition> GetCatalogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var usersTask = client.GetFromJsonAsync<List<UserRowDto>>("/api/security/users");
        var tenantsTask = client.GetFromJsonAsync<List<UserTenantLookupDto>>("/api/security/users/tenants");
        var rolesTask = client.GetFromJsonAsync<List<UserRoleLookupDto>>("/api/security/users/roles");
        await Task.WhenAll(usersTask!, tenantsTask!, rolesTask!);

        var users = FilterUsers(usersTask.Result ?? []);
        var tenants = FilterTenants(tenantsTask.Result ?? []);
        var roles = FilterRoles(rolesTask.Result ?? []);
        var showTenantColumn = _authState.IsPlatformOwner;

        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "UserId", Caption = "User ID", DataType = "string", AllowEditing = false, Width = 220, Visible = false },
            new() { DataField = "TenantId", Caption = "Tenant", DataType = "string", Required = showTenantColumn, Visible = showTenantColumn, Width = 220, UseLookup = true, LookupItems = tenants.Select(x => new CatalogLookupItem { Id = x.TenantId.ToString("D"), Name = x.TenantName }).ToList() },
            new() { DataField = "Username", Caption = "Usuario", DataType = "string", Required = true, Width = 140 },
            new() { DataField = "Email", Caption = "Correo", DataType = "string", Required = true, Width = 220 },
            new() { DataField = "FirstName", Caption = "Nombre", DataType = "string", Required = true, Width = 160 },
            new() { DataField = "LastName", Caption = "Apellidos", DataType = "string", Required = true, Width = 180 },
            new() { DataField = "Phone", Caption = "Teléfono", DataType = "string", Width = 150 },
            new() { DataField = "RoleId", Caption = "Rol principal", DataType = "string", Width = 220, UseLookup = true, LookupItems = roles.Select(x => new CatalogLookupItem { Id = x.RoleId.ToString("D"), Name = x.RoleName }).ToList() },
            new() { DataField = "MustChangePassword", Caption = "Cambiar password", DataType = "boolean", Width = 135 },
            new() { DataField = "IsLocked", Caption = "Bloqueado", DataType = "boolean", Width = 110 },
            new() { DataField = "IsActive", Caption = "Activo", DataType = "boolean", Width = 90 },
            new() { DataField = "LastLoginAt", Caption = "Último acceso", DataType = "datetime", Width = 180, AllowEditing = false }
        };

        var rows = users.Select(x => new Dictionary<string, object?>
        {
            ["UserId"] = x.UserId.ToString("D"),
            ["TenantId"] = x.TenantId.ToString("D"),
            ["Username"] = x.Username,
            ["Email"] = x.Email,
            ["FirstName"] = x.FirstName,
            ["LastName"] = x.LastName,
            ["Phone"] = x.Phone,
            ["RoleId"] = x.RoleId?.ToString("D"),
            ["MustChangePassword"] = x.MustChangePassword,
            ["IsLocked"] = x.IsLocked,
            ["IsActive"] = x.IsActive,
            ["LastLoginAt"] = x.LastLoginAt
        }).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = "users",
            Title = "Usuarios",
            Subtitle = _authState.IsPlatformOwner ? "Administración real de usuarios desde API + PostgreSQL." : $"Usuarios del tenant activo: {_appState.CurrentTenantName}.",
            KeyExpr = "UserId",
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
        await ValidateRoleBelongsToTenantAsync(request);
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsJsonAsync("/api/security/users", request);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string key, JsonElement payload)
    {
        var request = MapRequest(payload);
        await ValidateRoleBelongsToTenantAsync(request);
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PutAsJsonAsync($"/api/security/users/{key}", request);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string key)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.DeleteAsync($"/api/security/users/{key}");
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync();
    }

    /// <summary>
    /// Verifica en frontend que el RoleId seleccionado pertenezca al Tenant seleccionado,
    /// para evitar el error "No se encontró el rol enviado para el tenant seleccionado"
    /// que se da cuando el dropdown muestra roles de varios tenants y el usuario escoge mal.
    /// </summary>
    private async Task ValidateRoleBelongsToTenantAsync(CreateOrUpdateUserRequest request)
    {
        if (!request.RoleId.HasValue) return;

        // Si no es Platform Owner, el TenantId lo forzamos al activo más adelante.
        // Pero igual validamos contra el tenant que se va a usar.
        var effectiveTenantId = _authState.IsPlatformOwner
            ? request.TenantId
            : _appState.CurrentTenantId;

        if (!effectiveTenantId.HasValue) return;

        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var roles = await client.GetFromJsonAsync<List<UserRoleLookupDto>>("/api/security/users/roles") ?? [];
        var role = roles.FirstOrDefault(r => r.RoleId == request.RoleId.Value);

        if (role is null)
            throw new InvalidOperationException("El rol seleccionado ya no existe. Recarga la pantalla y vuelve a intentarlo.");

        if (role.TenantId != effectiveTenantId.Value)
        {
            var tenantName = string.IsNullOrWhiteSpace(role.TenantName) ? "otro tenant" : role.TenantName;
            throw new InvalidOperationException(
                $"El rol \"{role.RoleName}\" pertenece a {tenantName}, no al tenant seleccionado. " +
                $"Cambia el rol o el tenant para que coincidan.");
        }
    }

    private CreateOrUpdateUserRequest MapRequest(JsonElement payload)
    {
        var request = new CreateOrUpdateUserRequest
        {
            TenantId = ReadGuid(payload, "TenantId"),
            RoleId = ReadGuid(payload, "RoleId"),
            Username = ReadString(payload, "Username"),
            Email = ReadString(payload, "Email"),
            FirstName = ReadString(payload, "FirstName"),
            LastName = ReadString(payload, "LastName"),
            Phone = ReadNullableString(payload, "Phone"),
            MustChangePassword = ReadBool(payload, "MustChangePassword", true),
            IsLocked = ReadBool(payload, "IsLocked", false),
            IsActive = ReadBool(payload, "IsActive", true)
        };
        if (!_authState.IsPlatformOwner && _appState.CurrentTenantId.HasValue)
            request.TenantId = _appState.CurrentTenantId;
        return request;
    }

    private List<UserRowDto> FilterUsers(List<UserRowDto> rows)
    {
        if (_authState.IsPlatformOwner || !_appState.CurrentTenantId.HasValue)
            return rows.OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ToList();
        return rows.Where(x => x.TenantId == _appState.CurrentTenantId.Value)
                   .OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<UserTenantLookupDto> FilterTenants(List<UserTenantLookupDto> rows)
    {
        if (_authState.IsPlatformOwner || !_appState.CurrentTenantId.HasValue)
            return rows.OrderBy(x => x.TenantName, StringComparer.OrdinalIgnoreCase).ToList();
        return rows.Where(x => x.TenantId == _appState.CurrentTenantId.Value)
                   .OrderBy(x => x.TenantName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<UserRoleLookupDto> FilterRoles(List<UserRoleLookupDto> rows)
    {
        IEnumerable<UserRoleLookupDto> filtered = rows;

        // Si NO eres platform owner, sólo ves los roles de tu tenant.
        if (!_authState.IsPlatformOwner && _appState.CurrentTenantId.HasValue)
            filtered = rows.Where(x => x.TenantId == _appState.CurrentTenantId.Value);

        // Quitar duplicados exactos (mismo RoleId).
        filtered = filtered
            .GroupBy(x => x.RoleId)
            .Select(g => g.First());

        // Si eres platform owner: detectar nombres repetidos entre tenants
        // y agregar el nombre del tenant para distinguirlos visualmente.
        if (_authState.IsPlatformOwner)
        {
            var grouped = filtered.ToList();
            var nameCounts = grouped
                .GroupBy(x => x.RoleName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var role in grouped)
            {
                if (nameCounts.TryGetValue(role.RoleName, out var count) && count > 1
                    && !string.IsNullOrWhiteSpace(role.TenantName))
                {
                    role.RoleName = $"{role.RoleName} · {role.TenantName}";
                }
            }

            return grouped
                .OrderBy(x => x.TenantName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.RoleName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return filtered
            .OrderBy(x => x.RoleName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ReadString(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;
        return string.Empty;
    }

    private static string? ReadNullableString(JsonElement payload, string name)
    {
        var value = ReadString(payload, name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
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

public sealed class UserRowDto
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public bool IsLocked { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public sealed class UserTenantLookupDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class UserRoleLookupDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class CreateOrUpdateUserRequest
{
    public Guid? TenantId { get; set; }
    public Guid? RoleId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public bool IsLocked { get; set; }
    public bool IsActive { get; set; } = true;
}
