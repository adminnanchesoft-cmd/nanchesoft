using Nanchesoft.Application.PayrollIncidentTypes;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Services;

public sealed class NomPayrollIncidentTypeService : INomPayrollIncidentTypeService
{
    private readonly INomPayrollIncidentTypeRepository _repository;

    public NomPayrollIncidentTypeService(INomPayrollIncidentTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<NomPayrollIncidentTypeDto>> ListAsync(Guid? tenantId, Guid? companyId, bool includeInactive = false, CancellationToken cancellationToken = default)
        => (await _repository.ListAsync(tenantId, companyId, includeInactive, cancellationToken))
            .Select(NomPayrollIncidentTypeMapper.ToDto)
            .ToList();

    public async Task<NomPayrollIncidentTypeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.GetAsync(id, cancellationToken) is { } entity
            ? NomPayrollIncidentTypeMapper.ToDto(entity)
            : null;

    public async Task<(bool Success, string? Error, Guid? Id)> CreateAsync(
        NomPayrollIncidentTypeRequest request,
        Guid? tenantId,
        Guid? companyId,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        request.TenantId ??= tenantId;
        request.CompanyId ??= companyId;
        request.BranchId ??= branchId;

        if (!request.TenantId.HasValue || !request.CompanyId.HasValue)
            return (false, "No existe contexto de tenant/empresa para el tipo de incidencia.", null);

        var errors = NomPayrollIncidentTypeValidator.Validate(request);
        if (errors.Count > 0)
            return (false, string.Join(" ", errors), null);

        var code = NomPayrollIncidentTypeValidator.NormalizeCode(request.Code);
        if (await _repository.CodeExistsAsync(request.TenantId.Value, request.CompanyId.Value, code, cancellationToken: cancellationToken))
            return (false, "Ya existe un tipo de incidencia con ese codigo para la empresa.", null);

        var category = NomPayrollIncidentTypeValidator.NormalizeEnum(request.IncidentCategory);
        var entity = new NomPayrollIncidentType
        {
            TenantId = request.TenantId.Value,
            CompanyId = request.CompanyId.Value,
            BranchId = request.BranchId,
            Code = code,
            Name = NomPayrollIncidentTypeValidator.NormalizeText(request.Name),
            Description = NomPayrollIncidentTypeValidator.NormalizeText(request.Description),
            IncidentCategory = category,
            AffectType = NomPayrollIncidentTypeValidator.NormalizeEnum(request.AffectType),
            PayrollConceptType = NomPayrollIncidentTypeValidator.NormalizeEnum(request.PayrollConceptType),
            PayrollConceptId = request.PayrollConceptId,
            SatCode = NomPayrollIncidentTypeValidator.NormalizeCode(request.SatCode),
            Color = NomPayrollIncidentTypeValidator.NormalizeColor(request.Color, category),
            Icon = NomPayrollIncidentTypeValidator.NormalizeText(request.Icon),
            SortOrder = request.SortOrder,
            IsDiscount = request.IsDiscount,
            IsPerception = request.IsPerception,
            IsInformative = request.IsInformative,
            RequiresAmount = request.RequiresAmount,
            RequiresQuantity = request.RequiresQuantity,
            RequiresAuthorization = request.RequiresAuthorization,
            AppliesToPayroll = request.AppliesToPayroll,
            IsSystem = request.IsSystem,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        ApplyCategoryFlags(entity);
        _repository.Add(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return (true, null, entity.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Guid id, NomPayrollIncidentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(id, cancellationToken);
        if (entity is null)
            return (false, "No se encontro el tipo de incidencia.");

        request.TenantId = entity.TenantId;
        request.CompanyId = entity.CompanyId;
        request.BranchId ??= entity.BranchId;

        var errors = NomPayrollIncidentTypeValidator.Validate(request);
        if (errors.Count > 0)
            return (false, string.Join(" ", errors));

        var code = NomPayrollIncidentTypeValidator.NormalizeCode(request.Code);
        if (await _repository.CodeExistsAsync(entity.TenantId, entity.CompanyId, code, id, cancellationToken))
            return (false, "Ya existe otro tipo de incidencia con ese codigo para la empresa.");

        var category = NomPayrollIncidentTypeValidator.NormalizeEnum(request.IncidentCategory);
        entity.BranchId = request.BranchId;
        entity.Code = code;
        entity.Name = NomPayrollIncidentTypeValidator.NormalizeText(request.Name);
        entity.Description = NomPayrollIncidentTypeValidator.NormalizeText(request.Description);
        entity.IncidentCategory = category;
        entity.AffectType = NomPayrollIncidentTypeValidator.NormalizeEnum(request.AffectType);
        entity.PayrollConceptType = NomPayrollIncidentTypeValidator.NormalizeEnum(request.PayrollConceptType);
        entity.PayrollConceptId = request.PayrollConceptId ?? entity.PayrollConceptId;
        entity.SatCode = NomPayrollIncidentTypeValidator.NormalizeCode(request.SatCode);
        entity.Color = NomPayrollIncidentTypeValidator.NormalizeColor(request.Color, category);
        entity.Icon = NomPayrollIncidentTypeValidator.NormalizeText(request.Icon);
        entity.SortOrder = request.SortOrder;
        entity.IsDiscount = request.IsDiscount;
        entity.IsPerception = request.IsPerception;
        entity.IsInformative = request.IsInformative;
        entity.RequiresAmount = request.RequiresAmount;
        entity.RequiresQuantity = request.RequiresQuantity;
        entity.RequiresAuthorization = request.RequiresAuthorization;
        entity.AppliesToPayroll = request.AppliesToPayroll;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        ApplyCategoryFlags(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(id, cancellationToken);
        if (entity is null)
            return (false, "No se encontro el tipo de incidencia.");

        if (entity.IsSystem || await _repository.IsUsedByIncidentsAsync(id, cancellationToken))
        {
            entity.IsActive = false;
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = "web-api";
        }
        else
        {
            entity.IsActive = false;
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = "web-api";
        }

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";
        await _repository.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private static void ApplyCategoryFlags(NomPayrollIncidentType entity)
    {
        entity.IsDiscount = entity.IncidentCategory == "DEDUCCION" || entity.IsDiscount;
        entity.IsPerception = entity.IncidentCategory == "PERCEPCION" || entity.IsPerception;
        entity.IsInformative = entity.IncidentCategory == "INFORMATIVA" || entity.IsInformative;
    }
}
