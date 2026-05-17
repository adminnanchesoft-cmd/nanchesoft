using System.Net.Http.Json;
using System.Text.Json;

namespace Nanchesoft.Web.Services.HumanResources;

public sealed class PayrollMvpApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PayrollMvpApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PayrollMvpImportResult> ImportEmployeesFromExcelAsync(Stream stream, string fileName)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        var response = await client.PostAsync("/api/hr/employees/import", content);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PayrollMvpImportResult>() ?? new();
    }

    public async Task<PayrollMvpImportResult> ImportPunchesFromExcelAsync(Stream stream, string fileName)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        var response = await client.PostAsync("/api/hr/time-clock/import", content);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PayrollMvpImportResult>() ?? new();
    }

    public async Task<PayrollMvpOperationResult> GenerateAttendanceSummariesAsync(Guid periodId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"/api/payroll/periods/{periodId}/generate-summaries", null);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PayrollMvpOperationResult>() ?? new();
    }

    public async Task<PayrollMvpOperationResult> GenerateIncidentsAsync(Guid periodId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"/api/payroll/periods/{periodId}/generate-incidents", null);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PayrollMvpOperationResult>() ?? new();
    }

    public async Task<PayrollMvpCalculateResult> CalculatePayrollRunAsync(Guid runId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"/api/payroll/runs/{runId}/calculate", null);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PayrollMvpCalculateResult>() ?? new();
    }

    public async Task<List<PayrollPeriodSimple>> GetPeriodsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<PayrollPeriodSimple>>("/api/payroll/periods") ?? [];
    }

    public async Task<List<PayrollRunSimple>> GetRunsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<PayrollRunSimple>>("/api/payroll/runs") ?? [];
    }

    public async Task<PayrollRunSimple?> CreateRunAsync(Guid periodId, string folio)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsJsonAsync("/api/payroll/runs", new
        {
            payrollPeriodId = periodId,
            folio,
            runDate = DateTime.UtcNow,
            status = "draft",
            isActive = true
        });
        await EnsureSuccessAsync(response);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var newId = created.GetProperty("id").GetGuid();
        var runs = await GetRunsAsync();
        return runs.FirstOrDefault(x => x.PayrollRunId == newId);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            string message;
            try
            {
                var doc = JsonDocument.Parse(body);
                message = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? body : body;
            }
            catch
            {
                message = body;
            }
            throw new InvalidOperationException(message);
        }
    }
}

public sealed class PayrollMvpImportResult
{
    public bool Success { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = [];
}

public sealed class PayrollMvpOperationResult
{
    public bool Success { get; set; }
    public int Created { get; set; }
    public int Employees { get; set; }
    public int Days { get; set; }
}

public sealed class PayrollMvpCalculateResult
{
    public bool Success { get; set; }
    public int Employees { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
}

public sealed class PayrollPeriodSimple
{
    public Guid? PayrollPeriodId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PeriodType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
}

public sealed class PayrollRunSimple
{
    public Guid PayrollRunId { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime RunDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
}
