using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Application.SilvaSoft;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

/*
 * SilvaSoftEndpoints — API REST de integración SilvaSoft.
 *
 * ARQUITECTURA:
 * - ISilvaSoftService   → operaciones contra SQL Server de SilvaSoft
 * - ISilvaSoftConexionRepository → CRUD de configuración en Postgres
 * - NanchesoftDbContext → datos Nanchesoft para validación de duplicados
 *
 * MULTI-TENANT:
 * Todos los endpoints leen empresaId desde el header X-Tenant-Company
 * vía ApiTenantScope.ResolveCompanyId(). Cada empresa opera de forma
 * completamente aislada.
 *
 * ENDPOINTS:
 * GET    /api/silvasoft/config              → obtener configuración de la empresa
 * POST   /api/silvasoft/config              → guardar/actualizar configuración
 * GET    /api/silvasoft/probar-conexion     → test de conexión SQL Server
 * GET    /api/silvasoft/composicion         → leer tabla composicion de SilvaSoft
 * GET    /api/silvasoft/composicion/vista-importacion → preview + mapeo + duplicados
 * GET    /api/silvasoft/logs                → historial de sincronizaciones
 */
public static class SilvaSoftEndpoints
{
    public static IEndpointRouteBuilder MapSilvaSoftEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/silvasoft").WithTags("SilvaSoft");

        g.MapGet("/config",                         GetConfigAsync);
        g.MapPost("/config",                        SaveConfigAsync);
        g.MapGet("/probar-conexion",                ProbarConexionAsync);
        g.MapGet("/composicion",                    GetComposicionAsync);
        g.MapGet("/composicion/vista-importacion",  GetVistaImportacionAsync);
        g.MapPost("/composicion/importar",          ImportarFamiliasMaterialesAsync);
        g.MapGet("/clase",                          GetClaseAsync);
        g.MapGet("/clase/vista-importacion",        GetVistaImportacionSubfamiliasAsync);
        g.MapPost("/clase/importar",                ImportarSubfamiliasAsync);
        g.MapGet("/logs",                           GetLogsAsync);

