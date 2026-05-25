namespace Nanchesoft.Domain.Entities;

public class AccountingGroupCompanyMember
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid GroupCompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
