using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Application.PayrollIncidentTypes;

public static class NomPayrollIncidentTypeMapper
{
    public static NomPayrollIncidentTypeDto ToDto(NomPayrollIncidentType entity) => new()
    {
        NomPayrollIncidentTypeId = entity.Id,
        TenantId = entity.TenantId,
        CompanyId = entity.CompanyId,
        CompanyName = entity.Company?.Name ?? string.Empty,
        BranchId = entity.BranchId,
        BranchName = entity.Branch?.Name ?? string.Empty,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        IncidentCategory = entity.IncidentCategory,
        AffectType = entity.AffectType,
        PayrollConceptType = entity.PayrollConceptType,
        PayrollConceptId = entity.PayrollConceptId,
        SatCode = entity.SatCode,
        Color = entity.Color,
        Icon = entity.Icon,
        SortOrder = entity.SortOrder,
        IsDiscount = entity.IsDiscount,
        IsPerception = entity.IsPerception,
        IsInformative = entity.IsInformative,
        RequiresAmount = entity.RequiresAmount,
        RequiresQuantity = entity.RequiresQuantity,
        RequiresAuthorization = entity.RequiresAuthorization,
        AppliesToPayroll = entity.AppliesToPayroll,
        IsSystem = entity.IsSystem,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy
    };
}
