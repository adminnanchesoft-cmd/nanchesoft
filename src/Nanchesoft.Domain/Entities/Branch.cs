using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Branch : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }

    public Tenant? Tenant { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}