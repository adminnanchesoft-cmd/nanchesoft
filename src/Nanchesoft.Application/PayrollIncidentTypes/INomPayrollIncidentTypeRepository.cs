using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Application.PayrollIncidentTypes;

public interface INomPayrollIncidentTypeRepository
{
    Task<List<NomPayrollIncidentType>> ListAsync(Guid? tenantId, Guid? companyId, bool includeInactive, CancellationToken cancellationToken = default);
    Task<NomPayrollIncidentType?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid tenantId, Guid companyId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsUsedByIncidentsAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(NomPayrollIncidentType entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
