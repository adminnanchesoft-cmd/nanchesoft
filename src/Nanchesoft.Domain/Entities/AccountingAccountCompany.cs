namespace Nanchesoft.Domain.Entities;

public class AccountingAccountCompany
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AccountId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool? Applies { get; set; }
    public string? ImportSource { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
