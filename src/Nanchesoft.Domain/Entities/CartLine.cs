using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CartLine : BaseEntity
{
    public Guid CartSessionId { get; set; }
    public CartSession? CartSession { get; set; }

    public Guid ProductId { get; set; }

    public Guid? VariantId { get; set; }

    public int LineOrder { get; set; }

    public decimal Qty { get; set; }
    public decimal UnitPriceBase { get; set; }
    public decimal UnitPriceApplied { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal LineSubTotal { get; set; }
    public decimal LineTax { get; set; }
    public decimal LineTotal { get; set; }

    public string? AppliedRulesJson { get; set; }
    public string? Comment { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
}
