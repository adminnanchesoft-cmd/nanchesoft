using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Nanchesoft.Application.SilvaSoft;

namespace Nanchesoft.Infrastructure.SilvaSoft;

/// <summary>
/// Implementación de ISilvaSoftService.
/// Soporta dos modos de conexión según la configuración de la empresa:
///   · Directo: SqlClient → SQL Server (requiere IP pública o VPN)
///   · Agente:  HttpClient → Windows Service local → SQL Server (sin puertos expuestos)
/// </summary>
public sealed class SilvaSoftService : ISilvaSoftService
{
    private readonly ISilvaSoftConexionRepository _conexiones;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SilvaSoftService> _logger;

    public SilvaSoftService(
        ISilvaSoftConexionRepository conexiones,
        IHttpClientFactory httpFactory,
        ILogger<SilvaSoftService> logger)
    {
        _conexiones = conexiones;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    // ── ProbarConexionAsync ───────────────────────────────────────────────────

    public async Task<(bool Exitoso, string Mensaje, long TiempoMs)> ProbarConexionAsync(
        Guid empresaId, CancellationToken ct = default)
    {
        var agente = await _conexiones.ObtenerConfigAgenteAsync(empresaId, ct);
        if (agente.HasValue)
            return await ProbarViaAgenteAsync(agente.Value.AgentUrl, agente.Value.AgentToken, empresaId, ct);

        return await ProbarDirectoAsync(empresaId, ct);
    }

    // ── ObtenerComposicionesAsync ─────────────────────────────────────────────

    public async Task<SilvaSoftComposicionResultado> ObtenerComposicionesAsync(
        Guid empresaId, int top = 100, CancellationToken ct = default)
    {
        var agente = await _conexiones.ObtenerConfigAgenteAsync(empresaId, ct);
        if (agente.HasValue)
            return await ObtenerComposicionesViaAgenteAsync(agente.Value.AgentUrl, agente.Value.AgentToken, empresaId, top, ct);

        return await ObtenerComposicionesDirectoAsync(empresaId, top, ct);
    }

    // ── ObtenerClaseAsync ─────────────────────────────────────────────────────

    public async Task<SilvaSoftTablaResultado> ObtenerClaseAsync(
        Guid empresaId, int top = 2000, CancellationToken ct = default)
    {
        var agente = await _conexiones.ObtenerConfigAgenteAsync(empresaId, ct);
        if (agente.HasValue)
            return await ObtenerTablaViaAgenteAsync("clase", agente.Value.AgentUrl, agente.Value.AgentToken, empresaId, top, ct);

        return await ObtenerTablaDirectoAsync("clase", empresaId, top, ct);
    }

    // ── ObtenerTablaViaAgenteAsync / ObtenerTablaDirectoAsync (genérico) ─────────

    private async Task<SilvaSoftTablaResultado> ObtenerTablaViaAgenteAsync(
        string nombreTabla, string agentUrl, string agentToken, Guid empresaId, int top, CancellationToken ct)
    {
        _logger.LogInformation("ObtenerTabla [{Tabla}] [{EmpresaId}]: usando agente {Url}", nombreTabla, empresaId, agentUrl);
        var sw = Stopwatch.StartNew();
        try
        {
            using var http = CrearHttpAgente(agentUrl, agentToken);
            var res = await http.GetAsync($"/api/{nombreTabla}?top={top}", ct);
            sw.Stop();
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<AgentComposicionResponse>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken: ct);

                if (body is not null)
                {
                    return new SilvaSoftTablaResultado
                    {
                        Exitoso = body.Exitoso,
                        Error = body.Error,
                        NombreTabla = body.NombreTabla ?? nombreTabla,
                        BaseDatos = body.BaseDatos ?? string.Empty,
                        Total = body.Total,
                        TiempoMs = sw.ElapsedMilliseconds,
                        Columnas = body.Columnas?.Select(c => new SilvaSoftColumnaMeta
                        {
                            NombreColumna = c.NombreColumna,
                            TipoDato = c.TipoDato,
                            EsNullable = c.EsNullable,
                            LongitudMax = c.LongitudMax,
                            Ordinal = c.Ordinal
                        }).ToList() ?? [],
                        Registros = body.Registros?.Select(r => new SilvaSoftFilaDto
                        {
                            Campos = r.ToDictionary(
                                kv => kv.Key,
                                kv => kv.Value.ValueKind == JsonValueKind.Null ? (object?)null : kv.Value.GetRawText() as object)
                        }).ToList() ?? []
                    };
                }
            }
            var raw = await res.Content.ReadAsStringAsync(ct);
            return FallaTabla($"Error del agente ({(int)res.StatusCode}): {raw}", nombreTabla, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ObtenerTabla [{Tabla}] [{EmpresaId}] vía agente: error en {Ms}ms", nombreTabla, empresaId, sw.ElapsedMilliseconds);
            return FallaTabla($"No se pudo contactar el agente: {ex.Message}", nombreTabla, sw.ElapsedMilliseconds);
        }
    }

