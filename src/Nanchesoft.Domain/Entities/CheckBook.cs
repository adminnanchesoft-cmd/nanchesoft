using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CheckBook : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public int FolioStart { get; set; }
    public int FolioEnd { get; set; }
    public int NextFolio { get; set; }
    public string Notes { get; set; } = string.Empty;
}
