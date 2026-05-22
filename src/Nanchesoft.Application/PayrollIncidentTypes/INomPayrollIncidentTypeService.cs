namespace Nanchesoft.Application.PayrollIncidentTypes;

public interface INomPayrollIncidentTypeService
{
    Task<List<NomPayrollIncidentTypeDto>> ListAsync(Guid? tenantId, Guid? companyId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<NomPayrollIncidentTypeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, Guid? Id)> CreateAsync(NomPayrollIncidentTypeRequest request, Guid? tenantId, Guid? companyId, Guid? branchId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(Guid id, NomPayrollIncidentTypeRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
