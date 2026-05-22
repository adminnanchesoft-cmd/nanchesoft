using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class AiConversation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Module { get; set; } = "hr_payroll";
    public string Status { get; set; } = "active";
    public string? LastIntent { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public int MessageCount { get; set; }

    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}
