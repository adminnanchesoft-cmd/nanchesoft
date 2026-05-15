using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class DocumentSeries : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int CurrentNumber { get; set; }
    public int NumberLength { get; set; } = 8;
    public bool IsDefault { get; set; }

    public ICollection<DocumentFolio> DocumentFolios { get; set; } = new List<DocumentFolio>();
}
