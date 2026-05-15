
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Nanchesoft.Api.Endpoints;

public static class CatalogRouteAliasesEndpoints
{
    private static readonly string[] ProductCatalogKeys =
    {
        "material-families",
        "material-subfamilies",
        "material-items",
        "product-components",
        "families",
        "lines",
        "styles",
        "brands",
        "models",
        "finished-products",
        "finished-product-materials",
        "product-consumption-profiles"
    };

    public static IEndpointRouteBuilder MapCatalogRouteAliases(this IEndpointRouteBuilder app)
    {
        foreach (var key in ProductCatalogKeys)
        {
            MapAlias(app, key);
        }

        return app;
    }

    private static void MapAlias(IEndpointRouteBuilder app, string key)
    {
        // GET list / definition
        app.MapMethods($"/api/catalogs/{key}", new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }, async context =>
        {
            var target = $"/api/products/{key}{context.Request.QueryString}";
            context.Response.Redirect(target, permanent: false, preserveMethod: true);
            await Task.CompletedTask;
        });

        // GET by id and nested operations
        app.MapMethods($"/api/catalogs/{key}/{{**rest}}", new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }, async context =>
        {
            var rest = context.Request.RouteValues["rest"]?.ToString() ?? string.Empty;
            var target = $"/api/products/{key}/{rest}{context.Request.QueryString}";
            context.Response.Redirect(target, permanent: false, preserveMethod: true);
            await Task.CompletedTask;
        });
    }
}
