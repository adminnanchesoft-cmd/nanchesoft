using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Plan : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxCompanies { get; set; }
    public int MaxBranches { get; set; }
    public decimal PriceMonthly { get; set; }

    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}