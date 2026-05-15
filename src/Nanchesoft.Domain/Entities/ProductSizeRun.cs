using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductSizeRun : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Compatibilidad funcional con Orange/Silvasoft.
    // En Orange existían Clave, Clave2, Consumos, PuntoMedio y T1..T30/M1..M30.
    // En Nanchesoft se normaliza: los T/M/N/F/P viven en ProductSizeRunSize.
    public string LegacyKey { get; set; } = string.Empty;
    public string SecondaryKey { get; set; } = string.Empty;
    public string ConsumptionMode { get; set; } = "I";
    public bool IsUniqueSizeRun { get; set; }
    public int SizeCount { get; set; }
    public int? MiddlePoint { get; set; }

    public ICollection<ProductSizeRunSize> Sizes { get; set; } = new List<ProductSizeRunSize>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
