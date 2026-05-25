using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nanchesoft.Web.Services.Purchases;

public sealed class MaterialPurchaseApiService
{
    private readonly IHttpClientFactory _factory;
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MaterialPurchaseApiService(IHttpClientFactory factory) => _factory = factory;

    private HttpClient Client => _factory.CreateClient("Nanchesoft.Api");

    // ── Lookups ────────────────────────────────────────────────
    public Task<MatLookups> GetLookupsAsync(Guid? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/mat/lookups?companyId={companyId}" : "/api/mat/lookups";
        return Client.GetFromJsonAsync<MatLookups>(url, _opts) ?? Task.FromResult(new MatLookups());
    }

    // ── Suppliers ──────────────────────────────────────────────
    public Task<PagedResult<MatSupplierListItem>> GetSuppliersAsync(Guid? companyId = null, string? q = null, int page = 1, int pageSize = 50)
    {
        var url = $"/api/mat/suppliers?page={page}&pageSize={pageSize}";
        if (companyId.HasValue) url += $"&companyId={companyId}";
        if (!string.IsNullOrWhiteSpace(q)) url += $"&q={Uri.EscapeDataString(q)}";
        return Client.GetFromJsonAsync<PagedResult<MatSupplierListItem>>(url, _opts)
            ?? Task.FromResult(new PagedResult<MatSupplierListItem>());
    }

    public Task<MatSupplierDetail?> GetSupplierAsync(Guid id)
        => Client.GetFromJsonAsync<MatSupplierDetail>($"/api/mat/suppliers/{id}", _opts);

    public async Task<Guid> SaveSupplierAsync(MatSupplierDetail model)
    {
        HttpResponseMessage resp;
        if (model.Id == Guid.Empty)
            resp = await Client.PostAsJsonAsync("/api/mat/suppliers", model);
        else
            resp = await Client.PutAsJsonAsync($"/api/mat/suppliers/{model.Id}", model);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<IdResult>(_opts);
        return result?.Id ?? model.Id;
    }

    public async Task DeleteSupplierAsync(Guid id)
        => (await Client.DeleteAsync($"/api/mat/suppliers/{id}")).EnsureSuccessStatusCode();

    public async Task<List<SupplierHistoryItem>> GetSupplierHistoryAsync(Guid id)
    {
        var data = await Client.GetFromJsonAsync<SupplierHistoryWrapper>($"/api/mat/suppliers/{id}/history", _opts);
        return data?.Orders ?? new List<SupplierHistoryItem>();
    }

    // ── Material Orders ────────────────────────────────────────
    public Task<PagedResult<MatOrderListItem>> GetOrdersAsync(Guid? companyId = null, string? status = null, int page = 1, int pageSize = 50)
    {
        var url = $"/api/mat/orders?page={page}&pageSize={pageSize}";
        if (companyId.HasValue) url += $"&companyId={companyId}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={status}";
        return Client.GetFromJsonAsync<PagedResult<MatOrderListItem>>(url, _opts)
            ?? Task.FromResult(new PagedResult<MatOrderListItem>());
    }

    public Task<MatOrderDetail?> GetOrderAsync(Guid id)
        => Client.GetFromJsonAsync<MatOrderDetail>($"/api/mat/orders/{id}", _opts);

    public Task<List<MatOrderDetail>> GetAuthorizedOrdersAsync(Guid? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/mat/orders/authorized?companyId={companyId}" : "/api/mat/orders/authorized";
        return Client.GetFromJsonAsync<List<MatOrderDetail>>(url, _opts) ?? Task.FromResult(new List<MatOrderDetail>());
    }

    public async Task<SaveResult> SaveOrderAsync(MatOrderDetail model)
    {
        HttpResponseMessage resp;
        if (model.Id == Guid.Empty)
            resp = await Client.PostAsJsonAsync("/api/mat/orders", model);
        else
            resp = await Client.PutAsJsonAsync($"/api/mat/orders/{model.Id}", model);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SaveResult>(_opts) ?? new SaveResult();
    }

