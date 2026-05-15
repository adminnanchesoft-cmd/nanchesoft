using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Item : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? CategoryId { get; set; }
    public ItemCategory? Category { get; set; }

    public Guid? BrandId { get; set; }
    public ItemBrand? Brand { get; set; }

    public Guid? ModelId { get; set; }
    public ItemModel? Model { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public Guid? TaxId { get; set; }
    public Tax? Tax { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Producto";
    public decimal BasePrice { get; set; }
    public decimal BaseCost { get; set; }
    public bool ManagesInventory { get; set; } = true;
    public bool UsesLots { get; set; }
    public bool UsesSerials { get; set; }
    public bool IsSaleItem { get; set; } = true;
    public bool IsPurchaseItem { get; set; } = true;
}
