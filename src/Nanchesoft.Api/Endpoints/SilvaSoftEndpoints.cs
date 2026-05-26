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

        // 2. Obtener familias de materiales existentes en Nanchesoft (para detectar duplicados)
        var codigosExistentes = await db.MaterialFamilies
            .AsNoTracking()
            .Where(x => x.CompanyId == empresaId.Value)
            .Select(x => x.Code)
            .ToHashSetAsync();

        // 3. Detectar qué columnas de SilvaSoft mapean a qué campos de MaterialFamily
        var mapeo = BuildMapeoColumnas(resultado.Columnas);

        // Columnas candidatas para código y nombre (heurística por nombre de columna)
        var colCodigo = FindColumn(resultado.Columnas, ["codigo", "clave", "cve", "code", "id"]);
        var colNombre = FindColumn(resultado.Columnas, ["nombre", "descripcion", "name", "desc", "description"]);

        // 4. Clasificar cada registro
        var vistaPrev = new List<SilvaSoftRegistroVistaPrevia>();
        int nuevos = 0, duplicados = 0, invalidos = 0;

        foreach (var reg in resultado.Registros)
        {
            var codigo = colCodigo is not null
                ? reg.Campos.GetValueOrDefault(colCodigo)?.ToString()?.Trim().ToUpperInvariant() ?? string.Empty
                : string.Empty;

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
            else if (codigosExistentes.Contains(codigo))
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

    /// <summary>
    /// Busca la primera columna que coincida con alguno de los candidatos (insensible a mayúsculas).
    /// Heurística para detectar columnas de código y nombre en esquemas variables de SilvaSoft.
    /// </summary>
    private static string? FindColumn(List<SilvaSoftColumnaMeta> columnas, string[] candidatos)
        => columnas
            .Select(c => c.NombreColumna)
            .FirstOrDefault(n => candidatos.Any(c =>
                n.Contains(c, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Construye el mapeo SilvaSoft columna → campo MaterialFamily de Nanchesoft.</summary>
    private static List<SilvaSoftMapeoColumna> BuildMapeoColumnas(List<SilvaSoftColumnaMeta> columnas)
    {
        // Mapa heurístico: palabras clave en nombre de columna → campo destino en Nanchesoft
        var mappingRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "clave"      , "Code" },
            { "codigo"     , "Code" },
            { "cve"        , "Code" },
            { "code"       , "Code" },
            { "nombre"     , "Name" },
            { "descripcion", "Name" },
            { "name"       , "Name" },
            { "desc"       , "Name" },
            { "grupo"      , "InventoryGroup" },
            { "group"      , "InventoryGroup" },
            { "inventario" , "InventoryGroup" },
            { "notas"      , "Notes" },
            { "notes"      , "Notes" },
            { "obs"        , "Notes" }
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