    public async Task<bool> AuthorizeOrderAsync(Guid id, string userName)
    {
        var resp = await Client.PostAsJsonAsync($"/api/mat/orders/{id}/authorize", new { UserName = userName });
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> CancelOrderAsync(Guid id, string userName, string reason)
    {
        var resp = await Client.PostAsJsonAsync($"/api/mat/orders/{id}/cancel", new { UserName = userName, Reason = reason });
        return resp.IsSuccessStatusCode;
    }

    // ── Receipts ───────────────────────────────────────────────
    public Task<PagedResult<MatReceiptListItem>> GetReceiptsAsync(Guid? companyId = null,
        string? receiptType = null, string? status = null, string? paymentStatus = null,
        int page = 1, int pageSize = 50)
    {
        var url = $"/api/mat/receipts?page={page}&pageSize={pageSize}";
        if (companyId.HasValue) url += $"&companyId={companyId}";
        if (!string.IsNullOrWhiteSpace(receiptType)) url += $"&receiptType={receiptType}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={status}";
        if (!string.IsNullOrWhiteSpace(paymentStatus)) url += $"&paymentStatus={paymentStatus}";
        return Client.GetFromJsonAsync<PagedResult<MatReceiptListItem>>(url, _opts)
            ?? Task.FromResult(new PagedResult<MatReceiptListItem>());
    }

    public Task<MatReceiptDetail?> GetReceiptAsync(Guid id)
        => Client.GetFromJsonAsync<MatReceiptDetail>($"/api/mat/receipts/{id}", _opts);

    public async Task<SaveResult> SaveReceiptAsync(MatReceiptDetail model)
    {
        HttpResponseMessage resp;
        if (model.Id == Guid.Empty)
            resp = await Client.PostAsJsonAsync("/api/mat/receipts", model);
        else
            resp = await Client.PutAsJsonAsync($"/api/mat/receipts/{model.Id}", model);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SaveResult>(_opts) ?? new SaveResult();
    }

    public async Task<bool> ReviewReceiptAsync(Guid id, string userName)
    {
        var resp = await Client.PostAsJsonAsync($"/api/mat/receipts/{id}/review", new { UserName = userName });
        return resp.IsSuccessStatusCode;
    }

    public Task<MatCompareResult?> GetCompareAsync(Guid id)
        => Client.GetFromJsonAsync<MatCompareResult>($"/api/mat/receipts/{id}/compare", _opts);

    public async Task<AuthorizeResult> AuthorizeReceiptAsync(Guid id, string userName, bool authorizeDiffs, string? notes)
    {
        var resp = await Client.PostAsJsonAsync($"/api/mat/receipts/{id}/authorize",
            new { UserName = userName, AuthorizeDifferences = authorizeDiffs, Notes = notes });
        if (resp.IsSuccessStatusCode)
            return await resp.Content.ReadFromJsonAsync<AuthorizeResult>(_opts) ?? new AuthorizeResult(true, "authorized", 0);
        return new AuthorizeResult(false, "error", 0);
    }

    public async Task<bool> RejectReceiptAsync(Guid id, string userName, string reason)
    {
        var resp = await Client.PostAsJsonAsync($"/api/mat/receipts/{id}/reject",
            new { UserName = userName, Reason = reason });
        return resp.IsSuccessStatusCode;
    }

    public async Task<SaveResult> ConvertToInvoiceAsync(Guid id, string userName, DateTime? invoiceDate, string? invoiceNumber)
    {
        var resp = await Client.PostAsJsonAsync($"/api/mat/receipts/{id}/convert-to-invoice",
            new { UserName = userName, InvoiceDate = invoiceDate, SupplierInvoiceNumber = invoiceNumber });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SaveResult>(_opts) ?? new SaveResult();
    }

    // ── Payments ───────────────────────────────────────────────
    public async Task<SaveResult> RegisterPaymentAsync(MatPaymentModel model)
    {
        var resp = await Client.PostAsJsonAsync("/api/mat/payments", model);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SaveResult>(_opts) ?? new SaveResult();
    }

    public Task<List<MatPaymentListItem>> GetPaymentsByReceiptAsync(Guid receiptId)
        => Client.GetFromJsonAsync<List<MatPaymentListItem>>($"/api/mat/payments/by-receipt/{receiptId}", _opts)
            ?? Task.FromResult(new List<MatPaymentListItem>());

    // ── Inventory ──────────────────────────────────────────────
    public Task<List<MatStockBalance>> GetStockBalancesAsync(Guid? companyId = null, Guid? warehouseId = null, string? q = null)
    {
        var url = "/api/mat/inventory/balances";
        var qs = new List<string>();
        if (companyId.HasValue) qs.Add($"companyId={companyId}");
        if (warehouseId.HasValue) qs.Add($"warehouseId={warehouseId}");
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q)}");
        if (qs.Any()) url += "?" + string.Join("&", qs);
        return Client.GetFromJsonAsync<List<MatStockBalance>>(url, _opts) ?? Task.FromResult(new List<MatStockBalance>());
    }

    public Task<MatKardex?> GetKardexAsync(Guid materialItemId, Guid? warehouseId = null, DateTime? from = null, DateTime? to = null)
    {
        var url = $"/api/mat/inventory/kardex/{materialItemId}";
        var qs = new List<string>();
        if (warehouseId.HasValue) qs.Add($"warehouseId={warehouseId}");
        if (from.HasValue) qs.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) qs.Add($"to={to:yyyy-MM-dd}");
        if (qs.Any()) url += "?" + string.Join("&", qs);
        return Client.GetFromJsonAsync<MatKardex>(url, _opts);
    }

    // ── Reports ────────────────────────────────────────────────
    public Task<MatDashboard?> GetDashboardAsync(Guid? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/mat/reports/dashboard?companyId={companyId}" : "/api/mat/reports/dashboard";
        return Client.GetFromJsonAsync<MatDashboard>(url, _opts);
    }

    public Task<List<MatOrderByMaterialItem>> GetOrdersByMaterialAsync(Guid? companyId = null, DateTime? from = null, DateTime? to = null)
    {
        var url = "/api/mat/reports/orders-by-material";
        var qs = new List<string>();
        if (companyId.HasValue) qs.Add($"companyId={companyId}");
        if (from.HasValue) qs.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) qs.Add($"to={to:yyyy-MM-dd}");
        if (qs.Any()) url += "?" + string.Join("&", qs);
        return Client.GetFromJsonAsync<List<MatOrderByMaterialItem>>(url, _opts) ?? Task.FromResult(new List<MatOrderByMaterialItem>());
    }

    public Task<List<MatPendingReceiptItem>> GetPendingReceiptsAsync(Guid? companyId = null)
    {
        var url = companyId.HasValue ? $"/api/mat/reports/pending-receipts?companyId={companyId}" : "/api/mat/reports/pending-receipts";
        return Client.GetFromJsonAsync<List<MatPendingReceiptItem>>(url, _opts) ?? Task.FromResult(new List<MatPendingReceiptItem>());
    }

    public Task<MatPaymentPreview?> GetPaymentPreviewAsync(Guid id)
        => Client.GetFromJsonAsync<MatPaymentPreview>($"/api/mat/payments/{id}", _opts);

    public Task<MatMovementPreview?> GetMovementPreviewAsync(Guid id)
        => Client.GetFromJsonAsync<MatMovementPreview>($"/api/mat/inventory/movement/{id}", _opts);
}

