using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CartSession : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? B2BAccountId { get; set; }
    public B2BAccount? B2BAccount { get; set; }

    public Guid? B2BUserId { get; set; }
    public B2BUser? B2BUser { get; set; }

    public Guid? SellerUserId { get; set; }

    public string Channel { get; set; } = "b2b_web";
    public string Status { get; set; } = "active";

    public Guid? WarehouseId { get; set; }
    public Guid? PriceListId { get; set; }
    public ItemPriceList? PriceList { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public string? NotesGlobal { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }

    public Guid? ConvertedSalesOrderId { get; set; }
    public SalesOrder? ConvertedSalesOrder { get; set; }

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);
    public DateTime LastEventAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartLine> Lines { get; set; } = new List<CartLine>();
}
