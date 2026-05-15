using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ServiceNote : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? ServiceCatalogItemId { get; set; }
    public ServiceCatalogItem? ServiceCatalogItem { get; set; }

    public string Folio { get; set; } = string.Empty;
    public DateTime NoteDate { get; set; } = DateTime.UtcNow.Date;
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public string ServiceCodeSnapshot { get; set; } = string.Empty;
    public string ServiceNameSnapshot { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StartTimeText { get; set; }
    public string? EndTimeText { get; set; }
    public int BreakMinutes { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string PaymentStatus { get; set; } = "PENDIENTE";
    public string PaymentMethod { get; set; } = "POR_DEFINIR";
    public DateTime? PaymentDate { get; set; }
    public string? PaymentDestination { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
}
