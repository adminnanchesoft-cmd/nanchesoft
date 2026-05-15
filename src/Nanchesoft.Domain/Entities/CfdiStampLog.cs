namespace Nanchesoft.Domain.Entities;

public class CfdiStampLog
{
    public Guid Id { get; set; }
    public Guid CfdiDocumentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RawResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