    private async Task<SilvaSoftTablaResultado> ObtenerTablaDirectoAsync(
        string nombreTabla, Guid empresaId, int top, CancellationToken ct)
    {
        var cs = await _conexiones.ObtenerCadenaConexionAsync(empresaId, ct);
        if (string.IsNullOrEmpty(cs))
            return FallaTabla("No hay configuración de conexión para esta empresa.", nombreTabla);

        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync(ct);

            var columnas = await DetectarColumnasAsync(conn, nombreTabla, ct);
            if (columnas.Count == 0)
            {
                sw.Stop();
                return FallaTabla($"La tabla '{nombreTabla}' no existe en '{conn.Database}'.", nombreTabla, sw.ElapsedMilliseconds);
            }

            var columnList = string.Join(", ", columnas.Select(c => $"[{c.NombreColumna}]"));
            var sql = $"SELECT TOP {top} {columnList} FROM [{nombreTabla}]";
            var registros = new List<SilvaSoftFilaDto>();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var dto = new SilvaSoftFilaDto();
                for (int i = 0; i < reader.FieldCount; i++)
                    dto.Campos[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                registros.Add(dto);
            }
            sw.Stop();

            return new SilvaSoftTablaResultado
            {
                Exitoso = true,
                NombreTabla = nombreTabla,
                BaseDatos = conn.Database,
                Columnas = columnas,
                Registros = registros,
                Total = registros.Count,
                TiempoMs = sw.ElapsedMilliseconds
            };
        }
        catch (SqlException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ObtenerTabla [{Tabla}] [{EmpresaId}]: error SQL {Number}", nombreTabla, empresaId, ex.Number);
            return FallaTabla($"Error SQL Server ({ex.Number}): {ex.Message}", nombreTabla, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ObtenerTabla [{Tabla}] [{EmpresaId}]: error inesperado", nombreTabla, empresaId);
            return FallaTabla($"Error inesperado: {ex.Message}", nombreTabla, sw.ElapsedMilliseconds);
        }
    }

    private static SilvaSoftTablaResultado FallaTabla(string error, string tabla, long ms = 0)
        => new() { Exitoso = false, Error = error, NombreTabla = tabla, TiempoMs = ms };

    // ── Modo agente ───────────────────────────────────────────────────────────

    private async Task<(bool Exitoso, string Mensaje, long TiempoMs)> ProbarViaAgenteAsync(
        string agentUrl, string agentToken, Guid empresaId, CancellationToken ct)
    {
        _logger.LogInformation("ProbarConexion [{EmpresaId}]: usando agente {Url}", empresaId, agentUrl);
        var sw = Stopwatch.StartNew();
        try
        {
            using var http = CrearHttpAgente(agentUrl, agentToken);
            var res = await http.GetAsync("/api/test", ct);
            sw.Stop();
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<AgentTestResponse>(cancellationToken: ct);
                if (body is not null)
                {
                    _logger.LogInformation("ProbarConexion [{EmpresaId}] vía agente: {Msg}", empresaId, body.Mensaje);
                    return (body.Exitoso, body.Mensaje, body.TiempoMs);
                }
            }
            var raw = await res.Content.ReadAsStringAsync(ct);
            return (false, $"Error del agente ({(int)res.StatusCode}): {raw}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ProbarConexion [{EmpresaId}] vía agente: error en {Ms}ms", empresaId, sw.ElapsedMilliseconds);
            return (false, $"No se pudo contactar el agente: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    private async Task<SilvaSoftComposicionResultado> ObtenerComposicionesViaAgenteAsync(
        string agentUrl, string agentToken, Guid empresaId, int top, CancellationToken ct)
    {
        const string tabla = "composicion";
        _logger.LogInformation("ObtenerComposiciones [{EmpresaId}]: usando agente {Url}", empresaId, agentUrl);
        var sw = Stopwatch.StartNew();
        try
        {
            using var http = CrearHttpAgente(agentUrl, agentToken);
            var res = await http.GetAsync($"/api/composicion?top={top}", ct);
            sw.Stop();
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<AgentComposicionResponse>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken: ct);

                if (body is not null)
                {
                    _logger.LogInformation(
                        "ObtenerComposiciones [{EmpresaId}] vía agente: {N} registros en {Ms}ms",
                        empresaId, body.Total, sw.ElapsedMilliseconds);

                    return new SilvaSoftComposicionResultado
                    {
                        Exitoso = body.Exitoso,
                        Error = body.Error,
                        NombreTabla = body.NombreTabla ?? tabla,
                        BaseDatos = body.BaseDatos ?? string.Empty,
                        Total = body.Total,
                        TiempoMs = sw.ElapsedMilliseconds,
                        Columnas = body.Columnas?.Select(c => new SilvaSoftColumnaMeta
                        {
                            NombreColumna = c.NombreColumna,
                            TipoDato = c.TipoDato,
                            EsNullable = c.EsNullable,
                            LongitudMax = c.LongitudMax,
                            Ordinal = c.Ordinal
                        }).ToList() ?? [],
                        Registros = body.Registros?.Select(r => new SilvaSoftComposicionDto
                        {
                            Campos = r.ToDictionary(
                                kv => kv.Key,
                                kv => kv.Value.ValueKind == JsonValueKind.Null ? (object?)null : kv.Value.GetRawText() as object)
                        }).ToList() ?? []
                    };
                }
            }
            var raw = await res.Content.ReadAsStringAsync(ct);
            return Falla($"Error del agente ({(int)res.StatusCode}): {raw}", tabla, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ObtenerComposiciones [{EmpresaId}] vía agente: error en {Ms}ms", empresaId, sw.ElapsedMilliseconds);
            return Falla($"No se pudo contactar el agente: {ex.Message}", tabla, sw.ElapsedMilliseconds);
        }
    }

