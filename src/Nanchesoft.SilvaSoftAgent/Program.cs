using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.SqlClient;

// ─────────────────────────────────────────────────────────────────────────────
//  Nanchesoft SilvaSoft Agent — Windows Service / consola
//
//  Corre en la red local del cliente donde está el SQL Server de SilvaSoft.
//  Expone una HTTP API mínima para que Nanchesoft (en la nube) lea los datos
//  sin necesidad de abrir el puerto 1433 al exterior.
//
//  INSTALACIÓN:
//    sc create NanchesoftSilvaSoftAgent binPath="C:\ruta\NanchesoftSilvaSoftAgent.exe"
//    sc start NanchesoftSilvaSoftAgent
//
//  CONFIGURACIÓN: editar appsettings.json junto al ejecutable.
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Garantiza que appsettings.json se lea desde la carpeta del exe,
// sin importar desde qué directorio se lance (o como Windows Service).
builder.Host.UseContentRoot(AppContext.BaseDirectory);
builder.Services.AddWindowsService(opts => opts.ServiceName = "NanchesoftSilvaSoftAgent");

var app = builder.Build();

var cfg     = app.Configuration;
var token   = cfg["AgentToken"] ?? "";
var connStr = cfg["SqlServerConnectionString"] ?? "";

// ── Middleware de autenticación por token ─────────────────────────────────────
app.Use(async (ctx, next) =>
{
    // /ping no requiere auth
    if (ctx.Request.Path.StartsWithSegments("/ping"))
    {
        await next();
        return;
    }
    var incoming = ctx.Request.Headers["X-Agent-Token"].ToString();
    if (string.IsNullOrWhiteSpace(token) || incoming != token)
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { error = "Token inválido." });
        return;
    }
    await next();
});

// ── GET /ping ─────────────────────────────────────────────────────────────────
app.MapGet("/ping", () => Results.Ok(new
{
    ok = true,
    version = "1.0.0",
    servicio = "Nanchesoft SilvaSoft Agent"
}));

// ── GET /api/test ─────────────────────────────────────────────────────────────
app.MapGet("/api/test", async () =>
{
    if (string.IsNullOrWhiteSpace(connStr))
        return Results.Ok(new { exitoso = false, mensaje = "Sin cadena de conexión configurada.", tiempoMs = 0L });

    var sw = Stopwatch.StartNew();
    try
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        sw.Stop();
        return Results.Ok(new
        {
            exitoso = true,
            mensaje = $"Conexión exitosa. Servidor: {conn.DataSource} | BD: {conn.Database} | Tiempo: {sw.ElapsedMilliseconds}ms",
            tiempoMs = sw.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        sw.Stop();
        return Results.Ok(new { exitoso = false, mensaje = ex.Message, tiempoMs = sw.ElapsedMilliseconds });
    }
});

// ── GET /api/composicion?top=100 ──────────────────────────────────────────────
app.MapGet("/api/composicion", async (int top = 100) =>
{
    top = Math.Clamp(top, 1, 5000);
    const string tabla = "composicion";

    if (string.IsNullOrWhiteSpace(connStr))
        return Results.Ok(Falla("Sin cadena de conexión configurada.", tabla));

    var sw = Stopwatch.StartNew();
    try
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        // 1. Detectar columnas
        var columnas = await LeerColumnasAsync(conn, tabla);
        if (columnas.Count == 0)
        {
            sw.Stop();
            return Results.Ok(Falla($"La tabla '{tabla}' no existe en '{conn.Database}'.", tabla, conn.Database, sw.ElapsedMilliseconds));
        }

        // 2. Leer datos
        var colList = string.Join(", ", columnas.Select(c => $"[{c.NombreColumna}]"));
        var sql = $"SELECT TOP {top} {colList} FROM [{tabla}]";
        var registros = new List<Dictionary<string, object?>>();

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            registros.Add(row);
        }
        sw.Stop();

        return Results.Ok(new
        {
            exitoso    = true,
            error      = (string?)null,
            tiempoMs   = sw.ElapsedMilliseconds,
            nombreTabla = tabla,
            baseDatos  = conn.Database,
            total      = registros.Count,
            columnas,
            registros
        });
    }
    catch (Exception ex)
    {
        sw.Stop();
        return Results.Ok(Falla(ex.Message, tabla, "", sw.ElapsedMilliseconds));
    }
});

