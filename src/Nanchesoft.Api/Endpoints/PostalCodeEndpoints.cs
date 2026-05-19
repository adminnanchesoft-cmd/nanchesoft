using System.Collections.Concurrent;
using System.Text.Json;

namespace Nanchesoft.Api.Endpoints;

public static class PostalCodeEndpoints
{
    // In-memory cache — survives for the lifetime of the process
    private static readonly ConcurrentDictionary<string, PostalCodeResult> _cache = new();

    public static IEndpointRouteBuilder MapPostalCodeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/organization/postal-code/{cp}", LookupAsync).WithTags("Organization");
        return app;
    }

    private static async Task<IResult> LookupAsync(string cp, IHttpClientFactory httpFactory)
    {
        cp = cp.Trim();
        if (cp.Length != 5 || !cp.All(char.IsDigit))
            return Results.BadRequest(new { message = "El código postal debe ser de 5 dígitos." });

        if (_cache.TryGetValue(cp, out var cached))
            return Results.Ok(cached);

        var client = httpFactory.CreateClient("Copomex");

        // ── Two parallel lookups ─────────────────────────────────────
        var zipTask       = FetchZippopotamAsync(client, cp);
        var nominatimTask = FetchNominatimAsync(client, cp);

        await Task.WhenAll(zipTask, nominatimTask);

        var (colonias, estado) = zipTask.Result;
        var municipio          = nominatimTask.Result;

        // If neither returned data → not found
        if (colonias.Count == 0 && string.IsNullOrEmpty(estado) && string.IsNullOrEmpty(municipio))
            return Results.Ok(new PostalCodeResult(cp, [], string.Empty, string.Empty));

        // Estado from Nominatim is sometimes more accurate for Mexico
        if (string.IsNullOrEmpty(estado) && !string.IsNullOrEmpty(nominatimTask.Result))
        {
            // Already handled below — keep estado from zippopotam
        }

        var result = new PostalCodeResult(cp, colonias, municipio, estado);
        _cache.TryAdd(cp, result);
        return Results.Ok(result);
    }

    // ── zippopotam.us: colonias + estado ────────────────────────────
    private static async Task<(List<string> Colonias, string Estado)> FetchZippopotamAsync(
        HttpClient client, string cp)
    {
        try
        {
            var json = await client.GetStringAsync($"https://api.zippopotam.us/MX/{cp}");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var colonias = new List<string>();
            var estado   = string.Empty;

            if (root.TryGetProperty("places", out var places) && places.ValueKind == JsonValueKind.Array)
            {
                foreach (var place in places.EnumerateArray())
                {
                    if (place.TryGetProperty("place name", out var pn))
                    {
                        var col = pn.GetString() ?? "";
                        if (!string.IsNullOrWhiteSpace(col) && !colonias.Contains(col))
                            colonias.Add(col);
                    }
                    if (string.IsNullOrEmpty(estado) && place.TryGetProperty("state", out var st))
                        estado = st.GetString() ?? "";
                }
            }

            colonias.Sort();
            return (colonias, estado);
        }
        catch
        {
            return ([], string.Empty);
        }
    }

    // ── Nominatim (OpenStreetMap): municipio ─────────────────────────
    private static async Task<string> FetchNominatimAsync(HttpClient client, string cp)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get,
                $"https://nominatim.openstreetmap.org/search?postalcode={cp}&country=mx&format=json&addressdetails=1&limit=1");
            req.Headers.TryAddWithoutValidation("User-Agent", "NanchesoftERP/1.0 (contact@nanchesoft.com)");

            var resp = await client.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return string.Empty;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                return string.Empty;

            var first = root[0];
            if (!first.TryGetProperty("address", out var addr)) return string.Empty;

            // Prefer "city", fall back to "county" (which is the municipio in Mexico)
            if (addr.TryGetProperty("city", out var city) && !string.IsNullOrWhiteSpace(city.GetString()))
                return city.GetString()!;

            if (addr.TryGetProperty("county", out var county) && !string.IsNullOrWhiteSpace(county.GetString()))
                return county.GetString()!;

            if (addr.TryGetProperty("town", out var town) && !string.IsNullOrWhiteSpace(town.GetString()))
                return town.GetString()!;

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed record PostalCodeResult(
        string Cp,
        List<string> Colonias,
        string Municipio,
        string Estado);
}
