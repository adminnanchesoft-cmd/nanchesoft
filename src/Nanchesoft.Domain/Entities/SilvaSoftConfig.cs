using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SilvaSoftConfig : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string ServerHost { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string DbUser { get; set; } = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public bool TrustServerCertificate { get; set; } = true;

    public string? Notes { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
