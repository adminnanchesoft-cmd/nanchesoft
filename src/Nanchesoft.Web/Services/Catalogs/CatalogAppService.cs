using System.Globalization;
using System.Text.Json;

namespace Nanchesoft.Web.Services.Catalogs;

public sealed class CatalogAppService
{
    public static CatalogAppService Instance { get; } = new();

    private readonly object _sync = new();
    private readonly Dictionary<string, CatalogStore> _stores;

    private CatalogAppService()
    {
        _stores = BuildStores();
    }

    public Task<CatalogViewDefinition> GetAsync(string catalogKey)
    {
        lock (_sync)
        {
            return Task.FromResult(BuildView(catalogKey));
        }
    }

    public Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        lock (_sync)
        {
            var store = GetStore(catalogKey);

            if (!store.AllowCreate)
            {
                throw new InvalidOperationException("Este catálogo es de solo lectura.");
            }

            var row = CreateEmptyRow(store);
            row[store.KeyExpr] = Guid.NewGuid().ToString("D");
            ApplyPayload(store, row, payload, isInsert: true);
            ApplySystemManagedDefaults(store, row, isInsert: true);

            ValidateRow(store, row);
            ApplyDerivedValues(catalogKey, row);
            store.Rows.Insert(0, row);

            return Task.FromResult(BuildView(catalogKey));
        }
    }

    public Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
    {
        lock (_sync)
        {
            var store = GetStore(catalogKey);

            if (!store.AllowUpdate)
            {
                throw new InvalidOperationException("Este catálogo es de solo lectura.");
            }

            var row = store.Rows.FirstOrDefault(x => GetString(x, store.KeyExpr) == key)
                      ?? throw new InvalidOperationException("No se encontró el registro a editar.");

            ApplyPayload(store, row, payload, isInsert: false);
            ApplySystemManagedDefaults(store, row, isInsert: false);
            ValidateRow(store, row);
            ApplyDerivedValues(catalogKey, row);

            return Task.FromResult(BuildView(catalogKey));
        }
    }

    public Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        lock (_sync)
        {
            var store = GetStore(catalogKey);

            if (!store.AllowDelete)
            {
                throw new InvalidOperationException("Este catálogo no permite eliminar registros.");
            }

            var row = store.Rows.FirstOrDefault(x => GetString(x, store.KeyExpr) == key);

            if (row is null)
            {
                return Task.FromResult(BuildView(catalogKey));
            }

            if (catalogKey.Equals("roles", StringComparison.OrdinalIgnoreCase) && GetBool(row, "IsSystem"))
            {
                throw new InvalidOperationException("No puedes eliminar un rol del sistema.");
            }

            store.Rows.Remove(row);
            return Task.FromResult(BuildView(catalogKey));
        }
    }

    private Dictionary<string, CatalogStore> BuildStores()
    {
        var stores = new Dictionary<string, CatalogStore>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenants"] = BuildTenantsStore(),
            ["companies"] = BuildCompaniesStore(),
            ["branches"] = BuildBranchesStore(),
            ["warehouses"] = BuildWarehousesStore(),
            ["roles"] = BuildRolesStore(),
            ["users"] = BuildUsersStore(),
            ["permissions"] = BuildPermissionsStore(),
            ["sessions"] = BuildSessionsStore(),
            ["accesslogs"] = BuildAccessLogsStore()
        };

        foreach (var store in stores.Values)
        {
            NormalizeStoreMetadata(store);
        }

        return stores;
    }

    private CatalogStore BuildTenantsStore() => new()
    {
        CatalogKey = "tenants",
        Title = "Tenants",
        Subtitle = "Base multiempresa del ecosistema SaaS.",
        KeyExpr = "TenantId",
        AllowCreate = true,
        AllowUpdate = true,
        AllowDelete = true,
        Columns =
        [
            StringCol("TenantId", "Tenant ID", false, 220),
            StringCol("Code", "Código", true, 110),
            StringCol("Name", "Tenant", true, 220),
            StringCol("BrandName", "Branding", true, 180),
            StringCol("Country", "País", true, 120),
            BoolCol("IsActive", "Activo", 90)
        ],
        Rows =
        [
            Row(("TenantId", Guid.NewGuid().ToString("D")), ("Code", "NAN"), ("Name", "Nanchesoft Demo"), ("BrandName", "Nanchesoft ERP"), ("Country", "México"), ("IsActive", true)),
            Row(("TenantId", Guid.NewGuid().ToString("D")), ("Code", "SIL"), ("Name", "Silva Shoes Group"), ("BrandName", "Silva Enterprise"), ("Country", "México"), ("IsActive", true))
        ]
    };

    private CatalogStore BuildCompaniesStore() => new()
    {
        CatalogKey = "companies",
        Title = "Empresas",
        Subtitle = "Administración empresarial por tenant.",
        KeyExpr = "CompanyId",
        AllowCreate = true,
        AllowUpdate = true,
        AllowDelete = true,
        Columns =
        [
            StringCol("CompanyId", "Company ID", false, 220),
            LookupCol("TenantName", "Tenant", "tenants", 200),
            StringCol("Code", "Código", true, 110),
            StringCol("Name", "Empresa", true, 220),
            StringCol("LegalName", "Razón social", true, 220),
            StringCol("Rfc", "RFC", true, 140),
            StringCol("BaseCurrency", "Moneda base", true, 110),
            StringCol("TimeZone", "Zona horaria", true, 170),
            BoolCol("IsActive", "Activo", 90)
        ],
        Rows =
        [
            Row(("CompanyId", Guid.NewGuid().ToString("D")), ("TenantName", "Nanchesoft Demo"), ("Code", "NAN-MX"), ("Name", "Nanchesoft Demo Company"), ("LegalName", "Nanchesoft Demo Company SA de CV"), ("Rfc", "NDE260406ABC"), ("BaseCurrency", "MXN"), ("TimeZone", "America/Mexico_City"), ("IsActive", true)),
            Row(("CompanyId", Guid.NewGuid().ToString("D")), ("TenantName", "Silva Shoes Group"), ("Code", "SIL-MX"), ("Name", "Silva Shoes Group"), ("LegalName", "Silva Shoes Group SA de CV"), ("Rfc", "SSG260406ABC"), ("BaseCurrency", "MXN"), ("TimeZone", "America/Mexico_City"), ("IsActive", true))
        ]
    };

    private CatalogStore BuildBranchesStore() => new()
    {
        CatalogKey = "branches",
        Title = "Sucursales",
        Subtitle = "Estructura operativa por empresa.",
        KeyExpr = "BranchId",
        AllowCreate = true,
        AllowUpdate = true,
        AllowDelete = true,
        Columns =
        [
            StringCol("BranchId", "Branch ID", false, 220),
            LookupCol("CompanyName", "Empresa", "companies", 220, "Name"),
            StringCol("Code", "Código", true, 110),
            StringCol("Name", "Sucursal", true, 220),
            StringCol("City", "Ciudad", true, 140),
            StringCol("State", "Estado", true, 140),
            StringCol("Phone", "Teléfono", false, 150),
            BoolCol("IsActive", "Activo", 90)
        ],
        Rows =
        [
            Row(("BranchId", Guid.NewGuid().ToString("D")), ("CompanyName", "Nanchesoft Demo Company"), ("Code", "MATRIZ"), ("Name", "Matriz"), ("City", "León"), ("State", "Guanajuato"), ("Phone", "4771001000"), ("IsActive", true)),
            Row(("BranchId", Guid.NewGuid().ToString("D")), ("CompanyName", "Silva Shoes Group"), ("Code", "PLANTA1"), ("Name", "Planta 1"), ("City", "León"), ("State", "Guanajuato"), ("Phone", "4772002000"), ("IsActive", true)),
            Row(("BranchId", Guid.NewGuid().ToString("D")), ("CompanyName", "Silva Shoes Group"), ("Code", "CEDIS"), ("Name", "CEDIS Bajío"), ("City", "León"), ("State", "Guanajuato"), ("Phone", "4773003000"), ("IsActive", true))
        ]
    };

    private CatalogStore BuildWarehousesStore() => new()
    {
        CatalogKey = "warehouses",
        Title = "Almacenes",
        Subtitle = "Control maestro de almacenes por sucursal.",
        KeyExpr = "WarehouseId",
        AllowCreate = true,
        AllowUpdate = true,
        AllowDelete = true,
        Columns =
        [
            StringCol("WarehouseId", "Warehouse ID", false, 220),
            LookupCol("CompanyName", "Empresa", "companies", 220, "Name"),
            LookupCol("BranchName", "Sucursal", "branches", 220, "Name"),
            StringCol("Code", "Código", true, 110),
            StringCol("Name", "Almacén", true, 220),
            StringCol("Type", "Tipo", true, 140),
            BoolCol("IsActive", "Activo", 90)
        ],
        Rows =
        [
            Row(("WarehouseId", Guid.NewGuid().ToString("D")), ("CompanyName", "Nanchesoft Demo Company"), ("BranchName", "Matriz"), ("Code", "GEN"), ("Name", "General"), ("Type", "General"), ("IsActive", true)),
            Row(("WarehouseId", Guid.NewGuid().ToString("D")), ("CompanyName", "Silva Shoes Group"), ("BranchName", "Planta 1"), ("Code", "MP-01"), ("Name", "Materia Prima"), ("Type", "Materia prima"), ("IsActive", true)),
            Row(("WarehouseId", Guid.NewGuid().ToString("D")), ("CompanyName", "Silva Shoes Group"), ("BranchName", "CEDIS Bajío"), ("Code", "PT-01"), ("Name", "Producto Terminado"), ("Type", "Terminado"), ("IsActive", true))
        ]
    };

    private CatalogStore BuildRolesStore() => new()
    {
        CatalogKey = "roles",
        Title = "Roles",
        Subtitle = "Control de roles y alcance operativo.",
        KeyExpr = "RoleId",
        AllowCreate = true,
        AllowUpdate = true,
        AllowDelete = true,
        Columns =
        [
            StringCol("RoleId", "Role ID", false, 220),
            StringCol("Code", "Código", true, 120),
            StringCol("Name", "Rol", true, 200),
            StringCol("Scope", "Alcance", true, 140),
            BoolCol("IsSystem", "Sistema", 90, false),
            BoolCol("IsActive", "Activo", 90)
        ],
        Rows =
        [
            Row(("RoleId", Guid.NewGuid().ToString("D")), ("Code", "PLATFORM_OWNER"), ("Name", "Platform Owner"), ("Scope", "Global"), ("IsSystem", true), ("IsActive", true)),
            Row(("RoleId", Guid.NewGuid().ToString("D")), ("Code", "TENANT_ADMIN"), ("Name", "Tenant Admin"), ("Scope", "Tenant"), ("IsSystem", false), ("IsActive", true)),
            Row(("RoleId", Guid.NewGuid().ToString("D")), ("Code", "WAREHOUSE_MANAGER"), ("Name", "Warehouse Manager"), ("Scope", "Sucursal"), ("IsSystem", false), ("IsActive", true)),
            Row(("RoleId", Guid.NewGuid().ToString("D")), ("Code", "READ_ONLY"), ("Name", "Read Only"), ("Scope", "Consulta"), ("IsSystem", false), ("IsActive", true))
        ]
    };

    private CatalogStore BuildUsersStore() => new()
    {
        CatalogKey = "users",
        Title = "Usuarios",
        Subtitle = "Administración de usuarios del ERP.",
        KeyExpr = "UserId",
        AllowCreate = true,
        AllowUpdate = true,
        AllowDelete = true,
        Columns =
        [
            StringCol("UserId", "User ID", false, 220),
            LookupCol("CompanyName", "Empresa", "companies", 220, "Name"),
            StringCol("FullName", "Nombre completo", true, 220),
            StringCol("Email", "Correo", true, 240),
            LookupCol("RoleName", "Rol", "roles", 220, "Name"),
            BoolCol("MustChangePassword", "Cambiar password", 130),
            BoolCol("IsActive", "Activo", 90)
        ],
        Rows =
        [
            Row(("UserId", Guid.NewGuid().ToString("D")), ("CompanyName", "Nanchesoft Demo Company"), ("FullName", "Administrador Nanchesoft"), ("Email", "admin@nanchesoft.local"), ("RoleName", "Platform Owner"), ("MustChangePassword", false), ("IsActive", true)),
            Row(("UserId", Guid.NewGuid().ToString("D")), ("CompanyName", "Silva Shoes Group"), ("FullName", "María Operaciones"), ("Email", "maria@silva.local"), ("RoleName", "Tenant Admin"), ("MustChangePassword", true), ("IsActive", true)),
            Row(("UserId", Guid.NewGuid().ToString("D")), ("CompanyName", "Silva Shoes Group"), ("FullName", "Jefe de Almacén"), ("Email", "almacen@silva.local"), ("RoleName", "Warehouse Manager"), ("MustChangePassword", false), ("IsActive", true))
        ]
    };

    private CatalogStore BuildPermissionsStore() => new()
    {
        CatalogKey = "permissions",
        Title = "Permisos",
        Subtitle = "Consulta de permisos base del sistema.",
        KeyExpr = "PermissionId",
        AllowCreate = false,
        AllowUpdate = false,
        AllowDelete = false,
        Columns =
        [
            StringCol("PermissionId", "Permission ID", false, 220, false),
            StringCol("Code", "Código", true, 250, false),
            StringCol("Module", "Módulo", true, 140, false),
            StringCol("Resource", "Recurso", true, 150, false),
            StringCol("Action", "Acción", true, 140, false),
            BoolCol("IsActive", "Activo", 90, false)
        ],
        Rows =
        [
            Row(("PermissionId", Guid.NewGuid().ToString("D")), ("Code", "organization.company.view"), ("Module", "Organization"), ("Resource", "Company"), ("Action", "View"), ("IsActive", true)),
            Row(("PermissionId", Guid.NewGuid().ToString("D")), ("Code", "organization.branch.edit"), ("Module", "Organization"), ("Resource", "Branch"), ("Action", "Edit"), ("IsActive", true)),
            Row(("PermissionId", Guid.NewGuid().ToString("D")), ("Code", "security.user.create"), ("Module", "Security"), ("Resource", "User"), ("Action", "Create"), ("IsActive", true)),
            Row(("PermissionId", Guid.NewGuid().ToString("D")), ("Code", "administration.session.view"), ("Module", "Administration"), ("Resource", "Session"), ("Action", "View"), ("IsActive", true))
        ]
    };

    private CatalogStore BuildSessionsStore() => new()
    {
        CatalogKey = "sessions",
        Title = "Sesiones activas",
        Subtitle = "Consulta administrativa de sesiones.",
        KeyExpr = "SessionId",
        AllowCreate = false,
        AllowUpdate = false,
        AllowDelete = false,
        Columns =
        [
            StringCol("SessionId", "Session ID", false, 220, false),
            StringCol("UserName", "Usuario", true, 220, false),
            StringCol("CompanyName", "Empresa", true, 220, false),
            StringCol("StartedOn", "Inicio", true, 150, false),
            StringCol("ExpiresOn", "Expira", true, 150, false),
            StringCol("IpAddress", "IP", true, 130, false),
            StringCol("Status", "Estatus", true, 120, false)
        ],
        Rows =
        [
            Row(("SessionId", Guid.NewGuid().ToString("D")), ("UserName", "Administrador Nanchesoft"), ("CompanyName", "Nanchesoft Demo Company"), ("StartedOn", "2026-04-06 08:00"), ("ExpiresOn", "2026-04-06 18:00"), ("IpAddress", "127.0.0.1"), ("Status", "Activa")),
            Row(("SessionId", Guid.NewGuid().ToString("D")), ("UserName", "María Operaciones"), ("CompanyName", "Silva Shoes Group"), ("StartedOn", "2026-04-06 09:10"), ("ExpiresOn", "2026-04-06 19:10"), ("IpAddress", "127.0.0.1"), ("Status", "Activa"))
        ]
    };

    private CatalogStore BuildAccessLogsStore() => new()
    {
        CatalogKey = "accesslogs",
        Title = "Bitácora de acceso",
        Subtitle = "Eventos recientes de login y sesión.",
        KeyExpr = "AccessLogId",
        AllowCreate = false,
        AllowUpdate = false,
        AllowDelete = false,
        Columns =
        [
            StringCol("AccessLogId", "AccessLog ID", false, 220, false),
            StringCol("UserName", "Usuario", true, 220, false),
            StringCol("CompanyName", "Empresa", true, 220, false),
            StringCol("EventType", "Evento", true, 140, false),
            StringCol("Result", "Resultado", true, 120, false),
            StringCol("IpAddress", "IP", true, 130, false),
            StringCol("CreatedOn", "Fecha", true, 150, false)
        ],
        Rows =
        [
            Row(("AccessLogId", Guid.NewGuid().ToString("D")), ("UserName", "Administrador Nanchesoft"), ("CompanyName", "Nanchesoft Demo Company"), ("EventType", "Login"), ("Result", "Success"), ("IpAddress", "127.0.0.1"), ("CreatedOn", "2026-04-06 08:00")),
            Row(("AccessLogId", Guid.NewGuid().ToString("D")), ("UserName", "María Operaciones"), ("CompanyName", "Silva Shoes Group"), ("EventType", "RefreshToken"), ("Result", "Success"), ("IpAddress", "127.0.0.1"), ("CreatedOn", "2026-04-06 09:30")),
            Row(("AccessLogId", Guid.NewGuid().ToString("D")), ("UserName", "Usuario demo"), ("CompanyName", "Silva Shoes Group"), ("EventType", "Login"), ("Result", "Failed"), ("IpAddress", "127.0.0.1"), ("CreatedOn", "2026-04-06 10:15"))
        ]
    };

    private CatalogViewDefinition BuildView(string catalogKey)
    {
        var store = GetStore(catalogKey);
        var columns = store.Columns.Select(CloneColumn).ToList();

        foreach (var column in columns.Where(x => x.UseLookup && !string.IsNullOrWhiteSpace(x.LookupCatalogKey)))
        {
            var lookupStore = GetStore(column.LookupCatalogKey!);
            var lookupValueField = column.LookupValueField ?? lookupStore.KeyExpr;
            var lookupDisplayField = column.LookupDisplayField ?? lookupStore.KeyExpr;

            column.LookupItems = lookupStore.Rows
                .Select(x => new CatalogLookupItem
                {
                    Id = GetString(x, lookupValueField),
                    Name = GetString(x, lookupDisplayField)
                })
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var rows = store.Rows.Select(CloneRow).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = store.CatalogKey,
            Title = store.Title,
            Subtitle = store.Subtitle,
            KeyExpr = store.KeyExpr,
            AllowCreate = store.AllowCreate,
            AllowUpdate = store.AllowUpdate,
            AllowDelete = store.AllowDelete,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => !HasField(store, "IsActive") || GetBool(x, "IsActive")),
            InactiveCount = rows.Count(x => HasField(store, "IsActive") && !GetBool(x, "IsActive")),
            Columns = columns,
            Rows = rows
        };
    }

    private CatalogStore GetStore(string catalogKey)
    {
        if (!_stores.TryGetValue(catalogKey, out var store))
        {
            throw new InvalidOperationException($"No existe el catálogo '{catalogKey}'.");
        }

        return store;
    }

    private static CatalogColumnDefinition StringCol(string field, string caption, bool required, int width, bool allowEditing = true)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "string",
            Required = required,
            Width = width,
            AllowEditing = allowEditing
        };

    private static CatalogColumnDefinition BoolCol(string field, string caption, int width, bool allowEditing = true)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "boolean",
            Width = width,
            AllowEditing = allowEditing
        };

    private static CatalogColumnDefinition LookupCol(string field, string caption, string lookupCatalogKey, int width, string lookupDisplayField = "Name")
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "string",
            Required = true,
            Width = width,
            UseLookup = true,
            LookupCatalogKey = lookupCatalogKey,
            LookupValueField = lookupDisplayField,
            LookupDisplayField = lookupDisplayField
        };

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] items)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            row[item.Key] = item.Value;
        }
        return row;
    }

    private static void NormalizeStoreMetadata(CatalogStore store)
    {
        foreach (var column in store.Columns)
        {
            if (IsHiddenSystemField(store, column.DataField))
            {
                column.Visible = false;
                column.AllowFiltering = false;
                column.AllowSorting = false;
            }

            if (IsSystemManagedField(store, column.DataField))
            {
                column.AllowEditing = false;
                column.Required = false;
            }
        }
    }

    private static bool IsHiddenSystemField(CatalogStore store, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        if (fieldName.Equals(store.KeyExpr, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("TenantId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("CompanyId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("BranchId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("WarehouseId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("UserId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("RoleId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("PermissionId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("SessionId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("AccessLogId", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("RowVersion", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("ConcurrencyStamp", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("PasswordHash", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSystemManagedField(CatalogStore store, string fieldName)
    {
        if (IsHiddenSystemField(store, fieldName))
        {
            return true;
        }

        return fieldName.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("CreatedOn", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("UpdatedOn", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("UpdatedBy", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyPayload(CatalogStore store, Dictionary<string, object?> row, JsonElement payload, bool isInsert)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in payload.EnumerateObject())
        {
            if (IsSystemManagedField(store, property.Name))
            {
                continue;
            }

            var column = store.Columns.FirstOrDefault(x => x.DataField.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
            if (column is null)
            {
                continue;
            }

            if (!column.AllowEditing)
            {
                continue;
            }

            row[property.Name] = ConvertJsonValue(property.Value);
        }
    }

    private static void ApplySystemManagedDefaults(CatalogStore store, Dictionary<string, object?> row, bool isInsert)
    {
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");

        row[store.KeyExpr] ??= Guid.NewGuid().ToString("D");

        if (HasField(store, "IsActive") && (!row.TryGetValue("IsActive", out var isActive) || isActive is null))
        {
            row["IsActive"] = true;
        }

        if (isInsert)
        {
            if (HasField(store, "CreatedOn"))
            {
                row["CreatedOn"] = now;
            }

            if (HasField(store, "CreatedAt"))
            {
                row["CreatedAt"] = now;
            }

            if (HasField(store, "CreatedBy") && string.IsNullOrWhiteSpace(GetString(row, "CreatedBy")))
            {
                row["CreatedBy"] = "system";
            }
        }

        if (HasField(store, "UpdatedOn"))
        {
            row["UpdatedOn"] = now;
        }

        if (HasField(store, "UpdatedAt"))
        {
            row["UpdatedAt"] = now;
        }

        if (HasField(store, "UpdatedBy"))
        {
            row["UpdatedBy"] = "system";
        }
    }

    private static Dictionary<string, object?> CreateEmptyRow(CatalogStore store)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in store.Columns)
        {
            row[column.DataField] = column.DataType.Equals("boolean", StringComparison.OrdinalIgnoreCase) ? false : null;
        }
        return row;
    }

    private static bool HasField(CatalogStore store, string fieldName)
        => store.Columns.Any(x => x.DataField.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private static void ValidateRow(CatalogStore store, Dictionary<string, object?> row)
    {
        foreach (var column in store.Columns.Where(x => x.Required))
        {
            if (!row.TryGetValue(column.DataField, out var value) || value is null)
            {
                throw new InvalidOperationException($"El campo '{column.Caption}' es obligatorio.");
            }

            if (value is string text && string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException($"El campo '{column.Caption}' es obligatorio.");
            }
        }
    }

    private static void ApplyDerivedValues(string catalogKey, Dictionary<string, object?> row)
    {
        if (catalogKey.Equals("users", StringComparison.OrdinalIgnoreCase))
        {
            row["Email"] = GetString(row, "Email").Trim().ToLowerInvariant();
        }

        if (row.ContainsKey("Code"))
        {
            row["Code"] = GetString(row, "Code").Trim().ToUpperInvariant();
        }
    }

    private static CatalogColumnDefinition CloneColumn(CatalogColumnDefinition source)
        => new()
        {
            DataField = source.DataField,
            Caption = source.Caption,
            DataType = source.DataType,
            Required = source.Required,
            AllowEditing = source.AllowEditing,
            AllowFiltering = source.AllowFiltering,
            AllowSorting = source.AllowSorting,
            Visible = source.Visible,
            Width = source.Width,
            UseLookup = source.UseLookup,
            LookupCatalogKey = source.LookupCatalogKey,
            LookupValueField = source.LookupValueField,
            LookupDisplayField = source.LookupDisplayField,
            LookupItems = source.LookupItems.Select(x => new CatalogLookupItem { Id = x.Id, Name = x.Name }).ToList()
        };

    private static Dictionary<string, object?> CloneRow(Dictionary<string, object?> source)
    {
        var target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
        {
            target[item.Key] = item.Value;
        }
        return target;
    }

    private static string GetString(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static bool GetBool(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => false
        };
    }

    private sealed class CatalogStore
    {
        public string CatalogKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string KeyExpr { get; set; } = string.Empty;
        public bool AllowCreate { get; set; }
        public bool AllowUpdate { get; set; }
        public bool AllowDelete { get; set; }
        public List<CatalogColumnDefinition> Columns { get; set; } = [];
        public List<Dictionary<string, object?>> Rows { get; set; } = [];
    }
}

public sealed class CatalogViewDefinition
{
    public string CatalogKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string KeyExpr { get; set; } = string.Empty;
    public bool AllowCreate { get; set; }
    public bool AllowUpdate { get; set; }
    public bool AllowDelete { get; set; }
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public List<CatalogColumnDefinition> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

public sealed class CatalogColumnDefinition
{
    public string DataField { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public bool Required { get; set; }
    public bool AllowEditing { get; set; } = true;
    public bool AllowFiltering { get; set; } = true;
    public bool AllowSorting { get; set; } = true;
    public bool Visible { get; set; } = true;
    public int Width { get; set; } = 160;
    public bool UseLookup { get; set; }
    public string? LookupCatalogKey { get; set; }
    public string? LookupValueField { get; set; }
    public string? LookupDisplayField { get; set; }
    public List<CatalogLookupItem> LookupItems { get; set; } = [];
}

public sealed class CatalogLookupItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
