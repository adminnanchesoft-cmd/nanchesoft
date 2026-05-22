using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nanchesoft.Web.Services.Ai;

public sealed class AiAssistantApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public AiAssistantApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Nanchesoft.Api");
    }

    public async Task<AiAskResultDto> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/ia/ask",
            new AiAskRequestDto { Question = question },
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AiAskResultDto>(JsonOptions, cancellationToken);
        return result ?? new AiAskResultDto
        {
            Intent = AiIntentDto.Unknown,
            Answer = "No encontré esa información, intenta preguntar de otra forma.",
            Echo = question
        };
    }
}

public sealed class AiAskRequestDto
{
    public string Question { get; set; } = string.Empty;
}

public sealed class AiAskResultDto
{
    public AiIntentDto Intent { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string Echo { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public JsonElement? Data { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

public enum AiIntentDto
{
    Unknown = 0,
    TodaySummary = 1,
    TodaySales = 2,
    CustomerBalance = 3,
    PayrollOpenPeriod = 4,
    OverdueCustomers = 5
}
