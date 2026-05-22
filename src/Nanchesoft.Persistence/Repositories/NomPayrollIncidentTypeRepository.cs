using Microsoft.EntityFrameworkCore;
using Nanchesoft.Application.PayrollIncidentTypes;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Repositories;

public sealed class NomPayrollIncidentTypeRepository : INomPayrollIncidentTypeRepository
{
    private readonly NanchesoftDbContext _db;

    public NomPayrollIncidentTypeRepository(NanchesoftDbContext db)
    {
        _db = db;
    }

    public Task<List<NomPayrollIncidentType>> ListAsync(Guid? tenantId, Guid? companyId, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _db.NomPayrollIncidentTypes
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Where(x => !x.IsDeleted);

        if (tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        if (companyId.HasValue)
            query = query.Where(x => x.CompanyId == companyId.Value);

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return query
            .OrderBy(x => x.IncidentCategory)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<NomPayrollIncidentType?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.NomPayrollIncidentTypes
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<bool> CodeExistsAsync(Guid tenantId, Guid companyId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = code.ToUpperInvariant();
        return _db.NomPayrollIncidentTypes.AnyAsync(x =>
            x.TenantId == tenantId
            && x.CompanyId == companyId
            && x.Code.ToUpper() == normalized
            && !x.IsDeleted
            && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }

    public Task<bool> IsUsedByIncidentsAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.EmployeeIncidents.AnyAsync(x => x.PayrollIncidentTypeId == id && !x.IsDeleted, cancellationToken);

    public void Add(NomPayrollIncidentType entity) => _db.NomPayrollIncidentTypes.Add(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _db.SaveChangesAsync(cancellationToken);
}