// ── GET /api/clase?top=2000 ───────────────────────────────────────────────────
app.MapGet("/api/clase", async (int top = 2000) =>
{
    top = Math.Clamp(top, 1, 5000);
    const string tabla = "clase";

    if (string.IsNullOrWhiteSpace(connStr))
        return Results.Ok(Falla("Sin cadena de conexión configurada.", tabla));

    var sw = Stopwatch.StartNew();
    try
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var columnas = await LeerColumnasAsync(conn, tabla);
        if (columnas.Count == 0)
        {
            sw.Stop();
            return Results.Ok(Falla($"La tabla '{tabla}' no existe en '{conn.Database}'.", tabla, conn.Database, sw.ElapsedMilliseconds));
        }

        var colList = string.Join(", ", columnas.Select(c => $"[{c.NombreColumna}]"));
        var sql = $"SELECT TOP {top} {colList} FROM [{tabla}]";
        var registros = new List<Dictionary<string, object?>>();

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            registros.Add(row);
        }
        sw.Stop();

        return Results.Ok(new
        {
            exitoso    = true,
            error      = (string?)null,
            tiempoMs   = sw.ElapsedMilliseconds,
            nombreTabla = tabla,
            baseDatos  = conn.Database,
            total      = registros.Count,
            columnas,
            registros
        });
    }
    catch (Exception ex)
    {
        sw.Stop();
        return Results.Ok(Falla(ex.Message, tabla, "", sw.ElapsedMilliseconds));
    }
});

// ── GET /api/fraccion?top=2000 ────────────────────────────────────────────────
app.MapGet("/api/fraccion", async (int top = 2000) =>
{
    top = Math.Clamp(top, 1, 5000);
    const string tabla = "Fraccion";

    if (string.IsNullOrWhiteSpace(connStr))
        return Results.Ok(Falla("Sin cadena de conexión configurada.", tabla));

    var sw = Stopwatch.StartNew();
    try
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var columnas = await LeerColumnasAsync(conn, tabla);
        if (columnas.Count == 0)
        {
            sw.Stop();
            return Results.Ok(Falla($"La tabla '{tabla}' no existe en '{conn.Database}'.", tabla, conn.Database, sw.ElapsedMilliseconds));
        }

        var colList = string.Join(", ", columnas.Select(c => $"[{c.NombreColumna}]"));
        var sql = $"SELECT TOP {top} {colList} FROM [{tabla}] ORDER BY [Secuencia], [Clave]";
        var registros = new List<Dictionary<string, object?>>();

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            registros.Add(row);
        }
        sw.Stop();

        return Results.Ok(new
        {
            exitoso = true,
            error = (string?)null,
            tiempoMs = sw.ElapsedMilliseconds,
            nombreTabla = tabla,
            baseDatos = conn.Database,
            total = registros.Count,
            columnas,
            registros
        });
    }
    catch (Exception ex)
    {
        sw.Stop();
        return Results.Ok(Falla(ex.Message, tabla, "", sw.ElapsedMilliseconds));
    }
});

// ── GET /api/fraccion_cadena?top=5000 ─────────────────────────────────────────
app.MapGet("/api/fraccion_cadena", async (int top = 5000) =>
{
    top = Math.Clamp(top, 1, 10000);
    const string tabla = "Fraccion_Cadena";

    if (string.IsNullOrWhiteSpace(connStr))
        return Results.Ok(Falla("Sin cadena de conexión configurada.", tabla));

    var sw = Stopwatch.StartNew();
    try
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var columnas = await LeerColumnasAsync(conn, tabla);
        if (columnas.Count == 0)
        {
            sw.Stop();
            return Results.Ok(Falla($"La tabla '{tabla}' no existe en '{conn.Database}'.", tabla, conn.Database, sw.ElapsedMilliseconds));
        }

        var colList = string.Join(", ", columnas.Select(c => $"[{c.NombreColumna}]"));
        var sql = $"SELECT TOP {top} {colList} FROM [{tabla}]";
        var registros = new List<Dictionary<string, object?>>();

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            registros.Add(row);
        }
        sw.Stop();

        return Results.Ok(new
        {
            exitoso = true,
            error = (string?)null,
            tiempoMs = sw.ElapsedMilliseconds,
            nombreTabla = tabla,
            baseDatos = conn.Database,
            total = registros.Count,
            columnas,
            registros
        });
    }
    catch (Exception ex)
    {
        sw.Stop();
        return Results.Ok(Falla(ex.Message, tabla, "", sw.ElapsedMilliseconds));
    }
});

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────

static async Task<List<ColMeta>> LeerColumnasAsync(SqlConnection conn, string tabla)
{
    var result = new List<ColMeta>();
    const string sql = """
        SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, ORDINAL_POSITION
        FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t ORDER BY ORDINAL_POSITION
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@t", tabla);
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        result.Add(new ColMeta(r.GetString(0), r.GetString(1), r.GetString(2) == "YES",
                               r.IsDBNull(3) ? null : r.GetInt32(3), r.GetInt32(4)));
    return result;
}

static object Falla(string error, string tabla, string bd = "", long ms = 0) => new
{
    exitoso = false, error, tiempoMs = ms, nombreTabla = tabla,
    baseDatos = bd, total = 0, columnas = Array.Empty<object>(), registros = Array.Empty<object>()
};

record ColMeta(string NombreColumna, string TipoDato, bool EsNullable, int? LongitudMax, int Ordinal)
{
    public string TipoDevExtreme => TipoDato switch
    {
        "int" or "bigint" or "smallint" or "tinyint" or "decimal" or "numeric"
            or "float" or "real" or "money" or "smallmoney" => "number",
        "bit"  => "boolean",
        "date" or "datetime" or "datetime2" or "smalldatetime" => "date",
        _ => "string"
    };
}
