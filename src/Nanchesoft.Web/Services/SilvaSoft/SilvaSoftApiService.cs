using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.SilvaSoft;

// ─────────────────────────────────────────────────────────────────────────────
//  SilvaSoftApiService — cliente HTTP hacia los endpoints /api/silvasoft/*
//  Actúa como fachada para todos los componentes Blazor que necesiten datos
//  de la integración SilvaSoft.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class SilvaSoftApiService
{
    private readonly IHttpClientFactory _http;
    private readonly AppState _appState;
    private readonly AuthState _authState;
    private readonly TenantContextAccessor _tenantAccessor;

    // ApiTenantScopeHandler lee TenantContextAccessor vía AsyncLocal.
    // Ese AsyncLocal NO persiste entre eventos SignalR, así que lo refrescamos
    // aquí usando los estados del circuit-scope (que sí tienen los valores reales).
    private HttpClient Client
    {
        get
        {
            _tenantAccessor.Set(
                _appState.CurrentTenantId,
                _appState.CurrentCompanyId,
                _appState.CurrentBranchId,
                _authState.UserId,
                _authState.IsPlatformOwner);
            return _http.CreateClient("Nanchesoft.Api");
        }
    }

    public SilvaSoftApiService(
        IHttpClientFactory http,
        AppState appState,
        AuthState authState,
        TenantContextAccessor tenantAccessor)
    {
        _http = http;
        _appState = appState;
        _authState = authState;
        _tenantAccessor = tenantAccessor;
    }

    // ── Configuración ─────────────────────────────────────────────────────────

    public async Task<SilvaSoftConexionDto?> ObtenerConfigAsync()
    {
        try { return await Client.GetFromJsonAsync<SilvaSoftConexionDto>("/api/silvasoft/config"); }
        catch { return null; }
    }

    public async Task<(bool Ok, string Mensaje)> GuardarConfigAsync(SilvaSoftConexionRequest req)
    {
        var res = await Client.PostAsJsonAsync("/api/silvasoft/config", req);
        if (res.IsSuccessStatusCode) return (true, "Configuración guardada correctamente.");
        var body = await res.Content.ReadAsStringAsync();
        try
        {
            var err = JsonSerializer.Deserialize<JsonElement>(body);
            return (false, err.TryGetProperty("message", out var m) ? m.GetString() ?? body : body);
        }
        catch { return (false, body); }
    }

    // ── Conexión ──────────────────────────────────────────────────────────────

    public async Task<SilvaSoftConexionTestDto> ProbarConexionAsync()
    {
        try
        {
            var res = await Client.GetAsync("/api/silvasoft/probar-conexion");
            if (res.IsSuccessStatusCode)
                return await res.Content.ReadFromJsonAsync<SilvaSoftConexionTestDto>()
                       ?? new SilvaSoftConexionTestDto { Exitoso = false, Mensaje = "Respuesta vacía de la API." };
            var body = await res.Content.ReadAsStringAsync();
            return new SilvaSoftConexionTestDto { Exitoso = false, Mensaje = $"Error {(int)res.StatusCode}: {body}" };
        }
        catch (Exception ex)
        {
            return new SilvaSoftConexionTestDto { Exitoso = false, Mensaje = $"Sin respuesta de la API: {ex.Message}" };
        }
    }

    // ── Composición ───────────────────────────────────────────────────────────

    public async Task<SilvaSoftComposicionResultado?> ObtenerComposicionAsync(int top = 100)
    {
        try
        {
            return await Client.GetFromJsonAsync<SilvaSoftComposicionResultado>(
                $"/api/silvasoft/composicion?top={top}");
        }
        catch { return null; }
    }

    public async Task<SilvaSoftVistaImportacionDto?> ObtenerVistaImportacionAsync()
    {
        try
        {
            return await Client.GetFromJsonAsync<SilvaSoftVistaImportacionDto>(
                "/api/silvasoft/composicion/vista-importacion");
        }
        catch { return null; }
    }

    public async Task<(bool Ok, SilvaSoftImportResultadoDto? Resultado, string Error)> ImportarFamiliasMaterialesAsync()
    {
        try
        {
            var res = await Client.PostAsync("/api/silvasoft/composicion/importar", null);
            if (res.IsSuccessStatusCode)
            {
                var dto = await res.Content.ReadFromJsonAsync<SilvaSoftImportResultadoDto>();
                return (true, dto, string.Empty);
            }
            var body = await res.Content.ReadAsStringAsync();
            try
            {
                var err = JsonSerializer.Deserialize<JsonElement>(body);
                var msg = err.TryGetProperty("detail", out var d) ? d.GetString()
                        : err.TryGetProperty("message", out var m) ? m.GetString()
                        : body;
                return (false, null, msg ?? body);
            }
            catch { return (false, null, body); }
        }
        catch (Exception ex)
        {
            return (false, null, $"Sin respuesta de la API: {ex.Message}");
        }
    }

    // ── Subfamilias (clase) ───────────────────────────────────────────────────

    public async Task<SilvaSoftTablaResultado?> ObtenerClaseAsync(int top = 200)
    {
        try
        {
            return await Client.GetFromJsonAsync<SilvaSoftTablaResultado>(
                $"/api/silvasoft/clase?top={top}");
        }
        catch { return null; }
    }

    public async Task<SilvaSoftVistaImportacionSubfamiliasDto?> ObtenerVistaImportacionSubfamiliasAsync()
    {
        try
        {
            return await Client.GetFromJsonAsync<SilvaSoftVistaImportacionSubfamiliasDto>(
                "/api/silvasoft/clase/vista-importacion");
        }
        catch { return null; }
    }

    public async Task<(bool Ok, SilvaSoftImportSubfamiliasResultadoDto? Resultado, string Error)> ImportarSubfamiliasAsync()
    {
        try
        {
            var res = await Client.PostAsync("/api/silvasoft/clase/importar", null);
            if (res.IsSuccessStatusCode)
            {
                var dto = await res.Content.ReadFromJsonAsync<SilvaSoftImportSubfamiliasResultadoDto>();
                return (true, dto, string.Empty);
            }
            var body = await res.Content.ReadAsStringAsync();
            try
            {
                var err = JsonSerializer.Deserialize<JsonElement>(body);
                var msg = err.TryGetProperty("detail", out var d) ? d.GetString()
                        : err.TryGetProperty("message", out var m) ? m.GetString()
                        : body;
                return (false, null, msg ?? body);
            }
            catch { return (false, null, body); }
        }
        catch (Exception ex) { return (false, null, $"Sin respuesta de la API: {ex.Message}"); }
    }

    // ── Logs ──────────────────────────────────────────────────────────────────

    public async Task<SilvaSoftSyncLogPagina?> ObtenerLogsAsync(int pagina = 1, int tamano = 50)
    {
        try
        {
            return await Client.GetFromJsonAsync<SilvaSoftSyncLogPagina>(
                $"/api/silvasoft/logs?pagina={pagina}&tamano={tamano}");
        }
        catch { return null; }
    }
}

