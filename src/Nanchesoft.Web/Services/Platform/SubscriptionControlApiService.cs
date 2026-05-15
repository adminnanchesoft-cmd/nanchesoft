using System.Net.Http.Json;
using System.Text.Json;

using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.Platform;

public sealed class SubscriptionControlApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthState _authState;

    public SubscriptionControlApiService(IHttpClientFactory httpClientFactory, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _authState = authState;
    }

    public async Task<SubscriptionDashboardSummaryDto> GetDashboardAsync(int year, int month)
    {
        var client = CreatePlatformClient();
        return await client.GetFromJsonAsync<SubscriptionDashboardSummaryDto>($"/api/subscription/control/dashboard?year={year}&month={month}")
               ?? new SubscriptionDashboardSummaryDto { Year = year, Month = month };
    }

    public async Task GenerateMonthAsync(GenerateSubscriptionChargesRequestDto request)
    {
        var client = CreatePlatformClient();
        var response = await client.PostAsJsonAsync("/api/subscription/control/generate-month", request);
        await EnsureSuccessAsync(response);
    }

    public async Task UpdateChargeAsync(Guid id, UpdateSubscriptionChargeRequestDto request)
    {
        var client = CreatePlatformClient();
        var response = await client.PutAsJsonAsync($"/api/subscription/control/charges/{id}", request);
        await EnsureSuccessAsync(response);
    }

    public async Task RegisterPaymentAsync(Guid id, RegisterSubscriptionPaymentRequestDto request)
    {
        var client = CreatePlatformClient();
        var response = await client.PostAsJsonAsync($"/api/subscription/control/charges/{id}/register-payment", request);
        await EnsureSuccessAsync(response);
    }

    private HttpClient CreatePlatformClient()
    {
        EnsurePlatformAccess();

        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        client.DefaultRequestHeaders.Remove("X-Nanchesoft-Platform-Owner");
        client.DefaultRequestHeaders.Add("X-Nanchesoft-Platform-Owner", _authState.IsPlatformOwner ? "true" : "false");
        return client;
    }

    private void EnsurePlatformAccess()
    {
        if (!_authState.IsPlatformOwner)
        {
            throw new InvalidOperationException("Solo el propietario de la plataforma puede administrar tenants, planes y suscripciones SaaS.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("La API devolvió un error sin detalle.");

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                throw new InvalidOperationException(message.GetString() ?? "La API devolvió un error.");
            }
        }
        catch (JsonException)
        {
        }

        throw new InvalidOperationException(content);
    }
}

public sealed class SubscriptionDashboardSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int PartialCount { get; set; }
    public int PaidCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CompensationAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public List<SubscriptionChargeRowDto> Rows { get; set; } = new();
}

public sealed class SubscriptionChargeRowDto
{
    public Guid SubscriptionChargeId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string ChargeMonth { get; set; } = string.Empty;
    public int BillingYear { get; set; }
    public int BillingMonth { get; set; }
    public DateTime ChargeDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PlanPriceMonthly { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CompensationAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class GenerateSubscriptionChargesRequestDto
{
    public Guid? TenantId { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public DateTime? ChargeDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateSubscriptionChargeRequestDto
{
    public DateTime? ChargeDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? PlanPriceMonthly { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? SurchargeAmount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class RegisterSubscriptionPaymentRequestDto
{
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal CompensationAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
