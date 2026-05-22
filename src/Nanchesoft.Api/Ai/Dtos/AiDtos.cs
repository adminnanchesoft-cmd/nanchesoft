namespace Nanchesoft.Api.Ai.Dtos;

public sealed class AiChatRequest
{
    public string Question { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
}

public sealed class AiChatResponse
{
    public Guid ConversationId { get; set; }
    public string ConversationTitle { get; set; } = string.Empty;
    public string Intent { get; set; } = "unknown";
    public string Module { get; set; } = "hr_payroll";
    public string Answer { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? Route { get; set; }
    public object? Data { get; set; }
    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();
}

public sealed class AiConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LastIntent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int MessageCount { get; set; }
}

public sealed class AiMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? Endpoint { get; set; }
    public object? Data { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AiSuggestionGroupDto
{
    public string Title { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
}