    private HttpClient CrearHttpAgente(string agentUrl, string agentToken)
    {
        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(agentUrl.TrimEnd('/'));
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.Add("X-Agent-Token", agentToken);
        return http;
    }

    // ── Modo directo (SQL Server) ─────────────────────────────────────────────

    private async Task<(bool Exitoso, string Mensaje, long TiempoMs)> ProbarDirectoAsync(
        Guid empresaId, CancellationToken ct)
    {
        var cs = await _conexiones.ObtenerCadenaConexionAsync(empresaId, ct);
        if (string.IsNullOrEmpty(cs))
        {
            _logger.LogWarning("ProbarConexion [{EmpresaId}]: sin configuración", empresaId);
            return (false, "No hay configuración de conexión para esta empresa.", 0);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync(ct);
            sw.Stop();

            _logger.LogInformation(
                "ProbarConexion [{EmpresaId}]: conexión exitosa en {Ms}ms. Server={Server}, DB={DB}",
                empresaId, sw.ElapsedMilliseconds, conn.DataSource, conn.Database);

            return (true,
                $"Conexión exitosa. Servidor: {conn.DataSource} | Base de datos: {conn.Database} | Tiempo: {sw.ElapsedMilliseconds}ms",
                sw.ElapsedMilliseconds);
        }
        catch (SqlException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ProbarConexion [{EmpresaId}]: error SQL {Number} en {Ms}ms", empresaId, ex.Number, sw.ElapsedMilliseconds);
            return (false, $"Error SQL Server ({ex.Number}): {ex.Message}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ProbarConexion [{EmpresaId}]: error inesperado en {Ms}ms", empresaId, sw.ElapsedMilliseconds);
            return (false, $"Error de conexión: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    private async Task<SilvaSoftComposicionResultado> ObtenerComposicionesDirectoAsync(
        Guid empresaId, int top, CancellationToken ct)
    {
        const string nombreTabla = "composicion";

        var cs = await _conexiones.ObtenerCadenaConexionAsync(empresaId, ct);
        if (string.IsNullOrEmpty(cs))
        {
            _logger.LogWarning("ObtenerComposiciones [{EmpresaId}]: sin configuración", empresaId);
            return Falla("No hay configuración de conexión para esta empresa.", nombreTabla);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync(ct);

            _logger.LogInformation(
                "ObtenerComposiciones [{EmpresaId}]: conectado a {Server}/{DB}",
                empresaId, conn.DataSource, conn.Database);

            var columnas = await DetectarColumnasAsync(conn, nombreTabla, ct);
            if (columnas.Count == 0)
            {
                sw.Stop();
                _logger.LogWarning(
                    "ObtenerComposiciones [{EmpresaId}]: tabla '{Tabla}' no encontrada en {DB}",
                    empresaId, nombreTabla, conn.Database);
                return new SilvaSoftComposicionResultado
                {
                    Exitoso = false,
                    Error = $"La tabla '{nombreTabla}' no existe en la base de datos '{conn.Database}' de SilvaSoft.",
                    NombreTabla = nombreTabla,
                    BaseDatos = conn.Database,
                    TiempoMs = sw.ElapsedMilliseconds
                };
            }

            _logger.LogInformation(
                "ObtenerComposiciones [{EmpresaId}]: {NCol} columnas detectadas en '{Tabla}'",
                empresaId, columnas.Count, nombreTabla);

            var registros = await LeerRegistrosAsync(conn, nombreTabla, columnas, top, ct);
            sw.Stop();

            _logger.LogInformation(
                "ObtenerComposiciones [{EmpresaId}]: {N} registros leídos en {Ms}ms",
                empresaId, registros.Count, sw.ElapsedMilliseconds);

            return new SilvaSoftComposicionResultado
            {
                Exitoso = true,
                NombreTabla = nombreTabla,
                BaseDatos = conn.Database,
                Columnas = columnas,
                Registros = registros,
                Total = registros.Count,
                TiempoMs = sw.ElapsedMilliseconds
            };
        }
        catch (SqlException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ObtenerComposiciones [{EmpresaId}]: error SQL {Number} en {Ms}ms", empresaId, ex.Number, sw.ElapsedMilliseconds);
            return Falla($"Error SQL Server ({ex.Number}): {ex.Message}", nombreTabla, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ObtenerComposiciones [{EmpresaId}]: error inesperado en {Ms}ms", empresaId, sw.ElapsedMilliseconds);
            return Falla($"Error inesperado: {ex.Message}", nombreTabla, sw.ElapsedMilliseconds);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<List<SilvaSoftColumnaMeta>> DetectarColumnasAsync(
        SqlConnection conn, string nombreTabla, CancellationToken ct)
    {
        var result = new List<SilvaSoftColumnaMeta>();
        const string sql = """
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, ORDINAL_POSITION
            FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tabla ORDER BY ORDINAL_POSITION
            """;
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tabla", nombreTabla);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new SilvaSoftColumnaMeta
            {
                NombreColumna = reader.GetString(0),
                TipoDato = reader.GetString(1),
                EsNullable = reader.GetString(2) == "YES",
                LongitudMax = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Ordinal = reader.GetInt32(4)
            });
        }
        return result;
    }

    private static async Task<List<SilvaSoftComposicionDto>> LeerRegistrosAsync(
        SqlConnection conn, string nombreTabla, List<SilvaSoftColumnaMeta> columnas, int top, CancellationToken ct)
    {
        var columnList = string.Join(", ", columnas.Select(c => $"[{c.NombreColumna}]"));
        var sql = $"SELECT TOP {top} {columnList} FROM [{nombreTabla}]";
        var result = new List<SilvaSoftComposicionDto>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dto = new SilvaSoftComposicionDto();
            for (int i = 0; i < reader.FieldCount; i++)
                dto.Campos[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            result.Add(dto);
        }
        return result;
    }

    private static SilvaSoftComposicionResultado Falla(string error, string tabla, long ms = 0)
        => new() { Exitoso = false, Error = error, NombreTabla = tabla, TiempoMs = ms };
}

// ── Response types for agent HTTP calls (internal only) ───────────────────────

file sealed class AgentTestResponse
{
    [JsonPropertyName("exitoso")] public bool Exitoso { get; set; }
    [JsonPropertyName("mensaje")] public string Mensaje { get; set; } = string.Empty;
    [JsonPropertyName("tiempoMs")] public long TiempoMs { get; set; }
}

file sealed class AgentComposicionResponse
{
    [JsonPropertyName("exitoso")] public bool Exitoso { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("tiempoMs")] public long TiempoMs { get; set; }
    [JsonPropertyName("nombreTabla")] public string? NombreTabla { get; set; }
    [JsonPropertyName("baseDatos")] public string? BaseDatos { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("columnas")] public List<AgentColumnaMeta>? Columnas { get; set; }
    [JsonPropertyName("registros")] public List<Dictionary<string, JsonElement>>? Registros { get; set; }
}

file sealed class AgentColumnaMeta
{
    [JsonPropertyName("nombreColumna")] public string NombreColumna { get; set; } = string.Empty;
    [JsonPropertyName("tipoDato")] public string TipoDato { get; set; } = string.Empty;
    [JsonPropertyName("esNullable")] public bool EsNullable { get; set; }
    [JsonPropertyName("longitudMax")] public int? LongitudMax { get; set; }
    [JsonPropertyName("ordinal")] public int Ordinal { get; set; }
}
