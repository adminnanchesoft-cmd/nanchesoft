using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CustomerLegalEntity : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string FiscalRegime { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string CfdiUse { get; set; } = string.Empty;
    public string FiscalSituationPdfPath { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
