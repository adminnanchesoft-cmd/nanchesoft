using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Audit;

public sealed class AuditApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AuditApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<AuditChangeLogRowDto>> GetChangeLogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<AuditChangeLogRowDto>>("/api/audit/change-log") ?? new();
    }

    public async Task<List<DocumentLogRowDto>> GetDocumentLogAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<DocumentLogRowDto>>("/api/audit/document-log") ?? new();
    }
}

public sealed class AuditChangeLogRowDto
{
    public Guid AuditLogId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public sealed class DocumentLogRowDto
{
    public Guid DocumentKey { get; set; }
    public string SourceModule { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
}
