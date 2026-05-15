using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class DocumentFolio : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public Guid SeriesId { get; set; }
    public DocumentSeries? Series { get; set; }

    public int CurrentNumber { get; set; }
}
