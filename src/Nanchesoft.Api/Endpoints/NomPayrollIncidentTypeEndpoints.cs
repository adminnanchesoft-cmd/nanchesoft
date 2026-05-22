using Nanchesoft.Application.PayrollIncidentTypes;

namespace Nanchesoft.Api.Endpoints;

public static class NomPayrollIncidentTypeEndpoints
{
    public static IEndpointRouteBuilder MapNomPayrollIncidentTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payroll/incident-types").WithTags("NomPayrollIncidentTypes");

        group.MapGet("/", async (HttpContext httpContext, INomPayrollIncidentTypeService service, bool includeInactive = false) =>
        {
            var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
            var companyId = ApiTenantScope.ResolveCompanyId(httpContext);
            return Results.Ok(await service.ListAsync(tenantId, companyId, includeInactive));
        });

        group.MapGet("/{id:guid}", async (Guid id, INomPayrollIncidentTypeService service) =>
        {
            var row = await service.GetAsync(id);
            return row is null
                ? Results.NotFound(new { message = "No se encontro el tipo de incidencia." })
                : Results.Ok(row);
        });

        group.MapPost("/", async (HttpContext httpContext, NomPayrollIncidentTypeRequest request, INomPayrollIncidentTypeService service) =>
        {
            var result = await service.CreateAsync(
                request,
                ApiTenantScope.ResolveTenantId(httpContext),
                ApiTenantScope.ResolveCompanyId(httpContext),
                ApiTenantScope.ResolveBranchId(httpContext));

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
