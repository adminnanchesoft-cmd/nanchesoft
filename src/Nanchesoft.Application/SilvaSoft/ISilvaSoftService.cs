namespace Nanchesoft.Application.SilvaSoft;

/// <summary>
/// Servicio de integración con SilvaSoft SQL Server.
///
/// ─── DISEÑO MULTI-TENANT ────────────────────────────────────────────────────
/// Todos los métodos reciben empresaId. El servicio obtiene las credenciales
/// de la empresa desde ISilvaSoftConexionRepository y abre su propia conexión.
/// Cada empresa puede tener un servidor SilvaSoft distinto.
///
/// ─── ARQUITECTURA FUTURA: AGENTE LOCAL ──────────────────────────────────────
/// SilvaSoft corre on-premise y Nanchesoft está en la nube. Opciones:
///
///   Opción A (implementación actual):
///     Nanchesoft → SqlClient → SQL Server SilvaSoft (requiere IP pública o VPN)
///
///   Opción B (implementación futura):
///     Nanchesoft → HTTP → SilvaSoftAgente (Windows Service local)
///                          └→ SqlClient → SQL Server SilvaSoft
///
///   Para cambiar a opción B, basta con registrar en DI:
///     services.AddScoped&lt;ISilvaSoftService, RemoteSilvaSoftAgentService&gt;();
///   sin tocar ninguna otra capa.
///
/// ─── SINCRONIZACIÓN FUTURA ──────────────────────────────────────────────────
///   - Manual: usuario dispara desde la pantalla de Nanchesoft
///   - Automática: IHostedService o Hangfire con cron configurable por tenant
///   - Incremental: trackear fecha_ultima_sincronizacion por entidad
///   - Bidireccional: importar de SilvaSoft + exportar precios/stock a SilvaSoft
///   - Logs: cada operación queda registrada en silvasoft_sync_logs con duración
///   - Reintentos: política de retry con backoff exponencial (Polly)
/// </summary>
public interface ISilvaSoftService
{
    /// <summary>
    /// Prueba la conexión SQL Server de la empresa.
    /// Devuelve: éxito, mensaje descriptivo y tiempo de respuesta en ms.
    /// </summary>
    Task<(bool Exitoso, string Mensaje, long TiempoMs)> ProbarConexionAsync(
        Guid empresaId, CancellationToken ct = default);

    /// <summary>
    /// Consulta TOP {top} registros de la tabla composicion en SilvaSoft.
    /// Detecta columnas dinámicamente — no falla si la estructura varía.
    /// </summary>
    Task<SilvaSoftComposicionResultado> ObtenerComposicionesAsync(
        Guid empresaId, int top = 100, CancellationToken ct = default);

    /// <summary>
    /// Consulta TOP {top} registros de la tabla clase en SilvaSoft.
    /// La tabla clase contiene las subfamilias de materiales.
    /// PK: claseid. FK al padre: composicionid.
    /// </summary>
    Task<SilvaSoftTablaResultado> ObtenerClaseAsync(
        Guid empresaId, int top = 2000, CancellationToken ct = default);

    /// <summary>
    /// Consulta TOP {top} registros de la tabla Fraccion en SilvaSoft.
    /// Fraccion representa las operaciones productivas (fases de fabricación).
    /// </summary>
    Task<SilvaSoftTablaResultado> ObtenerFraccionesAsync(
        Guid empresaId, int top = 2000, CancellationToken ct = default);

    /// <summary>
    /// Consulta TOP {top} registros de la tabla Fraccion_Cadena en SilvaSoft.
    /// Fraccion_Cadena define la regla de auto-replicación de destajos entre fases.
    /// </summary>
    Task<SilvaSoftTablaResultado> ObtenerFraccionCadenaAsync(
        Guid empresaId, int top = 5000, CancellationToken ct = default);

}
