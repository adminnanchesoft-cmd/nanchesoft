namespace Nanchesoft.Domain.Entities;

public class CfdiDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string SourceModule { get; set; } = string.Empty;
    public Guid SourceDocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string ReceiverRfc { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Uuid { get; set; }
    public DateTime? StampedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
