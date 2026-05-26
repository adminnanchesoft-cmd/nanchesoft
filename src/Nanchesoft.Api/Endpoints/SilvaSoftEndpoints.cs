using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class SilvaSoftEndpoints
{
    public static IEndpointRouteBuilder MapSilvaSoftEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/silvasoft").WithTags("SilvaSoft");
        g.MapGet("/config", GetConfigAsync);
        g.MapPost("/config", SaveConfigAsync);
        g.MapGet("/test-connection", TestConnectionAsync);
        g.MapGet("/families", GetFamiliesFromSilvaSoftAsync);
        g.MapPost("/families/import", ImportFamiliesAsync);
        g.MapGet("/composition", GetCompositionFromSilvaSoftAsync);
        g.MapPost("/composition/import", ImportCompositionAsync);
        g.MapGet("/logs", GetLogsAsync);
        return app;
    }

    // ── GET /api/silvasoft/config ───────────────────────────────────
    private static async Task<IResult> GetConfigAsync(HttpContext ctx, NanchesoftDbContext db)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var cfg = await db.SilvaSoftConfigs
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value)
            .Select(x => new SilvaSoftConfigDto
            {
                Id = x.Id,
                ServerHost = x.ServerHost,
                DatabaseName = x.DatabaseName,
                DbUser = x.DbUser,
                Port = x.Port,
                TrustServerCertificate = x.TrustServerCertificate,
                Notes = x.Notes,
                LastSyncAt = x.LastSyncAt,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();

        return cfg is null ? Results.NotFound(new { message = "Sin configuración SilvaSoft para esta empresa." }) : Results.Ok(cfg);
    }

    // ── POST /api/silvasoft/config ──────────────────────────────────
    private static async Task<IResult> SaveConfigAsync(HttpContext ctx, SilvaSoftConfigRequest req, NanchesoftDbContext db)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(ctx);
        var tenantId  = ApiTenantScope.ResolveTenantId(ctx);
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        if (string.IsNullOrWhiteSpace(req.ServerHost))
            return Results.BadRequest(new { message = "ServerHost es obligatorio." });
        if (string.IsNullOrWhiteSpace(req.DatabaseName))
            return Results.BadRequest(new { message = "DatabaseName es obligatorio." });

        if (!tenantId.HasValue)
            tenantId = await db.Companies.Where(x => x.Id == companyId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefaultAsync();

        var existing = await db.SilvaSoftConfigs.FirstOrDefaultAsync(x => x.CompanyId == companyId.Value);
        if (existing is not null)
        {
            existing.ServerHost = req.ServerHost.Trim();
            existing.DatabaseName = req.DatabaseName.Trim();
            existing.DbUser = req.DbUser?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(req.DbPassword))
                existing.DbPassword = req.DbPassword.Trim();
            existing.Port = req.Port <= 0 ? 1433 : req.Port;
            existing.TrustServerCertificate = req.TrustServerCertificate;
            existing.Notes = req.Notes?.Trim();
            existing.IsActive = req.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = "web-api";
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        }

        db.SilvaSoftConfigs.Add(new SilvaSoftConfig
        {
            TenantId = tenantId!.Value,
            CompanyId = companyId.Value,
            ServerHost = req.ServerHost.Trim(),
            DatabaseName = req.DatabaseName.Trim(),
            DbUser = req.DbUser?.Trim() ?? string.Empty,
            DbPassword = req.DbPassword?.Trim() ?? string.Empty,
            Port = req.Port <= 0 ? 1433 : req.Port,
            TrustServerCertificate = req.TrustServerCertificate,
            Notes = req.Notes?.Trim(),
            IsActive = req.IsActive,
            CreatedBy = "web-api"
        });
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    // ── GET /api/silvasoft/test-connection ─────────────────────────
    private static async Task<IResult> TestConnectionAsync(HttpContext ctx, NanchesoftDbContext db)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var cfg = await db.SilvaSoftConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId.Value);
        if (cfg is null)
            return Results.NotFound(new { message = "Sin configuración SilvaSoft." });

        try
        {
            await using var conn = BuildConnection(cfg);
            await conn.OpenAsync();
            return Results.Ok(new { success = true, message = "Conexión exitosa a SilvaSoft SQL Server." });
        }
        catch (Exception ex)
        {
            return Results.Ok(new { success = false, message = ex.Message });
        }
    }

    // ── GET /api/silvasoft/families ─────────────────────────────────
    private static async Task<IResult> GetFamiliesFromSilvaSoftAsync(HttpContext ctx, NanchesoftDbContext db)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var cfg = await db.SilvaSoftConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId.Value);
        if (cfg is null)
            return Results.NotFound(new { message = "Sin configuración SilvaSoft." });

        try
        {
            var rows = await QuerySilvaSoftFamiliesAsync(cfg);
            return Results.Ok(rows);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    // ── POST /api/silvasoft/families/import ─────────────────────────
    private static async Task<IResult> ImportFamiliesAsync(HttpContext ctx, NanchesoftDbContext db)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(ctx);
        var tenantId  = ApiTenantScope.ResolveTenantId(ctx);
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var cfg = await db.SilvaSoftConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId.Value);
        if (cfg is null)
            return Results.NotFound(new { message = "Sin configuración SilvaSoft." });

        if (!tenantId.HasValue)
            tenantId = await db.Companies.Where(x => x.Id == companyId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefaultAsync();

        var log = new SilvaSoftSyncLog
        {
            TenantId = tenantId!.Value,
            CompanyId = companyId.Value,
            Operation = "import-families",
            Status = "running",
            StartedAt = DateTime.UtcNow,
            TriggeredBy = "web-api"
        };
        db.SilvaSoftSyncLogs.Add(log);
        await db.SaveChangesAsync();

        try
        {
            var rows = await QuerySilvaSoftFamiliesAsync(cfg);
            log.RecordsRead = rows.Count;

            int imported = 0, skipped = 0;
            foreach (var r in rows)
            {
                var code = (r.Code ?? string.Empty).Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(code)) { skipped++; continue; }

                var existing = await db.ProductFamilies.FirstOrDefaultAsync(x => x.CompanyId == companyId.Value && x.Code == code);
                if (existing is not null) { skipped++; continue; }

                db.ProductFamilies.Add(new ProductFamily
                {
                    TenantId = tenantId!.Value,
                    CompanyId = companyId.Value,
                    Code = code,
                    Name = (r.Name ?? code).Trim(),
                    StatisticsGroup = (r.StatisticsGroup ?? string.Empty).Trim(),
                    IsFinishedProductFamily = r.IsFinishedProductFamily,
                    IsActive = true,
                    CreatedBy = "silvasoft-import"
                });
                imported++;
            }

            await db.SaveChangesAsync();

            // stamp last sync
            var configEntity = await db.SilvaSoftConfigs.FirstOrDefaultAsync(x => x.CompanyId == companyId.Value);
            if (configEntity is not null) { configEntity.LastSyncAt = DateTime.UtcNow; }

            log.RecordsImported = imported;
            log.RecordsSkipped = skipped;
            log.Status = "ok";
            log.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true, imported, skipped, total = rows.Count });
        }
        catch (Exception ex)
        {
            log.Status = "error";
            log.ErrorMessage = ex.Message;
            log.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    // ── GET /api/silvasoft/composition ─────────────────────────────
    private static async Task<IResult> GetCompositionFromSilvaSoftAsync(
        HttpContext ctx,
        NanchesoftDbContext db,
        string? style = null,
        int page = 1,
        int pageSize = 100)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var cfg = await db.SilvaSoftConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId.Value);
        if (cfg is null)
            return Results.NotFound(new { message = "Sin configuración SilvaSoft." });

        try
        {
            var rows = await QuerySilvaSoftCompositionAsync(cfg, style, page, pageSize);
            return Results.Ok(rows);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    // ── POST /api/silvasoft/composition/import ──────────────────────
    private static async Task<IResult> ImportCompositionAsync(
        HttpContext ctx,
        SilvaSoftImportCompositionRequest req,
        NanchesoftDbContext db)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(ctx);
        var tenantId  = ApiTenantScope.ResolveTenantId(ctx);
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var cfg = await db.SilvaSoftConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId.Value);
        if (cfg is null)
            return Results.NotFound(new { message = "Sin configuración SilvaSoft." });

        if (!tenantId.HasValue)
            tenantId = await db.Companies.Where(x => x.Id == companyId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefaultAsync();

        var log = new SilvaSoftSyncLog
        {
            TenantId = tenantId!.Value,
            CompanyId = companyId.Value,
            Operation = "import-composition",
            Status = "running",
            StartedAt = DateTime.UtcNow,
            TriggeredBy = "web-api"
        };
        db.SilvaSoftSyncLogs.Add(log);
        await db.SaveChangesAsync();

        try
        {
            var rows = await QuerySilvaSoftCompositionAsync(cfg, req.StyleFilter, 1, 5000);
            log.RecordsRead = rows.Count;

            // Ensure a generic SilvaSoft import component exists for this company
            const string importComponentCode = "SS-IMPORT";
            var importComponent = await db.ProductComponents.FirstOrDefaultAsync(
                x => x.CompanyId == companyId.Value && x.Code == importComponentCode);
            if (importComponent is null)
            {
                importComponent = new ProductComponent
                {
                    TenantId = tenantId!.Value,
                    CompanyId = companyId.Value,
                    Code = importComponentCode,
                    Name = "Importado SilvaSoft",
                    IsActive = true,
                    CreatedBy = "silvasoft-import"
                };
                db.ProductComponents.Add(importComponent);
                await db.SaveChangesAsync();
            }

            int imported = 0, skipped = 0;
            foreach (var r in rows)
            {
                var styleCode = (r.StyleCode ?? string.Empty).Trim().ToUpperInvariant();
                var materialCode = (r.MaterialCode ?? string.Empty).Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(styleCode) || string.IsNullOrWhiteSpace(materialCode))
                {
                    skipped++;
                    continue;
                }

                var fp = await db.FinishedProducts.FirstOrDefaultAsync(x => x.CompanyId == companyId.Value && x.Code == styleCode);
                if (fp is null) { skipped++; continue; }

                var mat = await db.MaterialItems.FirstOrDefaultAsync(x => x.CompanyId == companyId.Value && x.Code == materialCode);
                if (mat is null) { skipped++; continue; }

                var exists = await db.FinishedProductMaterials.AnyAsync(x =>
                    x.FinishedProductId == fp.Id && x.MaterialItemId == mat.Id && x.ProductComponentId == importComponent.Id);
                if (exists) { skipped++; continue; }

                db.FinishedProductMaterials.Add(new FinishedProductMaterial
                {
                    TenantId = tenantId!.Value,
                    CompanyId = companyId.Value,
                    FinishedProductId = fp.Id,
                    ProductComponentId = importComponent.Id,
                    MaterialItemId = mat.Id,
                    Quantity = r.Quantity > 0 ? r.Quantity : 1,
                    Notes = r.Notes ?? string.Empty,
                    IsActive = true,
                    CreatedBy = "silvasoft-import"
                });
                imported++;
            }

            await db.SaveChangesAsync();

            var configEntity = await db.SilvaSoftConfigs.FirstOrDefaultAsync(x => x.CompanyId == companyId.Value);
            if (configEntity is not null) configEntity.LastSyncAt = DateTime.UtcNow;

            log.RecordsImported = imported;
            log.RecordsSkipped = skipped;
            log.Status = "ok";
            log.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true, imported, skipped, total = rows.Count });
        }
        catch (Exception ex)
        {
            log.Status = "error";
            log.ErrorMessage = ex.Message;
            log.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    // ── GET /api/silvasoft/logs ─────────────────────────────────────
    private static async Task<IResult> GetLogsAsync(HttpContext ctx, NanchesoftDbContext db, int page = 1, int pageSize = 50)
    {
        var companyId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!companyId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var total = await db.SilvaSoftSyncLogs.CountAsync(x => x.CompanyId == companyId.Value);
        var rows = await db.SilvaSoftSyncLogs
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value)
            .OrderByDescending(x => x.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SilvaSoftSyncLogDto
            {
                Id = x.Id,
                Operation = x.Operation,
                Status = x.Status,
                RecordsRead = x.RecordsRead,
                RecordsImported = x.RecordsImported,
                RecordsSkipped = x.RecordsSkipped,
                ErrorMessage = x.ErrorMessage,
                StartedAt = x.StartedAt,
                FinishedAt = x.FinishedAt,
                TriggeredBy = x.TriggeredBy
            })
            .ToListAsync();

        return Results.Ok(new { total, page, pageSize, rows });
    }

    // ── SQL Server helpers ──────────────────────────────────────────

    private static SqlConnection BuildConnection(SilvaSoftConfig cfg)
    {
        var csb = new SqlConnectionStringBuilder
        {
            DataSource = cfg.Port == 1433 ? cfg.ServerHost : $"{cfg.ServerHost},{cfg.Port}",
            InitialCatalog = cfg.DatabaseName,
            UserID = cfg.DbUser,
            Password = cfg.DbPassword,
            TrustServerCertificate = cfg.TrustServerCertificate,
            ConnectTimeout = 10
        };
        return new SqlConnection(csb.ConnectionString);
    }

    private static async Task<List<SilvaSoftFamilyRow>> QuerySilvaSoftFamiliesAsync(SilvaSoftConfig cfg)
    {
        var result = new List<SilvaSoftFamilyRow>();
        await using var conn = BuildConnection(cfg);
        await conn.OpenAsync();

        // Consulta genérica compatible con el esquema típico de SilvaSoft
        const string sql = """
            SELECT
                ISNULL(CAST(cve_familia AS NVARCHAR(30)), '') AS Code,
                ISNULL(desc_familia, '') AS Name,
                ISNULL(grupo_estadistico, '') AS StatisticsGroup,
                CASE WHEN tipo_producto = 'T' THEN 1 ELSE 0 END AS IsFinishedProductFamily
            FROM dbo.familias
            ORDER BY cve_familia
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new SilvaSoftFamilyRow
            {
                Code = reader.GetString(0),
                Name = reader.GetString(1),
                StatisticsGroup = reader.GetString(2),
                IsFinishedProductFamily = reader.GetInt32(3) == 1
            });
        }
        return result;
    }

    private static async Task<List<SilvaSoftCompositionRow>> QuerySilvaSoftCompositionAsync(
        SilvaSoftConfig cfg, string? styleFilter, int page, int pageSize)
    {
        var result = new List<SilvaSoftCompositionRow>();
        await using var conn = BuildConnection(cfg);
        await conn.OpenAsync();

        var styleWhere = string.IsNullOrWhiteSpace(styleFilter)
            ? string.Empty
            : "AND UPPER(LTRIM(RTRIM(c.cve_estilo))) LIKE @style";

        var sql = $"""
            SELECT
                ISNULL(CAST(c.cve_estilo   AS NVARCHAR(60)), '') AS StyleCode,
                ISNULL(CAST(c.cve_material AS NVARCHAR(60)), '') AS MaterialCode,
                ISNULL(c.cantidad, 0)                            AS Quantity,
                ISNULL(c.notas, '')                              AS Notes
            FROM dbo.composicion c
            WHERE 1=1 {styleWhere}
            ORDER BY c.cve_estilo, c.cve_material
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
            """;

        await using var cmd = new SqlCommand(sql, conn);
        if (!string.IsNullOrWhiteSpace(styleFilter))
            cmd.Parameters.AddWithValue("@style", $"%{styleFilter.Trim().ToUpperInvariant()}%");
        cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@pageSize", pageSize);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new SilvaSoftCompositionRow
            {
                StyleCode = reader.GetString(0),
                MaterialCode = reader.GetString(1),
                Quantity = reader.GetDecimal(2),
                Notes = reader.GetString(3)
            });
        }
        return result;
    }
}

// ── DTOs & request models ──────────────────────────────────────────

public sealed class SilvaSoftConfigRequest
{
    public string ServerHost { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string? DbUser { get; set; }
    public string? DbPassword { get; set; }
    public int Port { get; set; } = 1433;
    public bool TrustServerCertificate { get; set; } = true;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SilvaSoftConfigDto
{
    public Guid Id { get; set; }
    public string ServerHost { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string DbUser { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool TrustServerCertificate { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SilvaSoftImportCompositionRequest
{
    public string? StyleFilter { get; set; }
}

public sealed class SilvaSoftSyncLogDto
{
    public Guid Id { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RecordsRead { get; set; }
    public int RecordsImported { get; set; }
    public int RecordsSkipped { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? TriggeredBy { get; set; }
}

public sealed class SilvaSoftFamilyRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StatisticsGroup { get; set; } = string.Empty;
    public bool IsFinishedProductFamily { get; set; }
}

public sealed class SilvaSoftCompositionRow
{
    public string StyleCode { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}
