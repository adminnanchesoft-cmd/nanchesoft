using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Ai;

public sealed class AiAssistantApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly AuthState _authState;
    private readonly AppState _appState;

    public AiAssistantApiService(IHttpClientFactory httpClientFactory, AuthState authState, AppState appState)
    {
        _httpClient = httpClientFactory.CreateClient("Nanchesoft.Api");
        _authState = authState;
        _appState = appState;
    }

    private void ApplyHeaders(HttpRequestMessage message)
    {
        Add(message, "X-Tenant-Id", _appState.CurrentTenantId ?? _authState.TenantId);
        Add(message, "X-Company-Id", _appState.CurrentCompanyId ?? _authState.CompanyId);
        Add(message, "X-Branch-Id", _appState.CurrentBranchId ?? _authState.BranchId);
        Add(message, "X-User-Id", _authState.UserId);
        message.Headers.Remove("X-Is-Platform-Owner");
        message.Headers.Add("X-Is-Platform-Owner", _authState.IsPlatformOwner ? "true" : "false");

        static void Add(HttpRequestMessage m, string h, Guid? v)
        {
            m.Headers.Remove(h);
            if (v.HasValue && v.Value != Guid.Empty) m.Headers.Add(h, v.Value.ToString("D"));
        }
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(method, uri);
        if (body is not null)
        {
            msg.Content = JsonContent.Create(body, options: JsonOptions);
        }
        ApplyHeaders(msg);
        using var resp = await _httpClient.SendAsync(msg, ct);
        resp.EnsureSuccessStatusCode();
        if (typeof(T) == typeof(Unit) || resp.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return default;
        }
        return await resp.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    public async Task<AiChatResultDto> ChatAsync(string question, Guid? conversationId = null, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<AiChatResultDto>(HttpMethod.Post, "/api/ia/chat",
            new AiChatRequestDto { Question = question, ConversationId = conversationId },
            cancellationToken);
        return result ?? new AiChatResultDto
        {
            Intent = "unknown",
            Answer = "No encontré información suficiente para responder eso."
        };
    }

    public async Task<List<AiConversationDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
        => await SendAsync<List<AiConversationDto>>(HttpMethod.Get, "/api/ia/conversations", null, cancellationToken) ?? new();

    public async Task<AiConversationDto?> CreateConversationAsync(CancellationToken cancellationToken = default)
        => await SendAsync<AiConversationDto>(HttpMethod.Post, "/api/ia/conversations", new { }, cancellationToken);

    public async Task<List<AiMessageDto>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
        => await SendAsync<List<AiMessageDto>>(HttpMethod.Get, $"/api/ia/conversations/{conversationId}/messages", null, cancellationToken) ?? new();

    public async Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
        => await SendAsync<Unit>(HttpMethod.Delete, $"/api/ia/conversations/{conversationId}", null, cancellationToken);

    public async Task<List<AiSuggestionGroupDto>> GetSuggestionsAsync(CancellationToken cancellationToken = default)
        => await SendAsync<List<AiSuggestionGroupDto>>(HttpMethod.Get, "/api/ia/suggestions", null, cancellationToken) ?? new();

    private sealed class Unit { }
}

public sealed class AiChatRequestDto
{
    public string Question { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
}

public sealed class AiChatResultDto
{
    public Guid ConversationId { get; set; }
    public string ConversationTitle { get; set; } = string.Empty;
    public string Intent { get; set; } = "unknown";
    public string Module { get; set; } = "hr_payroll";
    public string Answer { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? Route { get; set; }
    public JsonElement? Data { get; set; }
    public List<string> Suggestions { get; set; } = new();
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
    public JsonElement? Data { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AiSuggestionGroupDto
{
    public string Title { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
}
