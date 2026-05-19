using Nanchesoft.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Nanchesoft.Api.Endpoints;

public static class PostalCodeEndpoints
{
    // In-memory cache — each CP looked up once per server restart
    private static readonly ConcurrentDictionary<string, PostalCodeResult> _cache = new();

    public static IEndpointRouteBuilder MapPostalCodeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/organization/postal-code/{cp}", LookupAsync).WithTags("Organization");
        return app;
    }

    private static async Task<IResult> LookupAsync(string cp, NanchesoftDbContext db)
    {
        cp = cp.Trim();
        if (cp.Length != 5 || !cp.All(char.IsDigit))
            return Results.BadRequest(new { message = "El código postal debe ser de 5 dígitos." });

        if (_cache.TryGetValue(cp, out var cached))
            return Results.Ok(cached);

        // Query local SEPOMEX table — full official data from Correos de México
        var rows = await db.Database
            .SqlQueryRaw<SepomexRow>(
                "SELECT cp, colonia, municipio, estado, ciudad FROM org.sepomex_cp WHERE cp = {0} ORDER BY colonia",
                cp)
            .ToListAsync();

        if (rows.Count == 0)
        {
            var empty = new PostalCodeResult(cp, [], string.Empty, string.Empty);
            _cache.TryAdd(cp, empty);
            return Results.Ok(empty);
        }

        var colonias  = rows.Select(r => r.Colonia).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c).ToList();
        var municipio = rows[0].Ciudad is { Length: > 0 } ? rows[0].Ciudad : rows[0].Municipio;
        var estado    = rows[0].Estado;

        var result = new PostalCodeResult(cp, colonias, municipio, estado);
        _cache.TryAdd(cp, result);
        return Results.Ok(result);
    }

    // Raw SQL projection
    private sealed class SepomexRow
    {
        public string Cp        { get; set; } = "";
        public string Colonia   { get; set; } = "";
        public string Municipio { get; set; } = "";
        public string Estado    { get; set; } = "";
        public string Ciudad    { get; set; } = "";
    }

    private sealed record PostalCodeResult(
        string Cp,
        List<string> Colonias,
        string Municipio,
        string Estado);
}
