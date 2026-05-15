using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductVariant : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid FinishedProductId { get; set; }
    public FinishedProduct? FinishedProduct { get; set; }

    public Guid ProductSizeRunId { get; set; }
    public ProductSizeRun? ProductSizeRun { get; set; }

    public Guid ProductSizeRunSizeId { get; set; }
    public ProductSizeRunSize? ProductSizeRunSize { get; set; }

    public int Sequence { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