// ─── DTOs del lado Web (espejo de Application.SilvaSoft) ─────────────────────

public sealed class SilvaSoftConexionDto
{
    public Guid Id { get; set; }
    public string NombreServidor { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public bool Activo { get; set; }
    [JsonPropertyName("fechaUltimaSincronizacion")]
    public DateTime? FechaUltimaSincronizacion { get; set; }
    public string? Notas { get; set; }
    public bool UsarAgente { get; set; }
    public string? AgentUrl { get; set; }
}

public sealed class SilvaSoftConexionRequest
{
    public string NombreServidor { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
    public bool UsarAgente { get; set; }
    public string? AgentUrl { get; set; }
    /// <summary>Dejar vacío para no modificar el token existente.</summary>
    public string? AgentToken { get; set; }
}

public sealed class SilvaSoftConexionTestDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public long TiempoMs { get; set; }
}

public sealed class SilvaSoftColumnaMeta
{
    public string NombreColumna { get; set; } = string.Empty;
    public string TipoDato { get; set; } = string.Empty;
    public bool EsNullable { get; set; }
    public int? LongitudMax { get; set; }
    public int Ordinal { get; set; }
    public string TipoDevExtreme { get; set; } = "string";
}

public sealed class SilvaSoftComposicionDto
{
    public Dictionary<string, JsonElement> Campos { get; set; } = [];
}

public sealed class SilvaSoftComposicionResultado
{
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
    public long TiempoMs { get; set; }
    public List<SilvaSoftColumnaMeta> Columnas { get; set; } = [];
    public List<SilvaSoftComposicionDto> Registros { get; set; } = [];
    public int Total { get; set; }
    public string NombreTabla { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
}

public sealed class SilvaSoftVistaImportacionDto
{
    public int TotalEnSilvaSoft { get; set; }
    public int YaExistentesEnNanchesoft { get; set; }
    public int NuevosParaImportar { get; set; }
    public int RegistrosInvalidos { get; set; }
    public List<SilvaSoftMapeoColumna> Mapeo { get; set; } = [];
    public List<SilvaSoftRegistroVistaPrevia> VistaPrevia { get; set; } = [];
}

public sealed class SilvaSoftMapeoColumna
{
    public string ColumnaOrigen { get; set; } = string.Empty;
    public string CampoDestino { get; set; } = string.Empty;
    public string TipoDato { get; set; } = string.Empty;
    public bool Mapeado { get; set; }
    public string? Nota { get; set; }
}

public sealed class SilvaSoftRegistroVistaPrevia
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Razon { get; set; }
}

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

// ─── DTOs tabla clase (subfamilias) ──────────────────────────────────────────

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

public sealed class SilvaSoftFilaDto
{
    public Dictionary<string, JsonElement> Campos { get; set; } = [];
}

public sealed class SilvaSoftRegistroVistaSubfamiliaPrevia
{
    public Guid? ClaseId { get; set; }
    public Guid? ComposicionId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Razon { get; set; }
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
