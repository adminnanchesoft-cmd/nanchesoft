using System.ComponentModel.DataAnnotations;
using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialSupplierAssignment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid MaterialItemId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public Guid? CurrencyId { get; set; }

    [MaxLength(80)]
    public string SupplierItemCode { get; set; } = string.Empty;

    [MaxLength(220)]
    public string SupplierItemName { get; set; } = string.Empty;

    public decimal ConversionFactor { get; set; } = 1m;
    public decimal AuthorizedCost { get; set; }
    public decimal LastCost { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public bool IsPreferred { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    [MaxLength(1200)]
    public string Notes { get; set; } = string.Empty;

    public MaterialItem? MaterialItem { get; set; }
    public Supplier? Supplier { get; set; }
    public Unit? PurchaseUnit { get; set; }
    public Currency? Currency { get; set; }

    public ICollection<MaterialSupplierCostHistory> CostHistory { get; set; } = new List<MaterialSupplierCostHistory>();
}