// ── DTOs ─────────────────────────────────────────────────────

public sealed class MatLookups
{
    public List<LookupItem> Suppliers { get; set; } = new();
    public List<MatMaterialLookup> Materials { get; set; } = new();
    public List<LookupItem> Warehouses { get; set; } = new();
    public List<LookupItem> Units { get; set; } = new();
    public List<LookupItem> Taxes { get; set; } = new();
    public List<LookupItem> Currencies { get; set; } = new();
    public List<LookupItem> BankAccounts { get; set; } = new();
    public List<MatSeriesLookup> Series { get; set; } = new();
}

public sealed record LookupItem(Guid Id, string Name, string? Code = null);
public sealed record MatMaterialLookup(Guid Id, string Name, string Code, decimal AuthorizedCost, Guid? PurchaseUnitId);
public sealed record MatSeriesLookup(Guid Id, string Code, string Name, string Prefix, string DocumentType, int CurrentNumber);

public sealed class PagedResult<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 1;
}

public sealed class MatSupplierListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; }
    public string Classification { get; set; } = string.Empty;
    public string PreferredPaymentMethod { get; set; } = string.Empty;
}

public sealed class MatSupplierDetail
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string FiscalRegime { get; set; } = string.Empty;
    public string CfdiUse { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Colony { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = "México";
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public string SalesContact { get; set; } = string.Empty;
    public string CollectionContact { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public string AccountingAccount { get; set; } = string.Empty;
    public decimal DiscountPromptPayment { get; set; }
    public decimal Discount1 { get; set; }
    public decimal Discount2 { get; set; }
    public decimal Discount3 { get; set; }
    public decimal Discount4 { get; set; }
    public string PreferredPaymentMethod { get; set; } = "transfer";
    public string BankClabe { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class MatOrderListItem
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? SupplierDeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal ReceivedTotal { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public Guid CompanyId { get; set; }
}

public sealed class MatOrderDetail
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierRfc { get; set; } = string.Empty;
    public string SupplierAddress { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyRfc { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? WarehouseId { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? SupplierDeliveryDate { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public int PaymentTermDays { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal ReceivedTotal { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public List<MatOrderLineDetail> Lines { get; set; } = new();
}

public sealed class MatOrderLineDetail
{
    public Guid Id { get; set; }
    public Guid? MaterialItemId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PendingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Display helpers
    public string? MaterialName { get; set; }
    public string? MaterialCode { get; set; }
    public string? UnitName { get; set; }
}

public sealed class MatReceiptListItem
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string ReceiptType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string OrderFolio { get; set; } = string.Empty;
    public string SupplierDocumentNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public bool HasDifferences { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public Guid? ConvertedToInvoiceId { get; set; }
}

public sealed class MatReceiptDetail
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierRfc { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyRfc { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string OrderFolio { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string ReceiptType { get; set; } = "review";
    public DateTime? ReceiptDate { get; set; }
    public string SupplierDocumentNumber { get; set; } = string.Empty;
    public DateTime? SupplierDocumentDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "draft";
    public string PaymentStatus { get; set; } = "pending";
    public decimal PaidAmount { get; set; }
    public bool HasDifferences { get; set; }
    public bool DifferencesAuthorized { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public string? AuthorizedBy { get; set; }
    public Guid? ConvertedToInvoiceId { get; set; }
    public List<MatReceiptLineDetail> Lines { get; set; } = new();
    public List<MatPaymentListItem> Payments { get; set; } = new();
}

public sealed class MatReceiptLineDetail
{
    public Guid Id { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? MaterialItemId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal OrderedUnitPrice { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? MaterialName { get; set; }
    public string? MaterialCode { get; set; }
    public string? UnitName { get; set; }
}

public sealed class MatCompareResult
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public bool HasDifferences { get; set; }
    public List<MatDiffItem> Differences { get; set; } = new();
}

public sealed class MatDiffItem
{
    public Guid? MaterialItemId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string DiffType { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal QuantityDiff { get; set; }
    public decimal OrderedUnitPrice { get; set; }
    public decimal ReceivedUnitPrice { get; set; }
    public decimal PriceDiff { get; set; }
    public decimal OrderedTotal { get; set; }
    public decimal ReceivedTotal { get; set; }
    public decimal TotalDiff { get; set; }
}

public sealed class MatPaymentModel
{
    public Guid PurchaseReceiptId { get; set; }
    public Guid? BankAccountId { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "transfer";
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class MatPaymentListItem
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class MatStockBalance
{
    public Guid MaterialItemId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string SubfamilyName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public decimal LastCost { get; set; }
    public DateTime? LastMovementAt { get; set; }
}

public sealed class MatKardex
{
    public object? Material { get; set; }
    public List<MatKardexLine> Movements { get; set; } = new();
}

public sealed class MatKardexLine
{
    public Guid Id { get; set; }
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentFolio { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public sealed class MatDashboard
{
    public int PendingOrders { get; set; }
    public int DraftOrders { get; set; }
    public decimal MonthPurchased { get; set; }
    public int PendingPayment { get; set; }
    public int PartialPayment { get; set; }
    public int WithDiffs { get; set; }
    public List<SupplierTopItem> TopSuppliers { get; set; } = new();
}

public sealed record SupplierTopItem(string Name, decimal Total);
public sealed record MatOrderByMaterialItem(string OrderFolio, DateTime OrderDate, string SupplierName,
    string OrderStatus, string MaterialCode, string MaterialName,
    decimal Quantity, decimal ReceivedQuantity, decimal PendingQuantity, decimal UnitPrice, decimal LineTotal,
    DateTime? SupplierDeliveryDate = null);
public sealed record MatPendingReceiptItem(string OrderFolio, string SupplierName, DateTime? DeliveryDate,
    int DaysLate, string MaterialCode, string MaterialName,
    decimal Quantity, decimal ReceivedQuantity, decimal PendingQuantity, decimal UnitPrice);

public sealed class SupplierHistoryItem
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string OrderType { get; set; } = string.Empty;
}

public sealed class SupplierHistoryWrapper
{
    public List<SupplierHistoryItem> Orders { get; set; } = new();
}

public sealed record SaveResult(bool Success = false, Guid Id = default, string? Folio = null,
    string? PaymentStatus = null);
public sealed record IdResult(Guid Id);
public sealed record AuthorizeResult(bool Success, string Status, int DifsFound);

public sealed class MatPaymentPreview
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierRfc { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string ReceiptFolio { get; set; } = string.Empty;
    public DateTime? ReceiptDate { get; set; }
    public decimal ReceiptTotal { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyRfc { get; set; } = string.Empty;
}

public sealed class MatMovementPreview
{
    public Guid Id { get; set; }
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentFolio { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyRfc { get; set; } = string.Empty;
}
