using Nanchesoft.Application.PayrollIncidentTypes;

namespace Nanchesoft.Api.Endpoints;

public static class NomPayrollIncidentTypeEndpoints
{
    public static IEndpointRouteBuilder MapNomPayrollIncidentTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payroll/incident-types").WithTags("NomPayrollIncidentTypes");

        group.MapGet("/", async (HttpContext httpContext, INomPayrollIncidentTypeService service, bool includeInactive = false) =>
        {
            var scope = ApiTenantScope.RequireScope(httpContext);
            if (!scope.IsValid) return scope.Error!;
            return Results.Ok(await service.ListAsync(scope.TenantId, scope.CompanyId, includeInactive));
        });

        group.MapGet("/{id:guid}", async (HttpContext httpContext, Guid id, INomPayrollIncidentTypeService service) =>
        {
            var scope = ApiTenantScope.RequireScope(httpContext);
            if (!scope.IsValid) return scope.Error!;
            var row = await service.GetAsync(id);
            if (row is null || row.TenantId != scope.TenantId)
                return Results.NotFound(new { message = "No se encontro el tipo de incidencia en tu tenant." });
            return Results.Ok(row);
        });

        group.MapPost("/", async (HttpContext httpContext, NomPayrollIncidentTypeRequest request, INomPayrollIncidentTypeService service) =>
        {
            var scope = ApiTenantScope.RequireScope(httpContext);
            if (!scope.IsValid) return scope.Error!;

            var result = await service.CreateAsync(
                request,
                scope.TenantId,
                scope.CompanyId,
                scope.BranchId);

            return result.Success
                ? Results.Ok(new { success = true, id = result.Id })
                : Results.BadRequest(new { message = result.Error });
        });

        group.MapPut("/{id:guid}", async (Guid id, NomPayrollIncidentTypeRequest request, INomPayrollIncidentTypeService service) =>
        {
            var result = await service.UpdateAsync(id, request);
            return result.Success
                ? Results.Ok(new { success = true })
                : Results.BadRequest(new { message = result.Error });
        });

        group.MapDelete("/{id:guid}", async (Guid id, INomPayrollIncidentTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success
                ? Results.Ok(new { success = true })
                : Results.BadRequest(new { message = result.Error });
        });

        return app;
    }
}
