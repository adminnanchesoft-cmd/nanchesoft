using System.Net.Http.Json;
using System.Text.Json;

namespace Nanchesoft.Web.Services.SilvaSoft;

public sealed class SilvaSoftApiService
{
    private readonly IHttpClientFactory _http;

    public SilvaSoftApiService(IHttpClientFactory http) => _http = http;

    private HttpClient Client => _http.CreateClient("Nanchesoft.Api");

    // ── Config ──────────────────────────────────────────────────────
    public async Task<SilvaSoftConfigDto?> GetConfigAsync()
    {
        try { return await Client.GetFromJsonAsync<SilvaSoftConfigDto>("/api/silvasoft/config"); }
        catch { return null; }
    }

    public async Task<(bool ok, string message)> SaveConfigAsync(SilvaSoftConfigRequest req)
    {
        var res = await Client.PostAsJsonAsync("/api/silvasoft/config", req);
        if (res.IsSuccessStatusCode) return (true, "Guardado.");
        var body = await res.Content.ReadAsStringAsync();
        return (false, body);
    }

    public async Task<(bool ok, string message)> TestConnectionAsync()
    {
        var res = await Client.GetAsync("/api/silvasoft/test-connection");
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        var ok = json.TryGetProperty("success", out var s) && s.GetBoolean();
        var msg = json.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;
        return (ok, msg);
    }

    // ── Families ────────────────────────────────────────────────────
    public async Task<List<SilvaSoftFamilyRow>> GetFamiliesAsync()
    {
        try { return await Client.GetFromJsonAsync<List<SilvaSoftFamilyRow>>("/api/silvasoft/families") ?? []; }
        catch { return []; }
    }

    public async Task<SilvaSoftImportResult?> ImportFamiliesAsync()
    {
        var res = await Client.PostAsJsonAsync("/api/silvasoft/families/import", new { });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<SilvaSoftImportResult>();
    }

    // ── Composition ─────────────────────────────────────────────────
    public async Task<List<SilvaSoftCompositionRow>> GetCompositionAsync(string? styleFilter = null, int page = 1)
    {
        var url = $"/api/silvasoft/composition?page={page}&pageSize=200";
        if (!string.IsNullOrWhiteSpace(styleFilter))
            url += $"&style={Uri.EscapeDataString(styleFilter)}";
        try { return await Client.GetFromJsonAsync<List<SilvaSoftCompositionRow>>(url) ?? []; }
        catch { return []; }
    }

    public async Task<SilvaSoftImportResult?> ImportCompositionAsync(string? styleFilter = null)
    {
        var req = new { StyleFilter = styleFilter };
        var res = await Client.PostAsJsonAsync("/api/silvasoft/composition/import", req);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<SilvaSoftImportResult>();
    }

    // ── Logs ────────────────────────────────────────────────────────
    public async Task<SilvaSoftLogPage?> GetLogsAsync(int page = 1, int pageSize = 50)
    {
        try { return await Client.GetFromJsonAsync<SilvaSoftLogPage>($"/api/silvasoft/logs?page={page}&pageSize={pageSize}"); }
        catch { return null; }
    }
}

// ── DTOs ─────────────────────────────────────────────────────────

public sealed class SilvaSoftConfigDto
{
    public Guid Id { get; set; }
    public string ServerHost { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string DbUser { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public bool TrustServerCertificate { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsActive { get; set; }
}

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

public sealed class SilvaSoftImportResult
{
    public bool Success { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Total { get; set; }
}

public sealed class SilvaSoftLogPage
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<SilvaSoftSyncLogDto> Rows { get; set; } = [];
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