        return app;
    }

    // ── GET /api/silvasoft/config ─────────────────────────────────────────────

    private static async Task<IResult> GetConfigAsync(
        HttpContext ctx,
        ISilvaSoftConexionRepository repo)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var cfg = await repo.ObtenerPorEmpresaAsync(empresaId.Value);
        return cfg is null
            ? Results.NotFound(new { message = "Sin configuración SilvaSoft para esta empresa." })
            : Results.Ok(cfg);
    }

    // ── POST /api/silvasoft/config ────────────────────────────────────────────

    private static async Task<IResult> SaveConfigAsync(
        HttpContext ctx,
        SilvaSoftConexionRequest req,
        ISilvaSoftConexionRepository repo,
        NanchesoftDbContext db)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        if (string.IsNullOrWhiteSpace(req.NombreServidor))
            return Results.BadRequest(new { message = "NombreServidor es obligatorio." });
        if (string.IsNullOrWhiteSpace(req.BaseDatos))
            return Results.BadRequest(new { message = "BaseDatos es obligatorio." });

        var tenantId = ApiTenantScope.ResolveTenantId(ctx)
            ?? await db.Companies
                .Where(x => x.Id == empresaId.Value)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefaultAsync();

        if (!tenantId.HasValue)
            return Results.BadRequest(new { message = "No se pudo resolver el tenant." });

        await repo.GuardarAsync(req, tenantId.Value, empresaId.Value);
        return Results.Ok(new { success = true });
    }

    // ── GET /api/silvasoft/probar-conexion ────────────────────────────────────

    private static async Task<IResult> ProbarConexionAsync(
        HttpContext ctx,
        ISilvaSoftService svc)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var (exitoso, mensaje, tiempoMs) = await svc.ProbarConexionAsync(empresaId.Value);
        return Results.Ok(new { exitoso, mensaje, tiempoMs });
    }

    // ── GET /api/silvasoft/composicion ────────────────────────────────────────

    private static async Task<IResult> GetComposicionAsync(
        HttpContext ctx,
        ISilvaSoftService svc,
        int top = 100)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        top = Math.Clamp(top, 1, 5000);
        var resultado = await svc.ObtenerComposicionesAsync(empresaId.Value, top);
        return Results.Ok(resultado);
    }

    // ── GET /api/silvasoft/clase ──────────────────────────────────────────────

    private static async Task<IResult> GetClaseAsync(
        HttpContext ctx,
        ISilvaSoftService svc,
        int top = 200)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        top = Math.Clamp(top, 1, 5000);
        var resultado = await svc.ObtenerClaseAsync(empresaId.Value, top);
        return Results.Ok(resultado);
    }

    // ── GET /api/silvasoft/composicion/vista-importacion ─────────────────────

    private static async Task<IResult> GetVistaImportacionAsync(
        HttpContext ctx,
        ISilvaSoftService svc,
        NanchesoftDbContext db)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        // 1. Obtener datos de SilvaSoft
        var resultado = await svc.ObtenerComposicionesAsync(empresaId.Value, 500);
        if (!resultado.Exitoso)
            return Results.Problem(resultado.Error ?? "Error al conectar con SilvaSoft.", statusCode: 502);

        // 2. Obtener familias existentes (por código y por SilvaSoftComposicionId)
        var codigosExistentes = await db.MaterialFamilies
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId.Value)
            .Select(x => x.Code.ToUpperInvariant())
            .ToHashSetAsync();

        var idsExistentes = await db.MaterialFamilies
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId.Value && x.SilvaSoftComposicionId != null)
            .Select(x => x.SilvaSoftComposicionId!.Value)
            .ToHashSetAsync();

        // 3. Detectar columnas
        var mapeo = BuildMapeoColumnas(resultado.Columnas);
        var colPk     = FindColumnExact(resultado.Columnas, "composicionid");
        var colCodigo = colPk ?? FindColumn(resultado.Columnas, ["codigo", "clave", "cve", "code"]);
        var colNombre = FindColumn(resultado.Columnas, ["nombre", "descripcion", "name", "desc", "description"]);

        // 4. Clasificar cada registro
        var vistaPrev = new List<SilvaSoftRegistroVistaPrevia>();
        int nuevos = 0, duplicados = 0, invalidos = 0;

        foreach (var reg in resultado.Registros)
        {
            int? composicionId = null;
            if (colPk is not null && reg.Campos.TryGetValue(colPk, out var pkVal) && pkVal is not null)
                _ = int.TryParse(pkVal.ToString(), out var p) ? composicionId = p : composicionId = null;

            var codigo = colCodigo is not null
                ? reg.Campos.GetValueOrDefault(colCodigo)?.ToString()?.Trim().ToUpperInvariant() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(codigo) && composicionId.HasValue)
                codigo = composicionId.Value.ToString();

            var nombre = colNombre is not null
                ? reg.Campos.GetValueOrDefault(colNombre)?.ToString()?.Trim() ?? string.Empty
                : string.Empty;

            string estado;
            string? razon;

            if (string.IsNullOrWhiteSpace(codigo))
            {
                estado = "invalido";
                razon = "Sin código identificador";
                invalidos++;
            }
            else if ((composicionId.HasValue && idsExistentes.Contains(composicionId.Value))
                     || codigosExistentes.Contains(codigo))
            {
                estado = "duplicado";
                razon = $"Ya existe la familia '{codigo}' en Nanchesoft";
                duplicados++;
            }
            else
            {
                estado = "nuevo";
                razon = null;
                nuevos++;
            }

            if (vistaPrev.Count < 50)
            {
                vistaPrev.Add(new SilvaSoftRegistroVistaPrevia
                {
                    Codigo = codigo,
                    Nombre = nombre,
                    Estado = estado,
                    Razon = razon
                });
            }
        }

        var vista = new SilvaSoftVistaImportacionDto
        {
            TotalEnSilvaSoft = resultado.Total,
            YaExistentesEnNanchesoft = duplicados,
            NuevosParaImportar = nuevos,
            RegistrosInvalidos = invalidos,
            Mapeo = mapeo,
            VistaPrevia = vistaPrev
        };

        return Results.Ok(vista);
    }

    // ── POST /api/silvasoft/composicion/importar ─────────────────────────────

    private static async Task<IResult> ImportarFamiliasMaterialesAsync(
        HttpContext ctx,
        ISilvaSoftService svc,
        NanchesoftDbContext db)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var tenantId = ApiTenantScope.ResolveTenantId(ctx)
            ?? await db.Companies
                .Where(x => x.Id == empresaId.Value)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefaultAsync();

        if (!tenantId.HasValue)
            return Results.BadRequest(new { message = "No se pudo resolver el tenant." });

        var sw = Stopwatch.StartNew();
        var log = new SilvaSoftSyncLog
        {
            TenantId = tenantId.Value,
            CompanyId = empresaId.Value,
            Operation = "ImportarFamiliasMateriales",
            Status = "iniciado",
            StartedAt = DateTime.UtcNow,
            TriggeredBy = ctx.User.Identity?.Name ?? "usuario"
        };
        db.SilvaSoftSyncLogs.Add(log);
        await db.SaveChangesAsync();

        try
        {
            // 1. Leer datos de SilvaSoft
            var resultado = await svc.ObtenerComposicionesAsync(empresaId.Value, 2000);
            if (!resultado.Exitoso)
            {
                log.Status = "error";
                log.ErrorMessage = resultado.Error;
                log.FinishedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Problem(resultado.Error ?? "Error al conectar con SilvaSoft.", statusCode: 502);
            }

            log.RecordsRead = resultado.Total;

            // 2. Detectar columnas: PK (composicionid), código y nombre
            var colPk     = FindColumnExact(resultado.Columnas, "composicionid");
            var colCodigo = colPk ?? FindColumn(resultado.Columnas, ["codigo", "clave", "cve", "code"]);
            var colNombre = FindColumn(resultado.Columnas, ["nombre", "descripcion", "name", "desc", "description"]);

            // 3. IDs SilvaSoft ya importados + códigos existentes para detección de duplicados
            var idsExistentes = await db.MaterialFamilies
                .AsNoTracking()
                .Where(x => x.CompanyId == empresaId.Value && x.SilvaSoftComposicionId != null)
                .Select(x => x.SilvaSoftComposicionId!.Value)
                .ToHashSetAsync();

            var codigosExistentes = await db.MaterialFamilies
                .AsNoTracking()
                .Where(x => x.CompanyId == empresaId.Value)
                .Select(x => x.Code.ToUpperInvariant())
                .ToHashSetAsync();

            // 4. Clasificar e importar
            var nuevas = new List<MaterialFamily>();
            int omitidos = 0, invalidos = 0;
            var detalles = new List<string>();

            foreach (var reg in resultado.Registros)
            {
                // Leer composicionid (PK de SilvaSoft)
                int? composicionId = null;
                if (colPk is not null && reg.Campos.TryGetValue(colPk, out var pkVal) && pkVal is not null)
                    _ = int.TryParse(pkVal.ToString(), out var parsedPk) ? composicionId = parsedPk : composicionId = null;

                var codigo = colCodigo is not null
                    ? reg.Campos.GetValueOrDefault(colCodigo)?.ToString()?.Trim().ToUpperInvariant() ?? string.Empty
                    : string.Empty;

                var nombre = colNombre is not null
                    ? reg.Campos.GetValueOrDefault(colNombre)?.ToString()?.Trim() ?? string.Empty
                    : string.Empty;

                // Si no hay código, usar el composicionid como fallback
                if (string.IsNullOrWhiteSpace(codigo) && composicionId.HasValue)
                    codigo = composicionId.Value.ToString();

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    invalidos++;
                    continue;
                }

                // Duplicado por SilvaSoft ID (más confiable) o por código
                if ((composicionId.HasValue && idsExistentes.Contains(composicionId.Value))
                    || codigosExistentes.Contains(codigo))
                {
                    omitidos++;
                    continue;
                }

                if (composicionId.HasValue) idsExistentes.Add(composicionId.Value);
                codigosExistentes.Add(codigo);
                nuevas.Add(new MaterialFamily
                {
                    TenantId = tenantId.Value,
                    CompanyId = empresaId.Value,
                    Code = codigo.Length > 40 ? codigo[..40] : codigo,
                    Name = string.IsNullOrWhiteSpace(nombre) ? codigo : (nombre.Length > 140 ? nombre[..140] : nombre),
                    IsActive = true,
                    SilvaSoftComposicionId = composicionId,
                    CreatedBy = "SilvaSoft-Import",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 5. Persistir
            if (nuevas.Count > 0)
            {
                db.MaterialFamilies.AddRange(nuevas);
            }

            sw.Stop();

            log.RecordsImported = nuevas.Count;
            log.RecordsSkipped = omitidos + invalidos;
            log.Status = "completado";
            log.FinishedAt = DateTime.UtcNow;
            if (invalidos > 0) detalles.Add($"{invalidos} registros sin código (ignorados).");
            if (omitidos > 0) detalles.Add($"{omitidos} duplicados omitidos (ya existían en Nanchesoft).");
            if (nuevas.Count > 0) detalles.Add($"{nuevas.Count} familias importadas correctamente.");

            await db.SaveChangesAsync();

            return Results.Ok(new SilvaSoftImportResultadoDto
            {
                Exitoso = true,
                RegistrosLeidos = resultado.Total,
                RegistrosImportados = nuevas.Count,
                RegistrosOmitidos = omitidos,
                RegistrosInvalidos = invalidos,
                TiempoMs = sw.ElapsedMilliseconds,
                Detalles = detalles
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.Status = "error";
            log.ErrorMessage = ex.Message;
            log.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Problem($"Error inesperado: {ex.Message}", statusCode: 500);
        }
    }

    // ── GET /api/silvasoft/logs ───────────────────────────────────────────────

    private static async Task<IResult> GetLogsAsync(
        HttpContext ctx,
        NanchesoftDbContext db,
        int pagina = 1,
        int tamano = 50)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        tamano = Math.Clamp(tamano, 1, 200);
        var total = await db.SilvaSoftSyncLogs.CountAsync(x => x.CompanyId == empresaId.Value);
        var rows = await db.SilvaSoftSyncLogs
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId.Value)
            .OrderByDescending(x => x.StartedAt)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(x => new SilvaSoftSyncLogDto
            {
                Id = x.Id,
                Operacion = x.Operation,
                Estado = x.Status,
                RegistrosLeidos = x.RecordsRead,
                RegistrosImportados = x.RecordsImported,
                RegistrosOmitidos = x.RecordsSkipped,
                MensajeError = x.ErrorMessage,
                Iniciado = x.StartedAt,
                Terminado = x.FinishedAt,
                DuracionMs = x.FinishedAt.HasValue
                    ? (long)(x.FinishedAt.Value - x.StartedAt).TotalMilliseconds
                    : null,
                DisparadoPor = x.TriggeredBy
            })
            .ToListAsync();

        return Results.Ok(new SilvaSoftSyncLogPagina
        {
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamano,
            Registros = rows
        });
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    // ── GET /api/silvasoft/clase/vista-importacion ────────────────────────────

    private static async Task<IResult> GetVistaImportacionSubfamiliasAsync(
        HttpContext ctx,
        ISilvaSoftService svc,
        NanchesoftDbContext db)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var resultado = await svc.ObtenerClaseAsync(empresaId.Value, 2000);
        if (!resultado.Exitoso)
            return Results.Problem(resultado.Error ?? "Error al conectar con SilvaSoft.", statusCode: 502);

        // Subfamilias existentes (por SilvaSoftClaseId y por código)
        var idsExistentes = await db.MaterialSubfamilies
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId.Value && x.SilvaSoftClaseId != null)
            .Select(x => x.SilvaSoftClaseId!.Value)
            .ToHashSetAsync();

        var codigosExistentes = await db.MaterialSubfamilies
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId.Value)
            .Select(x => x.Code.ToUpperInvariant())
            .ToHashSetAsync();

        // Familias padre disponibles (por SilvaSoftComposicionId)
        var familiasPorSilvaSoftId = await db.MaterialFamilies
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId.Value && x.SilvaSoftComposicionId != null)
            .ToDictionaryAsync(x => x.SilvaSoftComposicionId!.Value, x => x.Name);

        var colPk          = FindColumnExact(resultado.Columnas, "claseid");
        var colComposicion = FindColumnExact(resultado.Columnas, "composicionid");
        var colCodigo      = colPk ?? FindColumn(resultado.Columnas, ["codigo", "clave", "cve", "code"]);
        var colNombre      = FindColumn(resultado.Columnas, ["nombre", "descripcion", "name", "desc", "description"]);
        var mapeo          = BuildMapeoColumnas(resultado.Columnas);

        var vistaPrev = new List<SilvaSoftRegistroVistaSubfamiliaPrevia>();
        int nuevos = 0, duplicados = 0, invalidos = 0, sinPadre = 0;

        foreach (var reg in resultado.Registros)
        {
            int? claseId = null;
            if (colPk is not null && reg.Campos.TryGetValue(colPk, out var pkVal) && pkVal is not null)
                _ = int.TryParse(pkVal.ToString(), out var p) ? claseId = p : claseId = null;

            int? composicionId = null;
            if (colComposicion is not null && reg.Campos.TryGetValue(colComposicion, out var cvVal) && cvVal is not null)
                _ = int.TryParse(cvVal.ToString(), out var pc) ? composicionId = pc : composicionId = null;

            var codigo = colCodigo is not null
                ? reg.Campos.GetValueOrDefault(colCodigo)?.ToString()?.Trim().ToUpperInvariant() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(codigo) && claseId.HasValue) codigo = claseId.Value.ToString();

            var nombre = colNombre is not null
                ? reg.Campos.GetValueOrDefault(colNombre)?.ToString()?.Trim() ?? string.Empty
                : string.Empty;

            string? familiaPadre = composicionId.HasValue && familiasPorSilvaSoftId.TryGetValue(composicionId.Value, out var fn) ? fn : null;

            string estado; string? razon;
            if (string.IsNullOrWhiteSpace(codigo))
            { estado = "invalido"; razon = "Sin código"; invalidos++; }
            else if ((claseId.HasValue && idsExistentes.Contains(claseId.Value)) || codigosExistentes.Contains(codigo))
            { estado = "duplicado"; razon = $"Ya existe '{codigo}' en Nanchesoft"; duplicados++; }
            else if (composicionId is null || familiaPadre is null)
            { estado = "invalido"; razon = composicionId is null ? "Sin composicionid (familia padre)" : $"Familia padre composicionid={composicionId} no importada aún"; sinPadre++; invalidos++; }
            else
            { estado = "nuevo"; razon = null; nuevos++; }

            if (vistaPrev.Count < 50)
                vistaPrev.Add(new SilvaSoftRegistroVistaSubfamiliaPrevia
                {
                    ClaseId = claseId, ComposicionId = composicionId,
                    Codigo = codigo, Nombre = nombre,
                    Estado = estado, Razon = razon, FamiliaPadre = familiaPadre
                });
        }

        return Results.Ok(new SilvaSoftVistaImportacionSubfamiliasDto
        {
            TotalEnSilvaSoft = resultado.Total,
            YaExistentesEnNanchesoft = duplicados,
            NuevosParaImportar = nuevos,
            RegistrosInvalidos = invalidos,
            SinFamiliaPadre = sinPadre,
            Mapeo = mapeo,
            VistaPrevia = vistaPrev
        });
    }

    // ── POST /api/silvasoft/clase/importar ────────────────────────────────────

    private static async Task<IResult> ImportarSubfamiliasAsync(
        HttpContext ctx,
        ISilvaSoftService svc,
        NanchesoftDbContext db)
    {
        var empresaId = ApiTenantScope.ResolveCompanyId(ctx);
        if (!empresaId.HasValue)
            return Results.BadRequest(new { message = "No hay empresa activa." });

        var tenantId = ApiTenantScope.ResolveTenantId(ctx)
            ?? await db.Companies
                .Where(x => x.Id == empresaId.Value)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefaultAsync();

        if (!tenantId.HasValue)
            return Results.BadRequest(new { message = "No se pudo resolver el tenant." });

        var sw = Stopwatch.StartNew();
        var log = new SilvaSoftSyncLog
        {
            TenantId = tenantId.Value,
            CompanyId = empresaId.Value,
            Operation = "ImportarSubfamilias",
            Status = "iniciado",
            StartedAt = DateTime.UtcNow,
            TriggeredBy = ctx.User.Identity?.Name ?? "usuario"
        };
        db.SilvaSoftSyncLogs.Add(log);
        await db.SaveChangesAsync();

        try
        {
            var resultado = await svc.ObtenerClaseAsync(empresaId.Value, 5000);
            if (!resultado.Exitoso)
            {
                log.Status = "error"; log.ErrorMessage = resultado.Error; log.FinishedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Problem(resultado.Error ?? "Error al conectar con SilvaSoft.", statusCode: 502);
            }
            log.RecordsRead = resultado.Total;

            var colPk          = FindColumnExact(resultado.Columnas, "claseid");
            var colComposicion = FindColumnExact(resultado.Columnas, "composicionid");
            var colCodigo      = colPk ?? FindColumn(resultado.Columnas, ["codigo", "clave", "cve", "code"]);
            var colNombre      = FindColumn(resultado.Columnas, ["nombre", "descripcion", "name", "desc", "description"]);

            // Familias padre (necesarias para el FK MaterialFamilyId)
            var familias = await db.MaterialFamilies
                .AsNoTracking()
                .Where(x => x.CompanyId == empresaId.Value && x.SilvaSoftComposicionId != null)
                .ToDictionaryAsync(x => x.SilvaSoftComposicionId!.Value, x => x.Id);

            var idsExistentes = await db.MaterialSubfamilies
                .AsNoTracking()
                .Where(x => x.CompanyId == empresaId.Value && x.SilvaSoftClaseId != null)
                .Select(x => x.SilvaSoftClaseId!.Value)
                .ToHashSetAsync();

            var codigosExistentes = await db.MaterialSubfamilies
                .AsNoTracking()
                .Where(x => x.CompanyId == empresaId.Value)
                .Select(x => x.Code.ToUpperInvariant())
                .ToHashSetAsync();

            var nuevas = new List<MaterialSubfamily>();
            int omitidos = 0, invalidos = 0, sinPadre = 0;
            var detalles = new List<string>();

            foreach (var reg in resultado.Registros)
            {
                int? claseId = null;
                if (colPk is not null && reg.Campos.TryGetValue(colPk, out var pkVal) && pkVal is not null)
                    _ = int.TryParse(pkVal.ToString(), out var p) ? claseId = p : claseId = null;

                int? composicionId = null;
                if (colComposicion is not null && reg.Campos.TryGetValue(colComposicion, out var cvVal) && cvVal is not null)
                    _ = int.TryParse(cvVal.ToString(), out var pc) ? composicionId = pc : composicionId = null;

                var codigo = colCodigo is not null
                    ? reg.Campos.GetValueOrDefault(colCodigo)?.ToString()?.Trim().ToUpperInvariant() ?? string.Empty
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(codigo) && claseId.HasValue) codigo = claseId.Value.ToString();

                var nombre = colNombre is not null
                    ? reg.Campos.GetValueOrDefault(colNombre)?.ToString()?.Trim() ?? string.Empty
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(codigo)) { invalidos++; continue; }
                if ((claseId.HasValue && idsExistentes.Contains(claseId.Value)) || codigosExistentes.Contains(codigo))
                { omitidos++; continue; }

                if (composicionId is null || !familias.TryGetValue(composicionId.Value, out var familiaId))
                { sinPadre++; continue; }

                if (claseId.HasValue) idsExistentes.Add(claseId.Value);
                codigosExistentes.Add(codigo);
                nuevas.Add(new MaterialSubfamily
                {
                    TenantId = tenantId.Value,
                    CompanyId = empresaId.Value,
                    MaterialFamilyId = familiaId,
                    Code = codigo.Length > 40 ? codigo[..40] : codigo,
                    Name = string.IsNullOrWhiteSpace(nombre) ? codigo : (nombre.Length > 140 ? nombre[..140] : nombre),
                    IsActive = true,
                    SilvaSoftClaseId = claseId,
                    SilvaSoftComposicionId = composicionId,
                    CreatedBy = "SilvaSoft-Import",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (nuevas.Count > 0) db.MaterialSubfamilies.AddRange(nuevas);

            sw.Stop();
            log.RecordsImported = nuevas.Count;
            log.RecordsSkipped  = omitidos + invalidos + sinPadre;
            log.Status          = "completado";
            log.FinishedAt      = DateTime.UtcNow;

            if (invalidos > 0)  detalles.Add($"{invalidos} registros sin código (ignorados).");
            if (sinPadre > 0)   detalles.Add($"{sinPadre} subfamilias sin familia padre importada (ignorados — importa familias primero).");
            if (omitidos > 0)   detalles.Add($"{omitidos} duplicados omitidos.");
            if (nuevas.Count > 0) detalles.Add($"{nuevas.Count} subfamilias importadas correctamente.");

            await db.SaveChangesAsync();

            return Results.Ok(new SilvaSoftImportSubfamiliasResultadoDto
            {
                Exitoso = true,
                RegistrosLeidos = resultado.Total,
                RegistrosImportados = nuevas.Count,
                RegistrosOmitidos = omitidos,
                RegistrosInvalidos = invalidos,
                SinFamiliaPadre = sinPadre,
                TiempoMs = sw.ElapsedMilliseconds,
                Detalles = detalles
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.Status = "error"; log.ErrorMessage = ex.Message; log.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Problem($"Error inesperado: {ex.Message}", statusCode: 500);
        }
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private static string? FindColumnExact(List<SilvaSoftColumnaMeta> columnas, string nombre)
        => columnas.Select(c => c.NombreColumna)
                   .FirstOrDefault(n => n.Equals(nombre, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Busca la primera columna que contenga alguno de los candidatos (insensible a mayúsculas).
    /// </summary>
    private static string? FindColumn(List<SilvaSoftColumnaMeta> columnas, string[] candidatos)
        => columnas
            .Select(c => c.NombreColumna)
            .FirstOrDefault(n => candidatos.Any(c =>
                n.Contains(c, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Construye el mapeo SilvaSoft columna → campo de Nanchesoft.</summary>
    private static List<SilvaSoftMapeoColumna> BuildMapeoColumnas(List<SilvaSoftColumnaMeta> columnas)
    {
        var mappingRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "clave"       , "Code" },
            { "codigo"      , "Code" },
            { "cve"         , "Code" },
            { "code"        , "Code" },
            { "nombre"      , "Name" },
            { "descripcion" , "Name" },
            { "name"        , "Name" },
            { "desc"        , "Name" },
            { "grupo"       , "InventoryGroup" },
            { "group"       , "InventoryGroup" },
            { "inventario"  , "InventoryGroup" },
            { "notas"       , "Notes" },
            { "notes"       , "Notes" },
            { "obs"         , "Notes" }
        };

        return columnas.Select(col =>
        {
            var destino = mappingRules
                .Where(kv => col.NombreColumna.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Value)
                .FirstOrDefault() ?? string.Empty;

            return new SilvaSoftMapeoColumna
            {
                ColumnaOrigen = col.NombreColumna,
                CampoDestino = destino,
                TipoDato = col.TipoDato,
                Nota = string.IsNullOrEmpty(destino) ? "Sin mapeo automático" : null
            };
        }).ToList();
    }
}
