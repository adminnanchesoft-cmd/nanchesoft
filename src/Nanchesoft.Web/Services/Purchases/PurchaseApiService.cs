using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.Purchases;

public sealed class PurchaseApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PurchaseApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey switch
        {
            "purchase-requisitions" => GetCatalogFromEndpointAsync(catalogKey, "Requisiciones", "Solicitud interna de compra con folio, solicitante y estatus.", "PurchaseRequisitionId", "/api/purchases/requisitions", BuildRequisitionColumnsAsync),
            "purchase-orders" => GetCatalogFromEndpointAsync(catalogKey, "Órdenes de compra", "Órdenes con proveedor, totales y requisición origen.", "PurchaseOrderId", "/api/purchases/orders", BuildOrderColumnsAsync),
            "purchase-receipts" => GetCatalogFromEndpointAsync(catalogKey, "Recepciones", "Recepciones parciales o totales contra orden de compra.", "PurchaseReceiptId", "/api/purchases/receipts", BuildReceiptColumnsAsync),
            "purchase-invoices" => GetCatalogFromEndpointAsync(catalogKey, "Facturas proveedor", "Facturas de compra relacionadas a órdenes y recepciones.", "PurchaseInvoiceId", "/api/purchases/invoices", BuildInvoiceColumnsAsync),
            "purchase-returns" => GetCatalogFromEndpointAsync(catalogKey, "Devoluciones compra", "Devoluciones a proveedor con origen en recepción o factura.", "PurchaseReturnId", "/api/purchases/returns", BuildReturnColumnsAsync),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var endpoint = GetBaseEndpoint(catalogKey);
        var response = await client.PostAsJsonAsync(endpoint, JsonSerializer.Deserialize<object>(payload.GetRawText()));
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var endpoint = $"{GetBaseEndpoint(catalogKey)}/{key}";
        var response = await client.PutAsJsonAsync(endpoint, JsonSerializer.Deserialize<object>(payload.GetRawText()));
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.DeleteAsync($"{GetBaseEndpoint(catalogKey)}/{key}");
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<PurchaseDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<PurchaseDashboardSummaryDto>("/api/purchases/dashboard/summary") ?? new PurchaseDashboardSummaryDto();
    }

    public async Task<PurchaseDocumentEditorDefinition> GetEditorDefinitionAsync(string catalogKey, Guid? id = null)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var lookups = await client.GetFromJsonAsync<PurchaseLookups>("/api/purchases/lookups") ?? new PurchaseLookups();
        var definition = BuildEditorDefinition(catalogKey, lookups);
        definition.Document = id.HasValue
            ? await LoadDocumentAsync(catalogKey, id.Value)
            : CreateEmptyDocument(catalogKey);
        return definition;
    }

    public async Task<Guid> SaveDocumentAsync(string catalogKey, PurchaseDocumentModel document)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var payload = BuildRequest(catalogKey, document);
        HttpResponseMessage response;

        if (document.Id.HasValue)
        {
            response = await client.PutAsJsonAsync($"{GetBaseEndpoint(catalogKey)}/{document.Id.Value}", payload);
        }
        else
        {
            response = await client.PostAsJsonAsync(GetBaseEndpoint(catalogKey), payload);
        }

        await EnsureSuccessAsync(response);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (document.Id.HasValue)
        {
            return document.Id.Value;
        }

        if (json.RootElement.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String && Guid.TryParse(idElement.GetString(), out var id))
        {
            return id;
        }

        if (json.RootElement.TryGetProperty("id", out idElement) && idElement.ValueKind != JsonValueKind.Null && Guid.TryParse(idElement.ToString(), out id))
        {
            return id;
        }

        return Guid.Empty;
    }

    private async Task<PurchaseDocumentModel> LoadDocumentAsync(string catalogKey, Guid id)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var endpoint = $"{GetBaseEndpoint(catalogKey)}/{id}";

        return catalogKey switch
        {
            "purchase-requisitions" => Map(await client.GetFromJsonAsync<PurchaseRequisitionRequest>(endpoint) ?? new PurchaseRequisitionRequest(), catalogKey),
            "purchase-orders" => Map(await client.GetFromJsonAsync<PurchaseOrderRequest>(endpoint) ?? new PurchaseOrderRequest(), catalogKey),
            "purchase-receipts" => Map(await client.GetFromJsonAsync<PurchaseReceiptRequest>(endpoint) ?? new PurchaseReceiptRequest(), catalogKey),
            "purchase-invoices" => Map(await client.GetFromJsonAsync<PurchaseInvoiceRequest>(endpoint) ?? new PurchaseInvoiceRequest(), catalogKey),
            "purchase-returns" => Map(await client.GetFromJsonAsync<PurchaseReturnRequest>(endpoint) ?? new PurchaseReturnRequest(), catalogKey),
            _ => CreateEmptyDocument(catalogKey)
        };
    }

    private static PurchaseDocumentModel CreateEmptyDocument(string catalogKey)
    {
        return new PurchaseDocumentModel
        {
            CatalogKey = catalogKey,
            Status = "draft",
            ExchangeRate = 1m,
            IsActive = true,
            DocumentDate = DateTime.Today,
            Lines = [new PurchaseLineModel { LineNumber = 1, Quantity = 1m }]
        };
    }

    private static PurchaseDocumentEditorDefinition BuildEditorDefinition(string catalogKey, PurchaseLookups lookups)
    {
        return catalogKey switch
        {
            "purchase-requisitions" => new PurchaseDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de requisición",
                Subtitle = "Captura encabezado y partidas de la requisición.",
                LinesTitle = "Partidas solicitadas",
                RequiresSupplier = false,
                RequiresWarehouse = false,
                RequiresCurrency = false,
                RequiresRequisition = false,
                RequiresOrder = false,
                RequiresReceipt = false,
                RequiresInvoice = false,
                UsesRequestedBy = true,
                UsesReason = false,
                UsesSupplierInvoiceFolio = false,
                UsesPaymentTermDays = false,
                UsesAmounts = false,
                Lookups = lookups
            },
            "purchase-orders" => new PurchaseDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de orden de compra",
                Subtitle = "Completa proveedor, moneda, condiciones y partidas.",
                LinesTitle = "Partidas de la orden",
                RequiresSupplier = true,
                RequiresWarehouse = false,
                RequiresCurrency = true,
                RequiresRequisition = true,
                RequiresOrder = false,
                RequiresReceipt = false,
                RequiresInvoice = false,
                UsesRequestedBy = false,
                UsesReason = false,
                UsesSupplierInvoiceFolio = false,
                UsesPaymentTermDays = true,
                UsesAmounts = true,
                Lookups = lookups
            },
            "purchase-receipts" => new PurchaseDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de recepción",
                Subtitle = "Recibe mercancía y captura cantidades recibidas.",
                LinesTitle = "Partidas recibidas",
                RequiresSupplier = true,
                RequiresWarehouse = true,
                RequiresCurrency = false,
                RequiresRequisition = false,
                RequiresOrder = true,
                RequiresReceipt = false,
                RequiresInvoice = false,
                UsesRequestedBy = false,
                UsesReason = false,
                UsesSupplierInvoiceFolio = false,
                UsesPaymentTermDays = false,
                UsesAmounts = false,
                Lookups = lookups
            },
            "purchase-invoices" => new PurchaseDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de factura de proveedor",
                Subtitle = "Relaciona orden/recepción y captura importes por partida.",
                LinesTitle = "Partidas facturadas",
                RequiresSupplier = true,
                RequiresWarehouse = false,
                RequiresCurrency = true,
                RequiresRequisition = false,
                RequiresOrder = true,
                RequiresReceipt = true,
                RequiresInvoice = false,
                UsesRequestedBy = false,
                UsesReason = false,
                UsesSupplierInvoiceFolio = true,
                UsesPaymentTermDays = false,
                UsesAmounts = true,
                Lookups = lookups
            },
            _ => new PurchaseDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de devolución de compra",
                Subtitle = "Captura el origen y las partidas devueltas al proveedor.",
                LinesTitle = "Partidas devueltas",
                RequiresSupplier = true,
                RequiresWarehouse = true,
                RequiresCurrency = false,
                RequiresRequisition = false,
                RequiresOrder = false,
                RequiresReceipt = true,
                RequiresInvoice = true,
                UsesRequestedBy = false,
                UsesReason = true,
                UsesSupplierInvoiceFolio = false,
                UsesPaymentTermDays = false,
                UsesAmounts = true,
                Lookups = lookups
            }
        };
    }

    private static PurchaseDocumentModel Map(PurchaseRequisitionRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.PurchaseRequisitionId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.RequisitionDate,
        Status = source.Status ?? "draft",
        Notes = source.Notes ?? string.Empty,
        ApprovedAt = source.ApprovedAt,
        RequestedByName = source.RequestedByName ?? string.Empty,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static PurchaseDocumentModel Map(PurchaseOrderRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.PurchaseOrderId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        SupplierId = source.SupplierId,
        CurrencyId = source.CurrencyId,
        PurchaseRequisitionId = source.PurchaseRequisitionId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.OrderDate,
        Status = source.Status ?? "draft",
        Notes = source.Notes ?? string.Empty,
        ApprovedAt = source.ApprovedAt,
        ClosedAt = source.ClosedAt,
        PaymentTermDays = source.PaymentTermDays,
        ExchangeRate = source.ExchangeRate,
        Subtotal = source.Subtotal,
        TaxAmount = source.TaxAmount,
        Total = source.Total,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static PurchaseDocumentModel Map(PurchaseReceiptRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.PurchaseReceiptId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        WarehouseId = source.WarehouseId,
        SupplierId = source.SupplierId,
        PurchaseOrderId = source.PurchaseOrderId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.ReceiptDate,
        Status = source.Status ?? "draft",
        Notes = source.Notes ?? string.Empty,
        PostedAt = source.PostedAt,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static PurchaseDocumentModel Map(PurchaseInvoiceRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.PurchaseInvoiceId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        SupplierId = source.SupplierId,
        PurchaseOrderId = source.PurchaseOrderId,
        PurchaseReceiptId = source.PurchaseReceiptId,
        CurrencyId = source.CurrencyId,
        Folio = source.Folio ?? string.Empty,
        SupplierInvoiceFolio = source.SupplierInvoiceFolio ?? string.Empty,
        DocumentDate = source.InvoiceDate,
        Status = source.Status ?? "draft",
        Notes = source.Notes ?? string.Empty,
        ApprovedAt = source.ApprovedAt,
        PostedAt = source.PostedAt,
        ExchangeRate = source.ExchangeRate,
        Subtotal = source.Subtotal,
        TaxAmount = source.TaxAmount,
        Total = source.Total,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static PurchaseDocumentModel Map(PurchaseReturnRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.PurchaseReturnId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        WarehouseId = source.WarehouseId,
        SupplierId = source.SupplierId,
        PurchaseReceiptId = source.PurchaseReceiptId,
        PurchaseInvoiceId = source.PurchaseInvoiceId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.ReturnDate,
        Status = source.Status ?? "draft",
        Reason = source.Reason ?? string.Empty,
        ApprovedAt = source.ApprovedAt,
        PostedAt = source.PostedAt,
        Subtotal = source.Subtotal,
        TaxAmount = source.TaxAmount,
        Total = source.Total,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static PurchaseLineModel Map(PurchaseLineRequest source) => new()
    {
        Id = source.Id,
        LineNumber = source.LineNumber,
        PurchaseOrderLineId = source.PurchaseOrderLineId,
        SourceLineId = source.SourceLineId,
        ItemId = source.ItemId,
        UnitId = source.UnitId,
        TaxId = source.TaxId,
        Description = source.Description ?? string.Empty,
        Quantity = source.Quantity,
        ReceivedQuantity = source.ReceivedQuantity,
        PendingQuantity = source.PendingQuantity,
        UnitPrice = source.UnitPrice,
        DiscountAmount = source.DiscountAmount,
        TaxAmount = source.TaxAmount,
        LineTotal = source.LineTotal,
        Notes = source.Notes ?? string.Empty
    };

    private static object BuildRequest(string catalogKey, PurchaseDocumentModel document)
    {
        var lines = document.Lines.Select(x => new PurchaseLineRequest
        {
            Id = x.Id,
            LineNumber = x.LineNumber,
            PurchaseOrderLineId = x.PurchaseOrderLineId,
            SourceLineId = x.SourceLineId,
            ItemId = x.ItemId,
            UnitId = x.UnitId,
            TaxId = x.TaxId,
            Description = x.Description,
            Quantity = x.Quantity,
            ReceivedQuantity = x.ReceivedQuantity,
            PendingQuantity = x.PendingQuantity,
            UnitPrice = x.UnitPrice,
            DiscountAmount = x.DiscountAmount,
            TaxAmount = x.TaxAmount,
            LineTotal = x.LineTotal,
            Notes = x.Notes
        }).ToList();

        return catalogKey switch
        {
            "purchase-requisitions" => new PurchaseRequisitionRequest
            {
                PurchaseRequisitionId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                Folio = document.Folio,
                RequisitionDate = document.DocumentDate,
                RequestedByName = document.RequestedByName,
                Status = document.Status,
                Notes = document.Notes,
                ApprovedAt = document.ApprovedAt,
                IsActive = document.IsActive,
                Lines = lines
            },
            "purchase-orders" => new PurchaseOrderRequest
            {
                PurchaseOrderId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                SupplierId = document.SupplierId,
                CurrencyId = document.CurrencyId,
                PurchaseRequisitionId = document.PurchaseRequisitionId,
                Folio = document.Folio,
                OrderDate = document.DocumentDate,
                Status = document.Status,
                PaymentTermDays = document.PaymentTermDays,
                ExchangeRate = document.ExchangeRate,
                Subtotal = document.Subtotal,
                TaxAmount = document.TaxAmount,
                Total = document.Total,
                Notes = document.Notes,
                ApprovedAt = document.ApprovedAt,
                ClosedAt = document.ClosedAt,
                IsActive = document.IsActive,
                Lines = lines
            },
            "purchase-receipts" => new PurchaseReceiptRequest
            {
                PurchaseReceiptId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                WarehouseId = document.WarehouseId,
                SupplierId = document.SupplierId,
                PurchaseOrderId = document.PurchaseOrderId,
                Folio = document.Folio,
                ReceiptDate = document.DocumentDate,
                Status = document.Status,
                Notes = document.Notes,
                PostedAt = document.PostedAt,
                IsActive = document.IsActive,
                Lines = lines
            },
            "purchase-invoices" => new PurchaseInvoiceRequest
            {
                PurchaseInvoiceId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                SupplierId = document.SupplierId,
                PurchaseOrderId = document.PurchaseOrderId,
                PurchaseReceiptId = document.PurchaseReceiptId,
                CurrencyId = document.CurrencyId,
                Folio = document.Folio,
                SupplierInvoiceFolio = document.SupplierInvoiceFolio,
                InvoiceDate = document.DocumentDate,
                Status = document.Status,
                ExchangeRate = document.ExchangeRate,
                Subtotal = document.Subtotal,
                TaxAmount = document.TaxAmount,
                Total = document.Total,
                Notes = document.Notes,
                ApprovedAt = document.ApprovedAt,
                PostedAt = document.PostedAt,
                IsActive = document.IsActive,
                Lines = lines
            },
            _ => new PurchaseReturnRequest
            {
                PurchaseReturnId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                WarehouseId = document.WarehouseId,
                SupplierId = document.SupplierId,
                PurchaseReceiptId = document.PurchaseReceiptId,
                PurchaseInvoiceId = document.PurchaseInvoiceId,
                Folio = document.Folio,
                ReturnDate = document.DocumentDate,
                Reason = document.Reason,
                Status = document.Status,
                Subtotal = document.Subtotal,
                TaxAmount = document.TaxAmount,
                Total = document.Total,
                ApprovedAt = document.ApprovedAt,
                PostedAt = document.PostedAt,
                IsActive = document.IsActive,
                Lines = lines
            }
        };
    }

    private static string GetBaseEndpoint(string catalogKey) => catalogKey switch
    {
        "purchase-requisitions" => "/api/purchases/requisitions",
        "purchase-orders" => "/api/purchases/orders",
        "purchase-receipts" => "/api/purchases/receipts",
        "purchase-invoices" => "/api/purchases/invoices",
        "purchase-returns" => "/api/purchases/returns",
        _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
    };

    private async Task<CatalogViewDefinition> GetCatalogFromEndpointAsync(string catalogKey, string title, string subtitle, string keyExpr, string endpoint, Func<Task<List<CatalogColumnDefinition>>> columnsFactory)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rawRows = await client.GetFromJsonAsync<List<Dictionary<string, object?>>>(endpoint) ?? [];
        var rows = rawRows.Select(NormalizeRowKeys).ToList();
        return new CatalogViewDefinition
        {
            CatalogKey = catalogKey,
            Title = title,
            Subtitle = subtitle,
            KeyExpr = keyExpr,
            AllowCreate = true,
            AllowUpdate = true,
            AllowDelete = true,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => x.TryGetValue("IsActive", out var v) && IsTrue(v)),
            InactiveCount = rows.Count(x => !x.TryGetValue("IsActive", out var v) || !IsTrue(v)),
            Columns = await columnsFactory(),
            Rows = rows
        };
    }

    private static Dictionary<string, object?> NormalizeRowKeys(Dictionary<string, object?> source)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;

            var key = char.ToUpperInvariant(pair.Key[0]) + pair.Key[1..];
            normalized[key] = pair.Value;
        }
        return normalized;
    }

    private async Task<List<CatalogColumnDefinition>> BuildRequisitionColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("PurchaseRequisitionId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("RequisitionDate", "Fecha", true, 120),
            TextColumn("RequestedByName", "Solicitó", true, true, 180),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            TextColumn("Notes", "Notas", false, true, 220),
            DateColumn("ApprovedAt", "Aprobada", false, 150),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildOrderColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("PurchaseOrderId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("SupplierId", "Proveedor", lookups.Suppliers, true, 220),
            LookupColumn("CurrencyId", "Moneda", lookups.Currencies, false, 160, "currencies"),
            LookupColumn("PurchaseRequisitionId", "Requisición", lookups.Requisitions, false, 150),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("OrderDate", "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            NumberColumn("PaymentTermDays", "Plazo", false, 90),
            NumberColumn("ExchangeRate", "TC", false, 90),
            NumberColumn("Subtotal", "Subtotal", false, 120),
            NumberColumn("TaxAmount", "Impuesto", false, 120),
            NumberColumn("Total", "Total", false, 120),
            TextColumn("Notes", "Notas", false, true, 200),
            DateColumn("ApprovedAt", "Aprobada", false, 150),
            DateColumn("ClosedAt", "Cerrada", false, 150),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildReceiptColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("PurchaseReceiptId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("WarehouseId", "Almacén", lookups.Warehouses, true, 220, "warehouses"),
            LookupColumn("SupplierId", "Proveedor", lookups.Suppliers, false, 220),
            LookupColumn("PurchaseOrderId", "Orden", lookups.Orders, false, 150),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("ReceiptDate", "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            TextColumn("Notes", "Notas", false, true, 220),
            DateColumn("PostedAt", "Posteada", false, 150),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildInvoiceColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("PurchaseInvoiceId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("SupplierId", "Proveedor", lookups.Suppliers, true, 220),
            LookupColumn("PurchaseOrderId", "Orden", lookups.Orders, false, 150),
            LookupColumn("PurchaseReceiptId", "Recepción", lookups.Receipts, false, 150),
            LookupColumn("CurrencyId", "Moneda", lookups.Currencies, false, 150, "currencies"),
            TextColumn("Folio", "Folio", true, true, 130),
            TextColumn("SupplierInvoiceFolio", "Folio proveedor", false, true, 150),
            DateColumn("InvoiceDate", "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            NumberColumn("ExchangeRate", "TC", false, 90),
            NumberColumn("Subtotal", "Subtotal", false, 120),
            NumberColumn("TaxAmount", "Impuesto", false, 120),
            NumberColumn("Total", "Total", false, 120),
            TextColumn("Notes", "Notas", false, true, 200),
            DateColumn("ApprovedAt", "Aprobada", false, 150),
            DateColumn("PostedAt", "Posteada", false, 150),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildReturnColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("PurchaseReturnId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("WarehouseId", "Almacén", lookups.Warehouses, true, 220, "warehouses"),
            LookupColumn("SupplierId", "Proveedor", lookups.Suppliers, true, 220),
            LookupColumn("PurchaseReceiptId", "Recepción", lookups.Receipts, false, 150),
            LookupColumn("PurchaseInvoiceId", "Factura", lookups.Invoices, false, 150),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("ReturnDate", "Fecha", true, 120),
            TextColumn("Reason", "Motivo", false, true, 220),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            NumberColumn("Subtotal", "Subtotal", false, 120),
            NumberColumn("TaxAmount", "Impuesto", false, 120),
            NumberColumn("Total", "Total", false, 120),
            DateColumn("ApprovedAt", "Aprobada", false, 150),
            DateColumn("PostedAt", "Posteada", false, 150),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<PurchaseLookups> GetLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<PurchaseLookups>("/api/purchases/lookups") ?? new PurchaseLookups();
    }

    private static bool IsTrue(object? value) => value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    private static CatalogColumnDefinition TextColumn(string field, string caption, bool required = false, bool allowEditing = true, int width = 160) => new() { DataField = field, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Width = width };
    private static CatalogColumnDefinition NumberColumn(string field, string caption, bool required = false, int width = 120) => new() { DataField = field, Caption = caption, DataType = "number", Required = required, Width = width };
    private static CatalogColumnDefinition DateColumn(string field, string caption, bool required = false, int width = 130) => new() { DataField = field, Caption = caption, DataType = "date", Required = required, Width = width };
    private static CatalogColumnDefinition BoolColumn(string field, string caption, int width = 90) => new() { DataField = field, Caption = caption, DataType = "boolean", Width = width };
    private static CatalogColumnDefinition LookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, bool required = false, int width = 180, string? quickCreateKey = null) => new() { DataField = field, Caption = caption, DataType = "string", Required = required, Width = width, UseLookup = true, LookupItems = lookupItems, QuickCreateKey = quickCreateKey };
    private static List<CatalogLookupItem> StatusLookups() => [new() { Id = "draft", Name = "Draft" }, new() { Id = "pending_approval", Name = "Pending approval" }, new() { Id = "approved", Name = "Approved" }, new() { Id = "posted", Name = "Posted" }, new() { Id = "closed", Name = "Closed" }, new() { Id = "cancelled", Name = "Cancelled" }];

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var content = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(content) ? "La API devolvió un error sin detalle." : content);
    }
}

public sealed class PurchaseDocumentEditorDefinition
{
    public string CatalogKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string LinesTitle { get; set; } = string.Empty;
    public bool RequiresSupplier { get; set; }
    public bool RequiresWarehouse { get; set; }
    public bool RequiresCurrency { get; set; }
    public bool RequiresRequisition { get; set; }
    public bool RequiresOrder { get; set; }
    public bool RequiresReceipt { get; set; }
    public bool RequiresInvoice { get; set; }
    public bool UsesRequestedBy { get; set; }
    public bool UsesReason { get; set; }
    public bool UsesSupplierInvoiceFolio { get; set; }
    public bool UsesPaymentTermDays { get; set; }
    public bool UsesAmounts { get; set; }
    public PurchaseLookups Lookups { get; set; } = new();
    public PurchaseDocumentModel Document { get; set; } = new();
}

public sealed class PurchaseDocumentModel
{
    public string CatalogKey { get; set; } = string.Empty;
    public Guid? Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? PurchaseRequisitionId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? PurchaseReceiptId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; } = DateTime.Today;
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string SupplierInvoiceFolio { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineModel> Lines { get; set; } = [];
}

public sealed class PurchaseLineModel
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? SourceLineId { get; set; }
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

    public string ItemIdString
    {
        get => ItemId?.ToString() ?? string.Empty;
        set => ItemId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    public string UnitIdString
    {
        get => UnitId?.ToString() ?? string.Empty;
        set => UnitId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    public string TaxIdString
    {
        get => TaxId?.ToString() ?? string.Empty;
        set => TaxId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

public sealed class PurchaseLookups
{
    public List<CatalogLookupItem> Companies { get; set; } = [];
    public List<CatalogLookupItem> Branches { get; set; } = [];
    public List<CatalogLookupItem> Warehouses { get; set; } = [];
    public List<CatalogLookupItem> Suppliers { get; set; } = [];
    public List<CatalogLookupItem> Currencies { get; set; } = [];
    public List<CatalogLookupItem> Requisitions { get; set; } = [];
    public List<CatalogLookupItem> Orders { get; set; } = [];
    public List<CatalogLookupItem> Receipts { get; set; } = [];
    public List<CatalogLookupItem> Invoices { get; set; } = [];
    public List<CatalogLookupItem> Returns { get; set; } = [];
    public List<CatalogLookupItem> Items { get; set; } = [];
    public List<CatalogLookupItem> Units { get; set; } = [];
    public List<CatalogLookupItem> Taxes { get; set; } = [];
}

public sealed class PurchaseDashboardSummaryDto
{
    public int PendingRequisitions { get; set; }
    public int OpenOrders { get; set; }
    public int RecentReceipts { get; set; }
    public int RecentInvoices { get; set; }
    public decimal PeriodPurchased { get; set; }
    public decimal ReturnsAmount { get; set; }
}

public sealed class PurchaseLineRequest
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? SourceLineId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PendingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

public sealed class PurchaseRequisitionRequest
{
    public Guid? PurchaseRequisitionId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Folio { get; set; }
    public DateTime? RequisitionDate { get; set; }
    public string? RequestedByName { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

public sealed class PurchaseOrderRequest
{
    public Guid? PurchaseOrderId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? PurchaseRequisitionId { get; set; }
    public string? Folio { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? Status { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

public sealed class PurchaseReceiptRequest
{
    public Guid? PurchaseReceiptId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? Folio { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

public sealed class PurchaseInvoiceRequest
{
    public Guid? PurchaseInvoiceId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? PurchaseReceiptId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? Folio { get; set; }
    public string? SupplierInvoiceFolio { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Status { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}

public sealed class PurchaseReturnRequest
{
    public Guid? PurchaseReturnId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseReceiptId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public string? Folio { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}
