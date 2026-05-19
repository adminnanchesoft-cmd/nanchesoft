using System.Text.Json;

namespace Nanchesoft.Api.Endpoints;

public static class PostalCodeEndpoints
{
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

        // ── zippopotam.us — free, no token required ─────────────────
        // Returns: { "post code":"37000", "places":[{"place name":"Centro","state":"Guanajuato",...}] }
        try
        {
            var client = httpFactory.CreateClient("Copomex");
            var json = await client.GetStringAsync($"https://api.zippopotam.us/MX/{cp}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var colonias   = new List<string>();
            string estado  = string.Empty;

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

            if (colonias.Count == 0 && string.IsNullOrEmpty(estado))
                return Results.Ok(new PostalCodeResult(cp, [], string.Empty, string.Empty));

            colonias.Sort();
            // municipio: zippopotam.us no lo tiene — se deja vacío para que el usuario lo llene
            return Results.Ok(new PostalCodeResult(cp, colonias, string.Empty, estado));
        }
        catch
        {
            return Results.Ok(new PostalCodeResult(cp, [], string.Empty, string.Empty));
        }
    }

    private sealed record PostalCodeResult(
        string Cp,
        List<string> Colonias,
        string Municipio,
        string Estado);
}
