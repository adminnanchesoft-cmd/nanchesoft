using System.Net.Http.Json;
using Nanchesoft.Web.Services.Sales;

namespace Nanchesoft.Web.Services.Cfdi;

public sealed class CfdiApiService
{
    private readonly HttpClient _http;

    public CfdiApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("Nanchesoft.Api");
    }

    public async Task<CfdiConfigurationDto> GetConfigurationAsync() =>
        await _http.GetFromJsonAsync<CfdiConfigurationDto>("api/cfdi/configuration") ?? new();

    public async Task<CfdiDashboardDto> GetDashboardAsync() =>
        await _http.GetFromJsonAsync<CfdiDashboardDto>("api/cfdi/dashboard") ?? new();

    public async Task<List<CfdiDocumentDto>> GetDocumentsAsync() =>
        await _http.GetFromJsonAsync<List<CfdiDocumentDto>>("api/cfdi/documents") ?? new();

    public async Task<List<CfdiDocumentDto>> GetStampQueueAsync() =>
        await _http.GetFromJsonAsync<List<CfdiDocumentDto>>("api/cfdi/stamp-queue") ?? new();

    public async Task<List<CfdiDocumentDto>> GetCancellationAsync() =>
        await _http.GetFromJsonAsync<List<CfdiDocumentDto>>("api/cfdi/cancellation") ?? new();

    public async Task<List<CfdiSalesSourceDto>> GetSalesInvoicesAsync() =>
        await _http.GetFromJsonAsync<List<CfdiSalesSourceDto>>("api/cfdi/sources/sales-invoices") ?? new();

    public async Task<List<CfdiSalesSourceDto>> GetCreditNotesAsync() =>
        await _http.GetFromJsonAsync<List<CfdiSalesSourceDto>>("api/cfdi/sources/credit-notes") ?? new();

    public async Task<List<CfdiShipmentSourceDto>> GetShipmentsAsync() =>
        await _http.GetFromJsonAsync<List<CfdiShipmentSourceDto>>("api/cfdi/sources/shipments") ?? new();

    public async Task<List<CfdiPayrollSourceDto>> GetPayrollRunsAsync() =>
        await _http.GetFromJsonAsync<List<CfdiPayrollSourceDto>>("api/cfdi/sources/payroll-runs") ?? new();

    public async Task<CfdiPayrollReceiptSummaryDto> GetPayrollReceiptSummaryAsync(Guid runId) =>
        await _http.GetFromJsonAsync<CfdiPayrollReceiptSummaryDto>($"api/cfdi/payroll-runs/{runId}/receipt-summary") ?? new();

    public async Task<List<CfdiPayrollReceiptDto>> GetPayrollReceiptsAsync(Guid runId) =>
        await _http.GetFromJsonAsync<List<CfdiPayrollReceiptDto>>($"api/cfdi/payroll-runs/{runId}/receipts") ?? new();

    public async Task<SalesDocumentModel?> GetInvoiceDraftFromShipmentAsync(Guid shipmentId)
    {
        var draft = await _http.GetFromJsonAsync<CfdiInvoiceDraftDto>($"api/cfdi/sources/shipments/{shipmentId}/invoice-draft");
        if (draft is null)
        {
            return null;
        }

        return new SalesDocumentModel
        {
            CatalogKey = "sales-invoices",
            CompanyId = draft.CompanyId,
            BranchId = draft.BranchId,
            CustomerId = draft.CustomerId,
            CurrencyId = draft.CurrencyId,
            SalesOrderId = draft.SalesOrderId,
            SalesShipmentId = draft.SalesShipmentId,
            Folio = draft.Folio,
            DocumentDate = draft.DocumentDate,
            Status = draft.Status,
            ExchangeRate = draft.ExchangeRate,
            Subtotal = draft.Subtotal,
            DiscountAmount = draft.DiscountAmount,
            TaxAmount = draft.TaxAmount,
            Total = draft.Total,
            Notes = draft.Notes,
            IsActive = true,
            Lines = draft.Lines.Select(x => new SalesLineModel
            {
                LineNumber = x.LineNumber,
                SalesOrderLineId = x.SalesOrderLineId,
                ItemId = x.ItemId,
                UnitId = x.UnitId,
                TaxId = x.TaxId,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                DiscountAmount = x.DiscountAmount,
                TaxAmount = x.TaxAmount,
                LineTotal = x.LineTotal
            }).ToList()
        };
    }

    public async Task<CfdiDocumentStatusDto?> GetSalesInvoiceStatusAsync(Guid id) =>
        await _http.GetFromJsonAsync<CfdiDocumentStatusDto>($"api/cfdi/sales-invoices/{id}/status");

    public async Task<CfdiDocumentStatusDto?> GetCreditNoteStatusAsync(Guid id) =>
        await _http.GetFromJsonAsync<CfdiDocumentStatusDto>($"api/cfdi/credit-notes/{id}/status");

    public async Task<CfdiDocumentStatusDto?> GetPayrollRunStatusAsync(Guid id) =>
        await _http.GetFromJsonAsync<CfdiDocumentStatusDto>($"api/cfdi/payroll-runs/{id}/status");

    public async Task<CfdiPayrollReceiptStatusDto?> GetPayrollRunLineStatusAsync(Guid id) =>
        await _http.GetFromJsonAsync<CfdiPayrollReceiptStatusDto>($"api/cfdi/payroll-run-lines/{id}/status");

    public async Task StampSalesInvoiceAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/sales-invoices/{id}/stamp", null));

    public async Task CancelSalesInvoiceAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/sales-invoices/{id}/cancel", null));

    public async Task StampCreditNoteAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/credit-notes/{id}/stamp", null));

    public async Task CancelCreditNoteAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/credit-notes/{id}/cancel", null));

    public async Task StampPayrollRunAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/payroll-runs/{id}/stamp", null));

    public async Task CancelPayrollRunAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/payroll-runs/{id}/cancel", null));

    public async Task GeneratePayrollReceiptsAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/payroll-runs/{id}/generate-receipts", null));

    public async Task CancelPayrollReceiptsAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/payroll-runs/{id}/cancel-receipts", null));

    public async Task StampPayrollRunLineAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/payroll-run-lines/{id}/stamp", null));

    public async Task CancelPayrollRunLineAsync(Guid id) =>
        await EnsureSuccessAsync(await _http.PostAsync($"api/cfdi/payroll-run-lines/{id}/cancel", null));

    public string GetPayrollRunLineXmlUrl(Guid id) => $"api/cfdi/payroll-run-lines/{id}/xml";
    public string GetPayrollRunLinePdfUrl(Guid id) => $"api/cfdi/payroll-run-lines/{id}/pdf";

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(content)
            ? "La API CFDI devolvió un error sin detalle."
            : content);
    }
}

