using System.Text.Json;
using System.Text.Json.Serialization;

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

        try
        {
            var client = httpFactory.CreateClient("Copomex");
            var json = await client.GetStringAsync($"https://api.copomex.com/query/{cp}?type=CP&token=demo");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Error from copomex (cp not found)
            if (root.TryGetProperty("error", out var errorProp) && errorProp.GetBoolean())
                return Results.Ok(new PostalCodeResult(cp, [], string.Empty, string.Empty));

            var colonias = new List<string>();
            string municipio = string.Empty;
            string estado = string.Empty;

            if (root.TryGetProperty("response", out var resp) && resp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in resp.EnumerateArray())
                {
                    if (item.TryGetProperty("asentamiento", out var col))
                    {
                        var colName = col.GetString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(colName) && !colonias.Contains(colName))
                            colonias.Add(colName);
                    }
                    if (string.IsNullOrEmpty(municipio) && item.TryGetProperty("municipio", out var mun))
                        municipio = mun.GetString() ?? string.Empty;
                    if (string.IsNullOrEmpty(estado) && item.TryGetProperty("estado", out var est))
                        estado = est.GetString() ?? string.Empty;
                }
            }

            colonias.Sort();
            return Results.Ok(new PostalCodeResult(cp, colonias, municipio, estado));
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
