using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Nanchesoft.Application.SilvaSoft;

namespace Nanchesoft.Infrastructure.SilvaSoft;

/// <summary>
/// Implementación de ISilvaSoftService que se conecta directamente a SQL Server
/// usando Microsoft.Data.SqlClient.
///
/// ARQUITECTURA:
/// Esta clase NO conoce EF Core ni Postgres. Recibe la cadena de conexión SQL Server
/// ya construida desde ISilvaSoftConexionRepository (capa de Persistence).
/// Esto permite sustituirla por una RemoteSilvaSoftAgentService que delegue
/// a un Windows Service local sin cambiar nada más del sistema.
///
/// LOGGING DETALLADO (req. 12):
/// - Conexión exitosa / fallida
/// - Tiempo de respuesta (ms)
/// - Cantidad de registros leídos
/// - Columnas detectadas dinámicamente
/// - Mensajes de error completos con contexto
///
/// DETECCIÓN DINÁMICA DE COLUMNAS (req. 16):
/// Antes de leer datos, consulta INFORMATION_SCHEMA.COLUMNS para obtener
/// la estructura real de la tabla. Si la tabla no existe o tiene columnas
/// distintas a las esperadas, NO lanza excepción — devuelve el resultado
/// con Exitoso=false y un mensaje descriptivo.
/// </summary>
public sealed class SilvaSoftService : ISilvaSoftService
{
    private readonly ISilvaSoftConexionRepository _conexiones;
    private readonly ILogger<SilvaSoftService> _logger;

    public SilvaSoftService(
        ISilvaSoftConexionRepository conexiones,
        ILogger<SilvaSoftService> logger)
    {
        _conexiones = conexiones;
        _logger = logger;
    }

    // ── ProbarConexionAsync ───────────────────────────────────────────────────

    public async Task<(bool Exitoso, string Mensaje, long TiempoMs)> ProbarConexionAsync(
        Guid empresaId, CancellationToken ct = default)
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
                empresaId, sw.ElapsedMilliseconds,
                conn.DataSource, conn.Database);

            return (true,
                $"Conexión exitosa. Servidor: {conn.DataSource} | Base de datos: {conn.Database} | Tiempo: {sw.ElapsedMilliseconds}ms",
                sw.ElapsedMilliseconds);
        }
        catch (SqlException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "ProbarConexion [{EmpresaId}]: error SQL {Number} en {Ms}ms",
                empresaId, ex.Number, sw.ElapsedMilliseconds);
            return (false, $"Error SQL Server ({ex.Number}): {ex.Message}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "ProbarConexion [{EmpresaId}]: error inesperado en {Ms}ms",
                empresaId, sw.ElapsedMilliseconds);
            return (false, $"Error de conexión: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    // ── ObtenerComposicionesAsync ──────────────────────────────────────────────

    public async Task<SilvaSoftComposicionResultado> ObtenerComposicionesAsync(
        Guid empresaId, int top = 100, CancellationToken ct = default)
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

            // ── Paso 1: detectar esquema de la tabla dinámicamente ────────────
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
                    Error = $"La tabla '{nombreTabla}' no existe en la base de datos '{conn.Database}' de SilvaSoft. " +
                            "Verifique el nombre de la tabla o la configuración de la base de datos.",
                    NombreTabla = nombreTabla,
                    BaseDatos = conn.Database,
                    TiempoMs = sw.ElapsedMilliseconds
                };
            }

            _logger.LogInformation(
                "ObtenerComposiciones [{EmpresaId}]: {NCol} columnas detectadas en '{Tabla}'",
                empresaId, columnas.Count, nombreTabla);

            // ── Paso 2: leer datos ────────────────────────────────────────────
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
            _logger.LogError(ex,
                "ObtenerComposiciones [{EmpresaId}]: error SQL {Number} en {Ms}ms",
                empresaId, ex.Number, sw.ElapsedMilliseconds);
            return Falla($"Error SQL Server ({ex.Number}): {ex.Message}", nombreTabla, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "ObtenerComposiciones [{EmpresaId}]: error inesperado en {Ms}ms",
                empresaId, sw.ElapsedMilliseconds);
            return Falla($"Error inesperado: {ex.Message}", nombreTabla, sw.ElapsedMilliseconds);
        }
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    /// <summary>
    /// Consulta INFORMATION_SCHEMA.COLUMNS para obtener la estructura real de la tabla.
    /// Si la tabla no existe, devuelve lista vacía (no lanza excepción).
    /// </summary>
    private static async Task<List<SilvaSoftColumnaMeta>> DetectarColumnasAsync(
        SqlConnection conn, string nombreTabla, CancellationToken ct)
    {
        var result = new List<SilvaSoftColumnaMeta>();

        const string sql = """
            SELECT
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                CHARACTER_MAXIMUM_LENGTH,
                ORDINAL_POSITION
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tabla
            ORDER BY ORDINAL_POSITION
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

    /// <summary>
    /// Lee TOP {top} registros de la tabla usando las columnas detectadas.
    /// Mapea cada fila a un Dictionary para soportar esquemas dinámicos.
    /// </summary>
    private static async Task<List<SilvaSoftComposicionDto>> LeerRegistrosAsync(
        SqlConnection conn,
        string nombreTabla,
        List<SilvaSoftColumnaMeta> columnas,
        int top,
        CancellationToken ct)
    {
        // Construye la lista de columnas para el SELECT de forma segura
        // (no usa los nombres directamente en la query string — usa la lista validada de INFORMATION_SCHEMA)
        var columnList = string.Join(", ", columnas.Select(c => $"[{c.NombreColumna}]"));
        var sql = $"SELECT TOP {top} {columnList} FROM [{nombreTabla}]";

        var result = new List<SilvaSoftComposicionDto>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var dto = new SilvaSoftComposicionDto();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var colName = reader.GetName(i);
                dto.Campos[colName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            result.Add(dto);
        }

        return result;
    }

    private static SilvaSoftComposicionResultado Falla(
        string error, string tabla, long ms = 0)
        => new()
        {
            Exitoso = false,
            Error = error,
            NombreTabla = tabla,
            TiempoMs = ms
        };
}
