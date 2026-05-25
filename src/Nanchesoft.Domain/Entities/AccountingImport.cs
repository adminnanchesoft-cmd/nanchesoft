namespace Nanchesoft.Domain.Entities;

public class AccountingImport
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? GroupCompanyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Status { get; set; } = "pending";
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
