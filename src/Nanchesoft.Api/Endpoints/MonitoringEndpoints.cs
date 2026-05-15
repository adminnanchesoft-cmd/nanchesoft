using System.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class MonitoringEndpoints
{
    public static IEndpointRouteBuilder MapMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/monitoring").WithTags("Monitoring");

        group.MapGet("/errors", async (NanchesoftDbContext db) =>
            Results.Ok(await db.ErrorLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .Select(x => new MonitoringErrorRowDto
                {
                    ErrorLogId = x.Id,
                    CreatedAt = x.CreatedAt,
                    Source = x.Source,
                    Message = x.Message,
                    RequestPath = x.RequestPath,
                    CompanyId = x.CompanyId,
                    UserId = x.UserId,
                    StackTrace = x.StackTrace
                })
                .ToListAsync()));

        group.MapGet("/security-review", async (NanchesoftDbContext db) =>
        {
            var totalUsers = await db.Users.CountAsync();
            var activeUsers = await db.Users.CountAsync(x => x.IsActive);
            var lockedUsers = await db.Users.CountAsync(x => x.IsLocked);
            var inactiveUsers = await db.Users.CountAsync(x => !x.IsActive);
            var activeSessions = await db.UserSessions.CountAsync(x => x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow);
            var totalRoles = await db.Roles.CountAsync();
            var totalPermissions = await db.Permissions.CountAsync();
            var recentAccessFailures = await db.AccessLogs.CountAsync(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-7) && x.EventResult != "success");

            return Results.Ok(new MonitoringSecurityReviewDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                LockedUsers = lockedUsers,
                ActiveSessions = activeSessions,
                TotalRoles = totalRoles,
                TotalPermissions = totalPermissions,
                RecentAccessFailures = recentAccessFailures
            });
        });

        group.MapGet("/health", async (NanchesoftDbContext db) =>
        {
            var lastPurchase = await db.PurchaseInvoices.AsNoTracking().OrderByDescending(x => x.InvoiceDate).Select(x => (DateTime?)x.InvoiceDate).FirstOrDefaultAsync();
            var lastSale = await db.SalesInvoices.AsNoTracking().OrderByDescending(x => x.InvoiceDate).Select(x => (DateTime?)x.InvoiceDate).FirstOrDefaultAsync();
            var lastInventoryMove = await db.InventoryMovements.AsNoTracking().OrderByDescending(x => x.MovementDate).Select(x => (DateTime?)x.MovementDate).FirstOrDefaultAsync();
            var lastTreasuryMove = await db.BankMovements.AsNoTracking().OrderByDescending(x => x.MovementDate).Select(x => (DateTime?)x.MovementDate).FirstOrDefaultAsync();

            return Results.Ok(new MonitoringHealthDto
            {
                Companies = await db.Companies.CountAsync(),
                Branches = await db.Branches.CountAsync(),
                Warehouses = await db.Warehouses.CountAsync(),
                Customers = await db.Customers.CountAsync(),
                Suppliers = await db.Suppliers.CountAsync(),
                Items = await db.Items.CountAsync(),
                PurchaseInvoices = await db.PurchaseInvoices.CountAsync(),
                SalesInvoices = await db.SalesInvoices.CountAsync(),
                InventoryMovements = await db.InventoryMovements.CountAsync(),
                CashAccounts = await db.CashAccounts.CountAsync(),
                BankAccounts = await db.BankAccounts.CountAsync(),
                LastPurchaseDate = lastPurchase,
                LastSalesDate = lastSale,
                LastInventoryMovementDate = lastInventoryMove,
                LastTreasuryMovementDate = lastTreasuryMove,
                LastErrorDate = await db.ErrorLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync()
            });
        });

        group.MapGet("/schema-catalog", async (NanchesoftDbContext db) =>
        {
            var modelTables = GetModelTables(db).ToList();
            var databaseTables = await GetDatabaseTablesAsync(db);
            var rowEstimates = await GetRowEstimatesAsync(db);

            var rows = modelTables
                .Select(model =>
                {
                    var key = GetKey(model.SchemaName, model.TableName);
                    var databaseTable = databaseTables.TryGetValue(key, out var table) ? table : null;

                    return new MonitoringSchemaCatalogRowDto
                    {
                        SchemaName = model.SchemaName,
                        TableName = model.TableName,
                        ClrType = model.ClrType,
                        PrimaryKey = model.PrimaryKey,
                        ModuleName = GetModuleName(model.SchemaName),
                        ExistsInDatabase = databaseTable is not null,
                        EstimatedRows = rowEstimates.TryGetValue(key, out var rows) ? rows : 0,
                        DatabaseComment = databaseTable?.Comment,
                        IsOwnedEntity = model.IsOwnedEntity
                    };
                })
                .OrderBy(x => x.SchemaName)
                .ThenBy(x => x.TableName)
                .ToList();

            return Results.Ok(rows);
        });

        group.MapGet("/schema-summary", async (NanchesoftDbContext db) =>
        {
            var modelTables = GetModelTables(db).ToList();
            var databaseTables = await GetDatabaseTablesAsync(db);
            var rowEstimates = await GetRowEstimatesAsync(db);

            var summaries = modelTables
                .GroupBy(x => x.SchemaName)
                .Select(grouped =>
                {
                    var schema = grouped.Key;
                    var modelTableCount = grouped.Count();
                    var physicalTableCount = grouped.Count(model => databaseTables.ContainsKey(GetKey(model.SchemaName, model.TableName)));
                    var estimatedRows = grouped.Sum(model => rowEstimates.TryGetValue(GetKey(model.SchemaName, model.TableName), out var count) ? count : 0L);

                    return new MonitoringSchemaSummaryDto
                    {
                        SchemaName = schema,
                        ModuleName = GetModuleName(schema),
                        ModelTableCount = modelTableCount,
                        PhysicalTableCount = physicalTableCount,
                        MissingTableCount = modelTableCount - physicalTableCount,
                        EstimatedRows = estimatedRows
                    };
                })
                .OrderBy(x => x.SchemaName)
                .ToList();

            return Results.Ok(summaries);
        });

        group.MapGet("/schema-gaps", async (NanchesoftDbContext db) =>
        {
            var modelTables = GetModelTables(db).ToList();
            var databaseTables = await GetDatabaseTablesAsync(db);

            var modelKeys = modelTables.Select(x => GetKey(x.SchemaName, x.TableName)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingInDatabase = modelTables
                .Where(model => !databaseTables.ContainsKey(GetKey(model.SchemaName, model.TableName)))
                .Select(model => new MonitoringSchemaGapDto
                {
                    GapType = "MissingInDatabase",
                    SchemaName = model.SchemaName,
                    TableName = model.TableName,
                    ModuleName = GetModuleName(model.SchemaName),
                    Details = $"La entidad {model.ClrType} existe en EF pero no en PostgreSQL."
                })
                .ToList();

            var orphanTables = databaseTables.Values
                .Where(table => !modelKeys.Contains(GetKey(table.SchemaName, table.TableName)))
                .Select(table => new MonitoringSchemaGapDto
                {
                    GapType = "OrphanInDatabase",
                    SchemaName = table.SchemaName,
                    TableName = table.TableName,
                    ModuleName = GetModuleName(table.SchemaName),
                    Details = "La tabla existe en PostgreSQL pero no está mapeada en el modelo actual."
                })
                .ToList();

            return Results.Ok(missingInDatabase.Concat(orphanTables)
                .OrderBy(x => x.GapType)
                .ThenBy(x => x.SchemaName)
                .ThenBy(x => x.TableName)
                .ToList());
        });

        return app;
    }

    private static IEnumerable<ModelTableInfo> GetModelTables(NanchesoftDbContext db)
    {
        return db.Model.GetEntityTypes()
            .Where(entity => entity.FindPrimaryKey() is not null)
            .Where(entity => !entity.IsOwned())
            .Select(entity => new ModelTableInfo
            {
                SchemaName = entity.GetSchema() ?? "public",
                TableName = entity.GetTableName() ?? entity.ClrType.Name,
                ClrType = entity.ClrType.Name,
                PrimaryKey = string.Join(", ", entity.FindPrimaryKey()!.Properties.Select(x => x.Name)),
                IsOwnedEntity = entity.IsOwned()
            });
    }

    private static async Task<Dictionary<string, DatabaseTableInfo>> GetDatabaseTablesAsync(NanchesoftDbContext db)
    {
        const string sql = @"
            select table_schema, table_name
            from information_schema.tables
            where table_type = 'BASE TABLE'
              and table_schema not in ('pg_catalog', 'information_schema')
            order by table_schema, table_name;";

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new Dictionary<string, DatabaseTableInfo>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var info = new DatabaseTableInfo
            {
                SchemaName = schema,
                TableName = table,
                Comment = null
            };

            result[GetKey(schema, table)] = info;
        }

        return result;
    }

    private static async Task<Dictionary<string, long>> GetRowEstimatesAsync(NanchesoftDbContext db)
    {
        const string sql = @"
            select n.nspname as schema_name,
                   c.relname as table_name,
                   cast(coalesce(c.reltuples, 0) as bigint) as estimated_rows
            from pg_class c
            inner join pg_namespace n on n.oid = c.relnamespace
            where c.relkind = 'r'
              and n.nspname not in ('pg_catalog', 'information_schema')
            order by n.nspname, c.relname;";

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var estimatedRows = reader.IsDBNull(2) ? 0L : reader.GetInt64(2);
            result[GetKey(schema, table)] = estimatedRows;
        }

        return result;
    }

    private static string GetKey(string schemaName, string tableName) => $"{schemaName}.{tableName}";

    private static string GetModuleName(string schemaName) => schemaName.ToLowerInvariant() switch
    {
        "core" => "Plataforma",
        "subscription" => "Suscripción",
        "auth" => "Seguridad",
        "config" => "Configuración",
        "catalog" => "Catálogos",
        "org" => "Organización",
        "product" => "Productos",
        "hr" => "Recursos humanos",
        "payroll" => "Nómina",
        "purchase" => "Compras",
        "inventory" => "Inventario",
        "sales" => "Ventas",
        "finance" => "Tesorería y finanzas",
        "accounting" => "Contabilidad",
        _ => schemaName
    };

    private sealed class ModelTableInfo
    {
        public string SchemaName { get; init; } = string.Empty;
        public string TableName { get; init; } = string.Empty;
        public string ClrType { get; init; } = string.Empty;
        public string PrimaryKey { get; init; } = string.Empty;
        public bool IsOwnedEntity { get; init; }
    }

    private sealed class DatabaseTableInfo
    {
        public string SchemaName { get; init; } = string.Empty;
        public string TableName { get; init; } = string.Empty;
        public string? Comment { get; init; }
    }
}

