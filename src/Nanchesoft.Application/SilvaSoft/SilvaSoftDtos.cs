namespace Nanchesoft.Application.SilvaSoft;

// ─────────────────────────────────────────────────────────────────────────────
//  DTOs de Integración SilvaSoft
//  Usados por ISilvaSoftService, ISilvaSoftConexionRepository y los endpoints
//  de la API. Todos son POCO puros sin dependencias de infraestructura.
// ─────────────────────────────────────────────────────────────────────────────

// ─── Conexión ─────────────────────────────────────────────────────────────────

/// <summary>Datos de conexión SilvaSoft, sin exponer contraseña ni token del agente.</summary>
public sealed class SilvaSoftConexionDto
{
    public Guid Id { get; set; }
    public string NombreServidor { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime? FechaUltimaSincronizacion { get; set; }
    public string? Notas { get; set; }
    public bool UsarAgente { get; set; }
    /// <summary>URL del agente. Se expone (no es secreto). El token NO se expone.</summary>
    public string? AgentUrl { get; set; }
}

/// <summary>Request para crear o actualizar la configuración de conexión.</summary>
public sealed class SilvaSoftConexionRequest
{
    public string NombreServidor { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    /// <summary>Dejar vacío o null para no modificar la contraseña existente.</summary>
    public string? Password { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
    public bool UsarAgente { get; set; }
    public string? AgentUrl { get; set; }
    /// <summary>Dejar vacío o null para no modificar el token existente.</summary>
    public string? AgentToken { get; set; }
}

// ─── Tabla genérica de SilvaSoft (composicion / clase) ───────────────────────

/// <summary>DTO genérico para una fila de cualquier tabla de SilvaSoft leída dinámicamente.</summary>
public sealed class SilvaSoftFilaDto
{
    public Dictionary<string, object?> Campos { get; set; } = [];
}

/// <summary>Resultado genérico de lectura de una tabla de SilvaSoft.</summary>
public sealed class SilvaSoftTablaResultado
{
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
    public long TiempoMs { get; set; }
    public List<SilvaSoftColumnaMeta> Columnas { get; set; } = [];
    public List<SilvaSoftFilaDto> Registros { get; set; } = [];
    public int Total { get; set; }
    public string NombreTabla { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
}

// ─── Composición (tabla SQL Server de SilvaSoft) ──────────────────────────────

/// <summary>
/// DTO dinámico para un registro de composicion en SilvaSoft.
/// Se usa Dictionary porque la estructura exacta varía por versión/cliente de SilvaSoft.
/// Req. 16: si la tabla tiene columnas distintas a las esperadas, no falla.
/// </summary>
public sealed class SilvaSoftComposicionDto
{
    /// <summary>Clave: nombre de columna en SQL Server. Valor: dato de la fila.</summary>
    public Dictionary<string, object?> Campos { get; set; } = [];
}

/// <summary>Metadata de una columna detectada dinámicamente vía INFORMATION_SCHEMA.</summary>
public sealed class SilvaSoftColumnaMeta
{
    public string NombreColumna { get; set; } = string.Empty;
    public string TipoDato { get; set; } = string.Empty;
    public bool EsNullable { get; set; }
    public int? LongitudMax { get; set; }
    public int Ordinal { get; set; }

    /// <summary>Tipo DevExtreme equivalente para la columna del DataGrid.</summary>
    public string TipoDevExtreme => TipoDato switch
    {
        "int" or "bigint" or "smallint" or "tinyint" or "decimal" or "numeric"
            or "float" or "real" or "money" or "smallmoney" => "number",
        "bit" => "boolean",
        "date" or "datetime" or "datetime2" or "smalldatetime" => "date",
        _ => "string"
    };
}

/// <summary>Resultado completo de ObtenerComposicionesAsync: datos + esquema + métricas.</summary>
public sealed class SilvaSoftComposicionResultado
{
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
    /// <summary>Tiempo de respuesta del SQL Server en milisegundos (incluye red + query).</summary>
    public long TiempoMs { get; set; }
    /// <summary>Columnas detectadas dinámicamente.</summary>
    public List<SilvaSoftColumnaMeta> Columnas { get; set; } = [];
    public List<SilvaSoftComposicionDto> Registros { get; set; } = [];
    public int Total { get; set; }
    /// <summary>Nombre de la tabla consultada (para mostrar en UI).</summary>
    public string NombreTabla { get; set; } = "composicion";
    /// <summary>Base de datos del servidor SilvaSoft.</summary>
    public string BaseDatos { get; set; } = string.Empty;
}

// ─── Vista previa de importación ──────────────────────────────────────────────

/// <summary>
/// Vista previa antes de importar: muestra mapeo, duplicados y lo que se importaría.
/// Req. 10: NO importa nada — sólo analiza y presenta al usuario.
/// </summary>
public sealed class SilvaSoftVistaImportacionDto
{
    public int TotalEnSilvaSoft { get; set; }
    public int YaExistentesEnNanchesoft { get; set; }
    public int NuevosParaImportar { get; set; }
    public int RegistrosInvalidos { get; set; }
    /// <summary>Mapeo columna SilvaSoft → campo MaterialFamily Nanchesoft.</summary>
    public List<SilvaSoftMapeoColumna> Mapeo { get; set; } = [];
    /// <summary>Primeros 50 registros clasificados (nuevo/duplicado/inválido).</summary>
    public List<SilvaSoftRegistroVistaPrevia> VistaPrevia { get; set; } = [];
}

public sealed class SilvaSoftMapeoColumna
{
    /// <summary>Nombre de la columna en la tabla composicion de SilvaSoft.</summary>
    public string ColumnaOrigen { get; set; } = string.Empty;
    /// <summary>Campo equivalente en MaterialFamily de Nanchesoft (vacío = sin mapeo).</summary>
    public string CampoDestino { get; set; } = string.Empty;
    public string TipoDato { get; set; } = string.Empty;
    public bool Mapeado => !string.IsNullOrEmpty(CampoDestino);
    public string? Nota { get; set; }
}

public sealed class SilvaSoftRegistroVistaPrevia
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    /// <summary>"nuevo" | "duplicado" | "invalido"</summary>
    public string Estado { get; set; } = string.Empty;
    public string? Razon { get; set; }
}

// ─── Resultado de importación ─────────────────────────────────────────────────

/// <summary>Resultado de la importación real de familias de materiales desde SilvaSoft.</summary>
public sealed class SilvaSoftImportResultadoDto
{
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
    public int RegistrosLeidos { get; set; }
    public int RegistrosImportados { get; set; }
    public int RegistrosOmitidos { get; set; }
    public int RegistrosInvalidos { get; set; }
    public long TiempoMs { get; set; }
    public List<string> Detalles { get; set; } = [];
}

// ─── Logs de sincronización ────────────────────────────────────────────────────

public sealed class SilvaSoftSyncLogDto
{
    public Guid Id { get; set; }
    public string Operacion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int RegistrosLeidos { get; set; }
    public int RegistrosImportados { get; set; }
    public int RegistrosOmitidos { get; set; }
    public string? MensajeError { get; set; }
    public DateTime Iniciado { get; set; }
    public DateTime? Terminado { get; set; }
    public long? DuracionMs { get; set; }
    public string? DisparadoPor { get; set; }
}

public sealed class SilvaSoftSyncLogPagina
{
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public List<SilvaSoftSyncLogDto> Registros { get; set; } = [];
}

// ─── Vista previa e importación de Subfamilias (tabla clase) ──────────────────

public sealed class SilvaSoftRegistroVistaSubfamiliaPrevia
{
    public Guid? ClaseId { get; set; }
    public Guid? ComposicionId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    /// <summary>"nuevo" | "duplicado" | "invalido"</summary>
    public string Estado { get; set; } = string.Empty;
    public string? Razon { get; set; }
    /// <summary>Nombre de la familia padre encontrada en Nanchesoft (o null si no se encontró).</summary>
    public string? FamiliaPadre { get; set; }
}

public sealed class SilvaSoftVistaImportacionSubfamiliasDto
{
    public int TotalEnSilvaSoft { get; set; }
    public int YaExistentesEnNanchesoft { get; set; }
    public int NuevosParaImportar { get; set; }
    public int RegistrosInvalidos { get; set; }
    public int SinFamiliaPadre { get; set; }
    public List<SilvaSoftMapeoColumna> Mapeo { get; set; } = [];
    public List<SilvaSoftRegistroVistaSubfamiliaPrevia> VistaPrevia { get; set; } = [];
}

public sealed class SilvaSoftImportSubfamiliasResultadoDto
{
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
    public int RegistrosLeidos { get; set; }
    public int RegistrosImportados { get; set; }
    public int RegistrosOmitidos { get; set; }
    public int RegistrosInvalidos { get; set; }
    public int SinFamiliaPadre { get; set; }
    public long TiempoMs { get; set; }
    public List<string> Detalles { get; set; } = [];
}