public sealed class CfdiConfigurationDto
{
    public string Mode { get; set; } = "demo";
    public string Provider { get; set; } = "demo-local";
    public string Environment { get; set; } = "sandbox";
    public string Emitter { get; set; } = "Nanchesoft Demo";
    public string Notes { get; set; } = string.Empty;
}

public sealed class CfdiDashboardDto
{
    public int Pending { get; set; }
    public int Stamped { get; set; }
    public int Cancelled { get; set; }
    public int Failed { get; set; }
    public List<CfdiDocumentDto> Recent { get; set; } = [];
}

public sealed class CfdiDocumentDto
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public decimal Total { get; set; }
    public string BusinessStatus { get; set; } = string.Empty;
    public string CfdiStatus { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    public DateTime? StampedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string SourceRoute { get; set; } = string.Empty;
    public Guid? SalesShipmentId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class CfdiDocumentStatusDto
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string BusinessStatus { get; set; } = string.Empty;
    public string CfdiStatus { get; set; } = "pending";
    public string? Uuid { get; set; }
    public DateTime? StampedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class CfdiPayrollReceiptStatusDto
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = "payroll-receipt";
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string CfdiStatus { get; set; } = "pending";
    public string? Uuid { get; set; }
    public DateTime? StampedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string ReceiptSeries { get; set; } = string.Empty;
    public string ReceiptFolio { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class CfdiSalesSourceDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public decimal Total { get; set; }
    public string BusinessStatus { get; set; } = string.Empty;
    public string CfdiStatus { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    public DateTime? StampedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string EditRoute { get; set; } = string.Empty;
}

public sealed class CfdiShipmentSourceDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime ShipmentDate { get; set; }
    public string BusinessStatus { get; set; } = string.Empty;
    public int LinesCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public int LinkedInvoices { get; set; }
    public string EditRoute { get; set; } = string.Empty;
    public string InvoiceRoute { get; set; } = string.Empty;
}

public sealed class CfdiPayrollSourceDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string BusinessStatus { get; set; } = string.Empty;
    public string CfdiStatus { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    public DateTime? StampedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public int EmployeeCount { get; set; }
    public decimal Total { get; set; }
    public int StampedReceipts { get; set; }
    public int CancelledReceipts { get; set; }
    public int PendingReceipts { get; set; }
    public string EditRoute { get; set; } = string.Empty;
    public string ReceiptsRoute { get; set; } = string.Empty;
}

public sealed class CfdiPayrollReceiptSummaryDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime RunDate { get; set; }
    public string BusinessStatus { get; set; } = string.Empty;
    public string CfdiStatus { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public int TotalReceipts { get; set; }
    public int StampedReceipts { get; set; }
    public int CancelledReceipts { get; set; }
    public int PendingReceipts { get; set; }
    public DateTime? StampedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? PrimaryUuid { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class CfdiPayrollReceiptDto
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public decimal DaysPaid { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal IncidentsAmount { get; set; }
    public string CfdiStatus { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    public DateTime? StampedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string ReceiptSeries { get; set; } = string.Empty;
    public string ReceiptFolio { get; set; } = string.Empty;
    public string XmlUrl { get; set; } = string.Empty;
    public string PdfUrl { get; set; } = string.Empty;
    public bool CanStamp { get; set; }
    public bool CanCancel { get; set; }
}

public sealed class CfdiInvoiceDraftDto
{
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? SalesShipmentId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string Status { get; set; } = "draft";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<CfdiInvoiceDraftLineDto> Lines { get; set; } = [];
}

public sealed class CfdiInvoiceDraftLineDto
{
    public int LineNumber { get; set; }
    public Guid? SalesOrderLineId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
