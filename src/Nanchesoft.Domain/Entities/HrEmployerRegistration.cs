using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class HrEmployerRegistration : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? RiskClass { get; set; }
    public string? State { get; set; }
    public string? Notes { get; set; }
}
