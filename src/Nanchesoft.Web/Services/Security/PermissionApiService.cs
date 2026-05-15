using System.Text.Json;
using System.Net.Http.Json;
using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.Security;

public sealed class PermissionApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PermissionApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<CatalogViewDefinition> GetCatalogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var permissions = await client.GetFromJsonAsync<List<PermissionRowDto>>("/api/security/permissions") ?? [];

        var columns = new List<CatalogColumnDefinition>
        {
            new()
            {
                DataField = "PermissionId",
                Caption = "Permission ID",
                DataType = "string",
                AllowEditing = false,
                Width = 220,
                Visible = false
            },
            new()
            {
                DataField = "Code",
                Caption = "Código",
                DataType = "string",
                AllowEditing = false,
                Width = 260
            },
            new()
            {
                DataField = "Module",
                Caption = "Módulo",
                DataType = "string",
                AllowEditing = false,
                Width = 140
            },
            new()
            {
                DataField = "Resource",
                Caption = "Recurso",
                DataType = "string",
                AllowEditing = false,
                Width = 150
            },
            new()
            {
                DataField = "Action",
                Caption = "Acción",
                DataType = "string",
                AllowEditing = false,
                Width = 120
            },
            new()
            {
                DataField = "Name",
                Caption = "Nombre",
                DataType = "string",
                AllowEditing = false,
                Width = 220
            },
            new()
            {
                DataField = "IsActive",
                Caption = "Activo",
                DataType = "boolean",
                AllowEditing = false,
                Width = 90
            }
        };

        var rows = permissions.Select(x => new Dictionary<string, object?>
        {
            ["PermissionId"] = x.PermissionId.ToString("D"),
            ["Code"] = x.Code,
            ["Module"] = x.Module,
            ["Resource"] = x.Resource,
            ["Action"] = x.Action,
            ["Name"] = x.Name,
            ["IsActive"] = x.IsActive
        }).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = "permissions",
            Title = "Permisos",
            Subtitle = "Consulta real de permisos desde API + PostgreSQL.",
            KeyExpr = "PermissionId",
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

    public Task<CatalogViewDefinition> InsertAsync(JsonElement payload)
        => throw new InvalidOperationException("El catálogo de permisos es solo lectura.");

    public Task<CatalogViewDefinition> UpdateAsync(string key, JsonElement payload)
        => throw new InvalidOperationException("El catálogo de permisos es solo lectura.");

    public Task<CatalogViewDefinition> DeleteAsync(string key)
        => throw new InvalidOperationException("El catálogo de permisos es solo lectura.");
}

public sealed class PermissionRowDto
{
    public Guid PermissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
