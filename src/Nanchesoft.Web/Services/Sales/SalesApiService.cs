using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.Sales;

public sealed class SalesApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SalesApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey switch
        {
            "sales-quotes" => GetCatalogFromEndpointAsync(catalogKey, "Cotizaciones", "Cotizaciones comerciales con cliente, vigencia y partidas.", "SalesQuoteId", "/api/sales/quotes", BuildQuoteColumnsAsync),
            "sales-orders" => GetCatalogFromEndpointAsync(catalogKey, "Pedidos", "Pedidos de venta con cliente, totales y cotización origen.", "SalesOrderId", "/api/sales/orders", BuildOrderColumnsAsync),
            "sales-shipments" => GetCatalogFromEndpointAsync(catalogKey, "Remisiones", "Remisiones parciales o totales contra pedido de venta.", "SalesShipmentId", "/api/sales/shipments", BuildShipmentColumnsAsync),
            "sales-invoices" => GetCatalogFromEndpointAsync(catalogKey, "Facturas", "Facturas de venta relacionadas a pedidos y remisiones.", "SalesInvoiceId", "/api/sales/invoices", BuildInvoiceColumnsAsync),
            "sales-returns" => GetCatalogFromEndpointAsync(catalogKey, "Devoluciones", "Devoluciones de cliente con origen en remisión o factura.", "SalesReturnId", "/api/sales/returns", BuildReturnColumnsAsync),
            "credit-notes" => GetCatalogFromEndpointAsync(catalogKey, "Notas de crédito", "Notas de crédito ligadas a factura de venta.", "CreditNoteId", "/api/sales/credit-notes", BuildCreditNoteColumnsAsync),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsJsonAsync(GetBaseEndpoint(catalogKey), JsonSerializer.Deserialize<object>(payload.GetRawText()));
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PutAsJsonAsync($"{GetBaseEndpoint(catalogKey)}/{key}", JsonSerializer.Deserialize<object>(payload.GetRawText()));
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

    public async Task<SalesDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<SalesDashboardSummaryDto>("/api/sales/dashboard/summary") ?? new SalesDashboardSummaryDto();
    }

    public async Task<SalesDocumentEditorDefinition> GetEditorDefinitionAsync(string catalogKey, Guid? id = null)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var lookups = await client.GetFromJsonAsync<SalesLookups>("/api/sales/lookups") ?? new SalesLookups();
        var definition = BuildEditorDefinition(catalogKey, lookups);
        definition.Document = id.HasValue ? await LoadDocumentAsync(catalogKey, id.Value) : CreateEmptyDocument(catalogKey);
        return definition;
    }

    public async Task<Guid> SaveDocumentAsync(string catalogKey, SalesDocumentModel document)
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

        if (json.RootElement.TryGetProperty("id", out var idElement) && Guid.TryParse(idElement.ToString(), out var id))
        {
            return id;
        }

        return Guid.Empty;
    }

    private async Task<SalesDocumentModel> LoadDocumentAsync(string catalogKey, Guid id)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var endpoint = $"{GetBaseEndpoint(catalogKey)}/{id}";

        return catalogKey switch
        {
            "sales-quotes" => Map(await client.GetFromJsonAsync<SalesQuoteRequest>(endpoint) ?? new SalesQuoteRequest(), catalogKey),
            "sales-orders" => Map(await client.GetFromJsonAsync<SalesOrderRequest>(endpoint) ?? new SalesOrderRequest(), catalogKey),
            "sales-shipments" => Map(await client.GetFromJsonAsync<SalesShipmentRequest>(endpoint) ?? new SalesShipmentRequest(), catalogKey),
            "sales-invoices" => Map(await client.GetFromJsonAsync<SalesInvoiceRequest>(endpoint) ?? new SalesInvoiceRequest(), catalogKey),
            "sales-returns" => Map(await client.GetFromJsonAsync<SalesReturnRequest>(endpoint) ?? new SalesReturnRequest(), catalogKey),
            "credit-notes" => Map(await client.GetFromJsonAsync<CreditNoteRequest>(endpoint) ?? new CreditNoteRequest(), catalogKey),
            _ => CreateEmptyDocument(catalogKey)
        };
    }

    private static SalesDocumentModel CreateEmptyDocument(string catalogKey)
        => new()
        {
            CatalogKey = catalogKey,
            Status = "draft",
            ExchangeRate = 1m,
            IsActive = true,
            DocumentDate = DateTime.Today,
            ValidUntil = DateTime.Today.AddDays(15),
            Lines = [new SalesLineModel { LineNumber = 1, Quantity = 1m }]
        };

    private static SalesDocumentEditorDefinition BuildEditorDefinition(string catalogKey, SalesLookups lookups)
        => catalogKey switch
        {
            "sales-quotes" => new SalesDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de cotización",
                Subtitle = "Captura cliente, vigencia, precios e impuestos por partida.",
                LinesTitle = "Partidas cotizadas",
                RequiresCustomer = true,
                RequiresWarehouse = false,
                RequiresCurrency = true,
                RequiresQuote = false,
                RequiresOrder = false,
                RequiresShipment = false,
                RequiresInvoice = false,
                UsesValidUntil = true,
                UsesReason = false,
                UsesPaymentTermDays = false,
                UsesAmounts = true,
                UsesShipmentQuantities = false,
                Lookups = lookups
            },
            "sales-orders" => new SalesDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de pedido de venta",
                Subtitle = "Completa cliente, moneda, plazo y partidas del pedido.",
                LinesTitle = "Partidas del pedido",
                RequiresCustomer = true,
                RequiresWarehouse = false,
                RequiresCurrency = true,
                RequiresQuote = true,
                RequiresOrder = false,
                RequiresShipment = false,
                RequiresInvoice = false,
                UsesValidUntil = false,
                UsesReason = false,
                UsesPaymentTermDays = true,
                UsesAmounts = true,
                UsesShipmentQuantities = true,
                Lookups = lookups
            },
            "sales-shipments" => new SalesDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de remisión",
                Subtitle = "Surtido parcial o total contra pedido con almacén de salida.",
                LinesTitle = "Partidas surtidas",
                RequiresCustomer = true,
                RequiresWarehouse = true,
                RequiresCurrency = false,
                RequiresQuote = false,
                RequiresOrder = true,
                RequiresShipment = false,
                RequiresInvoice = false,
                UsesValidUntil = false,
                UsesReason = false,
                UsesPaymentTermDays = false,
                UsesAmounts = false,
                UsesShipmentQuantities = false,
                Lookups = lookups
            },
            "sales-invoices" => new SalesDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de factura de venta",
                Subtitle = "Relaciona pedido/remisión y captura importes por partida.",
                LinesTitle = "Partidas facturadas",
                RequiresCustomer = true,
                RequiresWarehouse = false,
                RequiresCurrency = true,
                RequiresQuote = false,
                RequiresOrder = true,
                RequiresShipment = true,
                RequiresInvoice = false,
                UsesValidUntil = false,
                UsesReason = false,
                UsesPaymentTermDays = false,
                UsesAmounts = true,
                UsesShipmentQuantities = false,
                Lookups = lookups
            },
            "sales-returns" => new SalesDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de devolución de venta",
                Subtitle = "Captura origen, motivo y partidas devueltas por el cliente.",
                LinesTitle = "Partidas devueltas",
                RequiresCustomer = true,
                RequiresWarehouse = true,
                RequiresCurrency = false,
                RequiresQuote = false,
                RequiresOrder = false,
                RequiresShipment = true,
                RequiresInvoice = true,
                UsesValidUntil = false,
                UsesReason = true,
                UsesPaymentTermDays = false,
                UsesAmounts = true,
                UsesShipmentQuantities = false,
                Lookups = lookups
            },
            _ => new SalesDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle de nota de crédito",
                Subtitle = "Relaciona la factura origen y captura el ajuste comercial o financiero.",
                LinesTitle = "Partidas acreditadas",
                RequiresCustomer = true,
                RequiresWarehouse = false,
                RequiresCurrency = false,
                RequiresQuote = false,
                RequiresOrder = false,
                RequiresShipment = false,
                RequiresInvoice = true,
                UsesValidUntil = false,
                UsesReason = true,
                UsesPaymentTermDays = false,
                UsesAmounts = true,
                UsesShipmentQuantities = false,
                Lookups = lookups
            }
        };

    private static SalesDocumentModel Map(SalesQuoteRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.SalesQuoteId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        CustomerId = source.CustomerId,
        CurrencyId = source.CurrencyId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.QuoteDate,
        ValidUntil = source.ValidUntil,
        Status = source.Status ?? "draft",
        ExchangeRate = source.ExchangeRate,
        Subtotal = source.Subtotal,
        DiscountAmount = source.DiscountAmount,
        TaxAmount = source.TaxAmount,
        Total = source.Total,
        Notes = source.Notes ?? string.Empty,
        ApprovedAt = source.ApprovedAt,
        ClosedAt = source.ClosedAt,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static SalesDocumentModel Map(SalesOrderRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.SalesOrderId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        CustomerId = source.CustomerId,
        CurrencyId = source.CurrencyId,
        SalesQuoteId = source.SalesQuoteId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.OrderDate,
        Status = source.Status ?? "draft",
        PaymentTermDays = source.PaymentTermDays,
        ExchangeRate = source.ExchangeRate,
        Subtotal = source.Subtotal,
        DiscountAmount = source.DiscountAmount,
        TaxAmount = source.TaxAmount,
        Total = source.Total,
        Notes = source.Notes ?? string.Empty,
        ApprovedAt = source.ApprovedAt,
        ClosedAt = source.ClosedAt,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static SalesDocumentModel Map(SalesShipmentRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.SalesShipmentId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        WarehouseId = source.WarehouseId,
        CustomerId = source.CustomerId,
        SalesOrderId = source.SalesOrderId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.ShipmentDate,
        Status = source.Status ?? "draft",
        Notes = source.Notes ?? string.Empty,
        ApprovedAt = source.ApprovedAt,
        PostedAt = source.PostedAt,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static SalesDocumentModel Map(SalesInvoiceRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.SalesInvoiceId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        CustomerId = source.CustomerId,
        CurrencyId = source.CurrencyId,
        SalesOrderId = source.SalesOrderId,
        SalesShipmentId = source.SalesShipmentId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.InvoiceDate,
        Status = source.Status ?? "draft",
        ExchangeRate = source.ExchangeRate,
        Subtotal = source.Subtotal,
        DiscountAmount = source.DiscountAmount,
        TaxAmount = source.TaxAmount,
        Total = source.Total,
        Notes = source.Notes ?? string.Empty,
        ApprovedAt = source.ApprovedAt,
        PostedAt = source.PostedAt,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static SalesDocumentModel Map(SalesReturnRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.SalesReturnId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        WarehouseId = source.WarehouseId,
        CustomerId = source.CustomerId,
        SalesShipmentId = source.SalesShipmentId,
        SalesInvoiceId = source.SalesInvoiceId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.ReturnDate,
        Status = source.Status ?? "draft",
        Reason = source.Reason ?? string.Empty,
        Subtotal = source.Subtotal,
        TaxAmount = source.TaxAmount,
        Total = source.Total,
        ApprovedAt = source.ApprovedAt,
        PostedAt = source.PostedAt,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static SalesDocumentModel Map(CreditNoteRequest source, string catalogKey) => new()
    {
        CatalogKey = catalogKey,
        Id = source.CreditNoteId,
        CompanyId = source.CompanyId,
        BranchId = source.BranchId,
        CustomerId = source.CustomerId,
        SalesInvoiceId = source.SalesInvoiceId,
        Folio = source.Folio ?? string.Empty,
        DocumentDate = source.CreditNoteDate,
        Status = source.Status ?? "draft",
        Reason = source.Reason ?? string.Empty,
        Subtotal = source.Subtotal,
        TaxAmount = source.TaxAmount,
        Total = source.Total,
        ApprovedAt = source.ApprovedAt,
        PostedAt = source.PostedAt,
        IsActive = source.IsActive,
        Lines = source.Lines.Select(Map).ToList()
    };

    private static SalesLineModel Map(SalesLineRequest source) => new()
    {
        Id = source.Id,
        LineNumber = source.LineNumber,
        SalesOrderLineId = source.SalesOrderLineId,
        SalesInvoiceLineId = source.SalesInvoiceLineId,
        SourceLineId = source.SourceLineId,
        ItemId = source.ItemId,
        UnitId = source.UnitId,
        TaxId = source.TaxId,
        Description = source.Description ?? string.Empty,
        Quantity = source.Quantity,
        ShippedQuantity = source.ShippedQuantity,
        InvoicedQuantity = source.InvoicedQuantity,
        PendingQuantity = source.PendingQuantity,
        UnitPrice = source.UnitPrice,
        DiscountAmount = source.DiscountAmount,
        TaxAmount = source.TaxAmount,
        LineTotal = source.LineTotal
    };

    private static object BuildRequest(string catalogKey, SalesDocumentModel document)
        => catalogKey switch
        {
            "sales-quotes" => new SalesQuoteRequest
            {
                SalesQuoteId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                CustomerId = document.CustomerId,
                CurrencyId = document.CurrencyId,
                Folio = document.Folio,
                QuoteDate = document.DocumentDate,
                ValidUntil = document.ValidUntil,
                Status = document.Status,
                ExchangeRate = document.ExchangeRate,
                Subtotal = document.Subtotal,
                DiscountAmount = document.DiscountAmount,
                TaxAmount = document.TaxAmount,
                Total = document.Total,
                Notes = document.Notes,
                ApprovedAt = document.ApprovedAt,
                ClosedAt = document.ClosedAt,
                IsActive = document.IsActive,
                Lines = document.Lines.Select(MapLine).ToList()
            },
            "sales-orders" => new SalesOrderRequest
            {
                SalesOrderId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                CustomerId = document.CustomerId,
                CurrencyId = document.CurrencyId,
                SalesQuoteId = document.SalesQuoteId,
                Folio = document.Folio,
                OrderDate = document.DocumentDate,
                Status = document.Status,
                PaymentTermDays = document.PaymentTermDays,
                ExchangeRate = document.ExchangeRate,
                Subtotal = document.Subtotal,
                DiscountAmount = document.DiscountAmount,
                TaxAmount = document.TaxAmount,
                Total = document.Total,
                Notes = document.Notes,
                ApprovedAt = document.ApprovedAt,
                ClosedAt = document.ClosedAt,
                IsActive = document.IsActive,
                Lines = document.Lines.Select(MapLine).ToList()
            },
            "sales-shipments" => new SalesShipmentRequest
            {
                SalesShipmentId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                WarehouseId = document.WarehouseId,
                CustomerId = document.CustomerId,
                SalesOrderId = document.SalesOrderId,
                Folio = document.Folio,
                ShipmentDate = document.DocumentDate,
                Status = document.Status,
                Notes = document.Notes,
                ApprovedAt = document.ApprovedAt,
                PostedAt = document.PostedAt,
                IsActive = document.IsActive,
                Lines = document.Lines.Select(MapLine).ToList()
            },
            "sales-invoices" => new SalesInvoiceRequest
            {
                SalesInvoiceId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                CustomerId = document.CustomerId,
                SalesOrderId = document.SalesOrderId,
                SalesShipmentId = document.SalesShipmentId,
                CurrencyId = document.CurrencyId,
                Folio = document.Folio,
                InvoiceDate = document.DocumentDate,
                Status = document.Status,
                ExchangeRate = document.ExchangeRate,
                Subtotal = document.Subtotal,
                DiscountAmount = document.DiscountAmount,
                TaxAmount = document.TaxAmount,
                Total = document.Total,
                Notes = document.Notes,
                ApprovedAt = document.ApprovedAt,
                PostedAt = document.PostedAt,
                IsActive = document.IsActive,
                Lines = document.Lines.Select(MapLine).ToList()
            },
            "sales-returns" => new SalesReturnRequest
            {
                SalesReturnId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                WarehouseId = document.WarehouseId,
                CustomerId = document.CustomerId,
                SalesShipmentId = document.SalesShipmentId,
                SalesInvoiceId = document.SalesInvoiceId,
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
                Lines = document.Lines.Select(MapLine).ToList()
            },
            _ => new CreditNoteRequest
            {
                CreditNoteId = document.Id,
                CompanyId = document.CompanyId,
                BranchId = document.BranchId,
                CustomerId = document.CustomerId,
                SalesInvoiceId = document.SalesInvoiceId,
                Folio = document.Folio,
                CreditNoteDate = document.DocumentDate,
                Reason = document.Reason,
                Status = document.Status,
                Subtotal = document.Subtotal,
                TaxAmount = document.TaxAmount,
                Total = document.Total,
                ApprovedAt = document.ApprovedAt,
                PostedAt = document.PostedAt,
                IsActive = document.IsActive,
                Lines = document.Lines.Select(MapLine).ToList()
            }
        };

    private static SalesLineRequest MapLine(SalesLineModel source) => new()
    {
        Id = source.Id,
        LineNumber = source.LineNumber,
        SalesOrderLineId = source.SalesOrderLineId,
        SalesInvoiceLineId = source.SalesInvoiceLineId,
        SourceLineId = source.SourceLineId,
        ItemId = source.ItemId,
        UnitId = source.UnitId,
        TaxId = source.TaxId,
        Description = source.Description,
        Quantity = source.Quantity,
        ShippedQuantity = source.ShippedQuantity,
        InvoicedQuantity = source.InvoicedQuantity,
        PendingQuantity = source.PendingQuantity,
        UnitPrice = source.UnitPrice,
        DiscountAmount = source.DiscountAmount,
        TaxAmount = source.TaxAmount,
        LineTotal = source.LineTotal
    };

    private static string GetBaseEndpoint(string catalogKey) => catalogKey switch
    {
        "sales-quotes" => "/api/sales/quotes",
        "sales-orders" => "/api/sales/orders",
        "sales-shipments" => "/api/sales/shipments",
        "sales-invoices" => "/api/sales/invoices",
        "sales-returns" => "/api/sales/returns",
        "credit-notes" => "/api/sales/credit-notes",
        _ => throw new InvalidOperationException($"No existe un endpoint para el catálogo '{catalogKey}'.")
    };

    private async Task<CatalogViewDefinition> GetCatalogFromEndpointAsync(string catalogKey, string title, string subtitle, string keyExpr, string endpoint, Func<Task<List<CatalogColumnDefinition>>> columnsFactory)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<Dictionary<string, object?>>>(endpoint) ?? [];
        var columns = await columnsFactory();
        return new CatalogViewDefinition
        {
            CatalogKey = catalogKey,
            Title = title,
            Subtitle = subtitle,
            KeyExpr = keyExpr,
            AllowCreate = true,
            AllowUpdate = true,
            AllowDelete = true,
            Rows = rows.Select(NormalizeRowKeys).ToList(),
            Columns = columns,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => x.TryGetValue("IsActive", out var active) && IsTrue(active)),
            InactiveCount = rows.Count(x => !x.TryGetValue("IsActive", out var active) || !IsTrue(active))
        };
    }

    private static Dictionary<string, object?> NormalizeRowKeys(Dictionary<string, object?> source)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Key)) continue;
            var key = char.ToUpperInvariant(pair.Key[0]) + pair.Key[1..];
            normalized[key] = pair.Value;
        }
        return normalized;
    }

    private async Task<List<CatalogColumnDefinition>> BuildQuoteColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("SalesQuoteId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("CustomerId", "Cliente", lookups.Customers, true, 220),
            LookupColumn("CurrencyId", "Moneda", lookups.Currencies, false, 150, "currencies"),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("QuoteDate", "Fecha", true, 120),
            DateColumn("ValidUntil", "Vigencia", false, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            NumberColumn("ExchangeRate", "TC", false, 90),
            NumberColumn("Subtotal", "Subtotal", false, 120),
            NumberColumn("DiscountAmount", "Descuento", false, 120),
            NumberColumn("TaxAmount", "Impuesto", false, 120),
            NumberColumn("Total", "Total", false, 120),
            TextColumn("Notes", "Notas", false, true, 220),
            DateColumn("ApprovedAt", "Aprobada", false, 150),
            DateColumn("ClosedAt", "Cerrada", false, 150),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildOrderColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("SalesOrderId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("CustomerId", "Cliente", lookups.Customers, true, 220),
            LookupColumn("CurrencyId", "Moneda", lookups.Currencies, false, 150, "currencies"),
            LookupColumn("SalesQuoteId", "Cotización", lookups.Quotes, false, 150),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("OrderDate", "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            NumberColumn("PaymentTermDays", "Plazo", false, 90),
            NumberColumn("ExchangeRate", "TC", false, 90),
            NumberColumn("Subtotal", "Subtotal", false, 120),
            NumberColumn("DiscountAmount", "Descuento", false, 120),
            NumberColumn("TaxAmount", "Impuesto", false, 120),
            NumberColumn("Total", "Total", false, 120),
            TextColumn("Notes", "Notas", false, true, 200),
            DateColumn("ApprovedAt", "Aprobado", false, 150),
            DateColumn("ClosedAt", "Cerrado", false, 150),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildShipmentColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("SalesShipmentId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("WarehouseId", "Almacén", lookups.Warehouses, true, 220, "warehouses"),
            LookupColumn("CustomerId", "Cliente", lookups.Customers, false, 220),
            LookupColumn("SalesOrderId", "Pedido", lookups.Orders, false, 150),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("ShipmentDate", "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            TextColumn("Notes", "Notas", false, true, 220),
            DateColumn("ApprovedAt", "Aprobada", false, 150),
            DateColumn("PostedAt", "Posteada", false, 150),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildInvoiceColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("SalesInvoiceId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("CustomerId", "Cliente", lookups.Customers, true, 220),
            LookupColumn("SalesOrderId", "Pedido", lookups.Orders, false, 150),
            LookupColumn("SalesShipmentId", "Remisión", lookups.Shipments, false, 150),
            LookupColumn("CurrencyId", "Moneda", lookups.Currencies, false, 150, "currencies"),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("InvoiceDate", "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            NumberColumn("ExchangeRate", "TC", false, 90),
            NumberColumn("Subtotal", "Subtotal", false, 120),
            NumberColumn("DiscountAmount", "Descuento", false, 120),
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
            TextColumn("SalesReturnId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("WarehouseId", "Almacén", lookups.Warehouses, true, 220, "warehouses"),
            LookupColumn("CustomerId", "Cliente", lookups.Customers, true, 220),
            LookupColumn("SalesShipmentId", "Remisión", lookups.Shipments, false, 150),
            LookupColumn("SalesInvoiceId", "Factura", lookups.Invoices, false, 150),
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

    private async Task<List<CatalogColumnDefinition>> BuildCreditNoteColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("CreditNoteId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 220),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 220),
            LookupColumn("CustomerId", "Cliente", lookups.Customers, true, 220),
            LookupColumn("SalesInvoiceId", "Factura", lookups.Invoices, true, 150),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("CreditNoteDate", "Fecha", true, 120),
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

    private async Task<SalesLookups> GetLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<SalesLookups>("/api/sales/lookups") ?? new SalesLookups();
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

public sealed class SalesDocumentEditorDefinition
{
    public string CatalogKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string LinesTitle { get; set; } = string.Empty;
    public bool RequiresCustomer { get; set; }
    public bool RequiresWarehouse { get; set; }
    public bool RequiresCurrency { get; set; }
    public bool RequiresQuote { get; set; }
    public bool RequiresOrder { get; set; }
    public bool RequiresShipment { get; set; }
    public bool RequiresInvoice { get; set; }
    public bool UsesValidUntil { get; set; }
    public bool UsesReason { get; set; }
    public bool UsesPaymentTermDays { get; set; }
    public bool UsesAmounts { get; set; }
    public bool UsesShipmentQuantities { get; set; }
    public SalesLookups Lookups { get; set; } = new();
    public SalesDocumentModel Document { get; set; } = new();
}

public sealed class SalesDocumentModel
{
    public string CatalogKey { get; set; } = string.Empty;
    public Guid? Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? SalesQuoteId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? SalesShipmentId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; } = DateTime.Today;
    public DateTime? ValidUntil { get; set; }
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineModel> Lines { get; set; } = [];
}

public sealed class SalesLineModel
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? SalesOrderLineId { get; set; }
    public Guid? SalesInvoiceLineId { get; set; }
    public Guid? SourceLineId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal PendingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

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

public sealed class SalesLookups
{
    public List<CatalogLookupItem> Companies { get; set; } = [];
    public List<CatalogLookupItem> Branches { get; set; } = [];
    public List<CatalogLookupItem> Warehouses { get; set; } = [];
    public List<CatalogLookupItem> Customers { get; set; } = [];
    public List<CatalogLookupItem> Currencies { get; set; } = [];
    public List<CatalogLookupItem> Quotes { get; set; } = [];
    public List<CatalogLookupItem> Orders { get; set; } = [];
    public List<CatalogLookupItem> Shipments { get; set; } = [];
    public List<CatalogLookupItem> Invoices { get; set; } = [];
    public List<CatalogLookupItem> Returns { get; set; } = [];
    public List<CatalogLookupItem> CreditNotes { get; set; } = [];
    public List<CatalogLookupItem> Items { get; set; } = [];
    public List<CatalogLookupItem> Units { get; set; } = [];
    public List<CatalogLookupItem> Taxes { get; set; } = [];
}

public sealed class SalesDashboardSummaryDto
{
    public int OpenQuotes { get; set; }
    public int OpenOrders { get; set; }
    public int RecentShipments { get; set; }
    public int RecentInvoices { get; set; }
    public decimal PeriodSales { get; set; }
    public decimal ReturnsAmount { get; set; }
    public decimal CreditNotesAmount { get; set; }
}

public sealed class SalesLineRequest
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? SalesOrderLineId { get; set; }
    public Guid? SalesInvoiceLineId { get; set; }
    public Guid? SourceLineId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal PendingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class SalesQuoteRequest
{
    public Guid? SalesQuoteId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? Folio { get; set; }
    public DateTime? QuoteDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Status { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class SalesOrderRequest
{
    public Guid? SalesOrderId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? SalesQuoteId { get; set; }
    public string? Folio { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? Status { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class SalesShipmentRequest
{
    public Guid? SalesShipmentId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? Folio { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class SalesInvoiceRequest
{
    public Guid? SalesInvoiceId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? SalesShipmentId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? Folio { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Status { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class SalesReturnRequest
{
    public Guid? SalesReturnId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesShipmentId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
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
    public List<SalesLineRequest> Lines { get; set; } = [];
}

public sealed class CreditNoteRequest
{
    public Guid? CreditNoteId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public string? Folio { get; set; }
    public DateTime? CreditNoteDate { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SalesLineRequest> Lines { get; set; } = [];
}
