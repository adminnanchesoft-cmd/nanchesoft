using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class InternalTransfer : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow.Date;
    public string SourceAccountType { get; set; } = "bank";
    public Guid SourceAccountId { get; set; }
    public string DestinationAccountType { get; set; } = "bank";
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "posted";
    public Guid? SourceMovementId { get; set; }
    public Guid? DestinationMovementId { get; set; }
}