public sealed class MonitoringErrorRowDto
{
    public Guid ErrorLogId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RequestPath { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string? StackTrace { get; set; }
}

public sealed class MonitoringSecurityReviewDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalRoles { get; set; }
    public int TotalPermissions { get; set; }
    public int RecentAccessFailures { get; set; }
}

public sealed class MonitoringHealthDto
{
    public int Companies { get; set; }
    public int Branches { get; set; }
    public int Warehouses { get; set; }
    public int Customers { get; set; }
    public int Suppliers { get; set; }
    public int Items { get; set; }
    public int PurchaseInvoices { get; set; }
    public int SalesInvoices { get; set; }
    public int InventoryMovements { get; set; }
    public int CashAccounts { get; set; }
    public int BankAccounts { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public DateTime? LastSalesDate { get; set; }
    public DateTime? LastInventoryMovementDate { get; set; }
    public DateTime? LastTreasuryMovementDate { get; set; }
    public DateTime? LastErrorDate { get; set; }
}

public sealed class MonitoringSchemaCatalogRowDto
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ClrType { get; set; } = string.Empty;
    public string PrimaryKey { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public bool ExistsInDatabase { get; set; }
    public long EstimatedRows { get; set; }
    public string? DatabaseComment { get; set; }
    public bool IsOwnedEntity { get; set; }
}

public sealed class MonitoringSchemaSummaryDto
{
    public string SchemaName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int ModelTableCount { get; set; }
    public int PhysicalTableCount { get; set; }
    public int MissingTableCount { get; set; }
    public long EstimatedRows { get; set; }
}

public sealed class MonitoringSchemaGapDto
{
    public string GapType { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
