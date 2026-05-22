using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class AiMessage : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid ConversationId { get; set; }
    public AiConversation? Conversation { get; set; }

    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? Endpoint { get; set; }
    public string? DataJson { get; set; }
    public int SequenceNumber { get; set; }
}
