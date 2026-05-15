using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.Inventory;

public sealed class InventoryApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public InventoryApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey switch
        {
            "stock-balances" => BuildCatalogAsync(catalogKey, "Existencias", "Existencias por almacén, producto, lote y serie.", "StockBalanceId", "/api/inventory/stock-balances", BuildStockColumnsAsync, false, false, false),
            "kardex" => BuildCatalogAsync(catalogKey, "Kardex", "Movimientos valorizados por producto y documento.", "InventoryMovementId", "/api/inventory/kardex", BuildKardexColumnsAsync, false, false, false),
            "inventory-entries" => BuildCatalogAsync(catalogKey, "Entradas", "Entradas con partidas, costo y referencia.", "InventoryEntryId", "/api/inventory/entries", BuildEntryColumnsAsync),
            "inventory-exits" => BuildCatalogAsync(catalogKey, "Salidas", "Salidas con productos, cantidades y costo.", "InventoryExitId", "/api/inventory/exits", BuildExitColumnsAsync),
            "inventory-transfers" => BuildCatalogAsync(catalogKey, "Traspasos", "Traspasos entre almacenes con detalle de líneas.", "InventoryTransferId", "/api/inventory/transfers", BuildTransferColumnsAsync),
            "inventory-adjustments" => BuildCatalogAsync(catalogKey, "Ajustes", "Ajustes positivos o negativos por producto.", "InventoryAdjustmentId", "/api/inventory/adjustments", BuildAdjustmentColumnsAsync),
            "physical-counts" => BuildCatalogAsync(catalogKey, "Conteos físicos", "Conteos con cantidad sistema, contada y diferencia.", "PhysicalCountId", "/api/inventory/physical-counts", BuildPhysicalCountColumnsAsync),
            "lots" => BuildCatalogAsync(catalogKey, "Lotes", "Consulta de lotes con existencia disponible.", "ItemLotId", "/api/inventory/lots", BuildLotColumnsAsync, false, false, false),
            "serials" => BuildCatalogAsync(catalogKey, "Series", "Consulta de números de serie individuales.", "ItemSerialId", "/api/inventory/serials", BuildSerialColumnsAsync, false, false, false),
            _ => throw new InvalidOperationException($"No existe el catálogo '{catalogKey}'.")
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

    public async Task<InventoryDocumentEditorDefinition> GetEditorDefinitionAsync(string catalogKey, Guid? id = null)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var lookups = await client.GetFromJsonAsync<InventoryLookups>("/api/inventory/lookups") ?? new();
        var definition = BuildEditorDefinition(catalogKey, lookups);
        definition.Document = id.HasValue
            ? await LoadDocumentAsync(catalogKey, id.Value)
            : CreateEmptyDocument(catalogKey);
        return definition;
    }

    public async Task<Guid> SaveDocumentAsync(string catalogKey, InventoryDocumentModel document)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var endpoint = GetBaseEndpoint(catalogKey);
        var payload = BuildRequest(document);
        HttpResponseMessage response;

        if (document.Id.HasValue)
        {
            response = await client.PutAsJsonAsync($"{endpoint}/{document.Id.Value}", payload);
        }
        else
        {
            response = await client.PostAsJsonAsync(endpoint, payload);
        }

        await EnsureSuccessAsync(response);

        if (document.Id.HasValue)
        {
            return document.Id.Value;
        }

        var body = await response.Content.ReadFromJsonAsync<SaveResponse>();
        return body?.Id ?? Guid.Empty;
    }

    public async Task<InventoryDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<InventoryDashboardSummaryDto>("/api/inventory/dashboard/summary") ?? new();
    }

    public async Task<List<InventoryStockDetailRow>> GetStockByItemAsync(Guid itemId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<InventoryStockDetailRow>>($"/api/inventory/stock-balances/by-item/{itemId}") ?? [];
    }

    public async Task<List<InventoryKardexDetailRow>> GetKardexByItemAsync(Guid itemId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<InventoryKardexDetailRow>>($"/api/inventory/kardex/by-item/{itemId}") ?? [];
    }

    private async Task<InventoryDocumentModel> LoadDocumentAsync(string catalogKey, Guid id)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var request = await client.GetFromJsonAsync<InventoryDocumentRequest>($"{GetBaseEndpoint(catalogKey)}/{id}") ?? new InventoryDocumentRequest();
        return Map(request, catalogKey);
    }

    private static InventoryDocumentEditorDefinition BuildEditorDefinition(string catalogKey, InventoryLookups lookups)
        => catalogKey switch
        {
            "inventory-entries" => new InventoryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle entrada de inventario",
                Subtitle = "Captura encabezado, producto, costo, lote y serie por partida.",
                LinesTitle = "Partidas de entrada",
                RequiresSingleWarehouse = true,
                RequiresSourceTargetWarehouse = false,
                RequiresReason = true,
                RequiresAdjustmentType = false,
                UsesCostLines = true,
                UsesCountLines = false,
                Lookups = lookups
            },
            "inventory-exits" => new InventoryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle salida de inventario",
                Subtitle = "Captura productos, cantidades, costo y documento de salida.",
                LinesTitle = "Partidas de salida",
                RequiresSingleWarehouse = true,
                RequiresSourceTargetWarehouse = false,
                RequiresReason = true,
                RequiresAdjustmentType = false,
                UsesCostLines = true,
                UsesCountLines = false,
                Lookups = lookups
            },
            "inventory-transfers" => new InventoryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle traspaso",
                Subtitle = "Define almacén origen, destino y partidas a mover.",
                LinesTitle = "Partidas del traspaso",
                RequiresSingleWarehouse = false,
                RequiresSourceTargetWarehouse = true,
                RequiresReason = true,
                RequiresAdjustmentType = false,
                UsesCostLines = true,
                UsesCountLines = false,
                Lookups = lookups
            },
            "inventory-adjustments" => new InventoryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle ajuste de inventario",
                Subtitle = "Registra ajustes positivos o negativos con costo por partida.",
                LinesTitle = "Partidas del ajuste",
                RequiresSingleWarehouse = true,
                RequiresSourceTargetWarehouse = false,
                RequiresReason = true,
                RequiresAdjustmentType = true,
                UsesCostLines = true,
                UsesCountLines = false,
                Lookups = lookups
            },
            _ => new InventoryDocumentEditorDefinition
            {
                CatalogKey = catalogKey,
                Title = "Detalle conteo físico",
                Subtitle = "Captura cantidad sistema, contada y diferencia por producto.",
                LinesTitle = "Partidas del conteo",
                RequiresSingleWarehouse = true,
                RequiresSourceTargetWarehouse = false,
                RequiresReason = false,
                RequiresAdjustmentType = false,
                UsesCostLines = false,
                UsesCountLines = true,
                Lookups = lookups
            }
        };

    private static InventoryDocumentModel CreateEmptyDocument(string catalogKey)
        => new()
        {
            CatalogKey = catalogKey,
            Status = "draft",
            DocumentDate = DateTime.Today,
            IsActive = true,
            AdjustmentType = "positive",
            Lines = [new InventoryLineModel { LineNumber = 1, Quantity = 1m, CountedQuantity = 0m, SystemQuantity = 0m }]
        };

    private static InventoryDocumentModel Map(InventoryDocumentRequest source, string catalogKey)
        => new()
        {
            CatalogKey = catalogKey,
            Id = source.InventoryEntryId ?? source.InventoryExitId ?? source.InventoryTransferId ?? source.InventoryAdjustmentId ?? source.PhysicalCountId,
            CompanyId = source.CompanyId,
            BranchId = source.BranchId,
            WarehouseId = source.WarehouseId,
            SourceWarehouseId = source.SourceWarehouseId,
            TargetWarehouseId = source.TargetWarehouseId,
            Folio = source.Folio ?? string.Empty,
            DocumentDate = source.DocumentDate,
            Status = source.Status ?? "draft",
            Reason = source.Reason ?? string.Empty,
            Notes = source.Notes ?? string.Empty,
            ApprovedAt = source.ApprovedAt,
            PostedAt = source.PostedAt,
            ClosedAt = source.ClosedAt,
            AdjustmentType = source.AdjustmentType ?? "positive",
            IsActive = source.IsActive,
            Lines = source.Lines.Select(MapLine).ToList()
        };

    private static InventoryLineModel MapLine(InventoryLineRequest source)
        => new()
        {
            Id = source.Id,
            LineNumber = source.LineNumber,
            ItemId = source.ItemId,
            UnitId = source.UnitId,
            LotId = source.LotId,
            SerialId = source.SerialId,
            Description = source.Description ?? string.Empty,
            Quantity = source.Quantity,
            UnitCost = source.UnitCost,
            LineTotal = source.LineTotal,
            SystemQuantity = source.SystemQuantity,
            CountedQuantity = source.CountedQuantity,
            DifferenceQuantity = source.DifferenceQuantity,
            ItemCode = source.ItemCode ?? string.Empty,
            ItemName = source.ItemName ?? string.Empty,
            UnitName = source.UnitName ?? string.Empty,
            LotNumber = source.LotNumber ?? string.Empty,
            SerialNumber = source.SerialNumber ?? string.Empty
        };

    private static InventoryDocumentRequest BuildRequest(InventoryDocumentModel document)
        => new()
        {
            InventoryEntryId = document.CatalogKey == "inventory-entries" ? document.Id : null,
            InventoryExitId = document.CatalogKey == "inventory-exits" ? document.Id : null,
            InventoryTransferId = document.CatalogKey == "inventory-transfers" ? document.Id : null,
            InventoryAdjustmentId = document.CatalogKey == "inventory-adjustments" ? document.Id : null,
            PhysicalCountId = document.CatalogKey == "physical-counts" ? document.Id : null,
            CompanyId = document.CompanyId,
            BranchId = document.BranchId,
            WarehouseId = document.WarehouseId,
            SourceWarehouseId = document.SourceWarehouseId,
            TargetWarehouseId = document.TargetWarehouseId,
            Folio = document.Folio,
            DocumentDate = document.DocumentDate,
            Status = document.Status,
            Reason = document.Reason,
            Notes = document.Notes,
            ApprovedAt = document.ApprovedAt,
            PostedAt = document.PostedAt,
            ClosedAt = document.ClosedAt,
            AdjustmentType = document.AdjustmentType,
            IsActive = document.IsActive,
            Lines = document.Lines.Select(x => new InventoryLineRequest
            {
                Id = x.Id,
                LineNumber = x.LineNumber,
                ItemId = x.ItemId,
                UnitId = x.UnitId,
                LotId = x.LotId,
                SerialId = x.SerialId,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitCost = x.UnitCost,
                LineTotal = x.LineTotal,
                SystemQuantity = x.SystemQuantity,
                CountedQuantity = x.CountedQuantity,
                DifferenceQuantity = x.DifferenceQuantity
            }).ToList()
        };

    private async Task<CatalogViewDefinition> BuildCatalogAsync(string catalogKey, string title, string subtitle, string keyExpr, string endpoint, Func<Task<List<CatalogColumnDefinition>>> columnsFactory, bool allowCreate = true, bool allowUpdate = true, bool allowDelete = true)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<Dictionary<string, object?>>>(endpoint) ?? [];
        var normalizedRows = rows.Select(NormalizeKeys).ToList();

        return new CatalogViewDefinition
        {
            CatalogKey = catalogKey,
            Title = title,
            Subtitle = subtitle,
            KeyExpr = keyExpr,
            AllowCreate = allowCreate,
            AllowUpdate = allowUpdate,
            AllowDelete = allowDelete,
            TotalCount = normalizedRows.Count,
            ActiveCount = normalizedRows.Count(x => x.TryGetValue("IsActive", out var v) && string.Equals(v?.ToString(), "true", StringComparison.OrdinalIgnoreCase)),
            InactiveCount = normalizedRows.Count(x => !x.TryGetValue("IsActive", out var v) || !string.Equals(v?.ToString(), "true", StringComparison.OrdinalIgnoreCase)),
            Columns = await columnsFactory(),
            Rows = normalizedRows
        };
    }

    private async Task<List<CatalogColumnDefinition>> BuildStockColumnsAsync()
        =>
        [
            TextColumn("StockBalanceId", "ID", false, false, 220),
            TextColumn("WarehouseName", "Almacén", false, false, 170),
            TextColumn("ItemCode", "Código", false, false, 110),
            TextColumn("ItemName", "Producto", false, false, 240),
            TextColumn("LotNumber", "Lote", false, false, 120),
            TextColumn("SerialNumber", "Serie", false, false, 140),
            NumberColumn("QuantityOnHand", "Existencia", false, 110),
            NumberColumn("QuantityReserved", "Reservado", false, 110),
            NumberColumn("QuantityAvailable", "Disponible", false, 110),
            NumberColumn("AverageCost", "Costo promedio", false, 120),
            NumberColumn("LastCost", "Último costo", false, 120),
            BoolColumn("IsActive", "Activo", 90)
        ];

    private async Task<List<CatalogColumnDefinition>> BuildKardexColumnsAsync()
        =>
        [
            TextColumn("InventoryMovementId", "ID", false, false, 220),
            TextColumn("WarehouseName", "Almacén", false, false, 170),
            TextColumn("ItemCode", "Código", false, false, 110),
            TextColumn("ItemName", "Producto", false, false, 240),
            TextColumn("MovementType", "Tipo", false, false, 120),
            TextColumn("DocumentType", "Documento", false, false, 140),
            DateColumn("MovementDate", "Fecha", false, 120),
            NumberColumn("QuantityIn", "Entrada", false, 90),
            NumberColumn("QuantityOut", "Salida", false, 90),
            NumberColumn("BalanceAfter", "Saldo", false, 90),
            NumberColumn("UnitCost", "Costo U.", false, 110),
            NumberColumn("TotalCost", "Costo T.", false, 110),
            TextColumn("Reference", "Referencia", false, false, 180)
        ];

    private async Task<List<CatalogColumnDefinition>> BuildEntryColumnsAsync() => await BuildSimpleDocumentColumnsAsync("InventoryEntryId", "EntryDate", false);
    private async Task<List<CatalogColumnDefinition>> BuildExitColumnsAsync() => await BuildSimpleDocumentColumnsAsync("InventoryExitId", "ExitDate", false);
    private async Task<List<CatalogColumnDefinition>> BuildAdjustmentColumnsAsync()
    {
        var columns = await BuildSimpleDocumentColumnsAsync("InventoryAdjustmentId", "AdjustmentDate", true);
        columns.Insert(7, TextColumn("AdjustmentType", "Tipo ajuste", false, true, 120));
        return columns;
    }

    private async Task<List<CatalogColumnDefinition>> BuildTransferColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("InventoryTransferId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 180),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 180),
            LookupColumn("SourceWarehouseId", "Origen", lookups.Warehouses, true, 180),
            LookupColumn("TargetWarehouseId", "Destino", lookups.Warehouses, true, 180),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("TransferDate", "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            TextColumn("Reason", "Motivo", false, true, 200),
            TextColumn("Notes", "Notas", false, true, 220),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildPhysicalCountColumnsAsync()
    {
        var lookups = await GetLookupsAsync();
        return
        [
            TextColumn("PhysicalCountId", "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 180),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 180),
            LookupColumn("WarehouseId", "Almacén", lookups.Warehouses, true, 180),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn("CountDate", "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            DateColumn("ClosedAt", "Cierre", false, 140),
            TextColumn("Notes", "Notas", false, true, 220),
            BoolColumn("IsActive", "Activo", 90)
        ];
    }

    private async Task<List<CatalogColumnDefinition>> BuildLotColumnsAsync()
        =>
        [
            TextColumn("ItemLotId", "ID", false, false, 220),
            TextColumn("ItemId", "ItemId", false, false, 220),
            TextColumn("WarehouseId", "WarehouseId", false, false, 180),
            TextColumn("LotNumber", "Lote", false, false, 140),
            TextColumn("Status", "Estatus", false, false, 120),
            NumberColumn("QuantityOnHand", "Existencia", false, 120),
            BoolColumn("IsActive", "Activo", 90)
        ];

    private async Task<List<CatalogColumnDefinition>> BuildSerialColumnsAsync()
        =>
        [
            TextColumn("ItemSerialId", "ID", false, false, 220),
            TextColumn("ItemId", "ItemId", false, false, 220),
            TextColumn("WarehouseId", "WarehouseId", false, false, 180),
            TextColumn("SerialNumber", "Serie", false, false, 140),
            TextColumn("Status", "Estatus", false, false, 120),
            TextColumn("DocumentType", "Documento", false, false, 140),
            BoolColumn("IsActive", "Activo", 90)
        ];

    private async Task<List<CatalogColumnDefinition>> BuildSimpleDocumentColumnsAsync(string idField, string dateField, bool hasAdjustmentType)
    {
        var lookups = await GetLookupsAsync();
        var columns = new List<CatalogColumnDefinition>
        {
            TextColumn(idField, "ID", false, false, 220),
            LookupColumn("CompanyId", "Empresa", lookups.Companies, true, 180),
            LookupColumn("BranchId", "Sucursal", lookups.Branches, true, 180),
            LookupColumn("WarehouseId", "Almacén", lookups.Warehouses, true, 180),
            TextColumn("Folio", "Folio", true, true, 130),
            DateColumn(dateField, "Fecha", true, 120),
            LookupColumn("Status", "Estatus", StatusLookups(), true, 140),
            TextColumn("Reason", "Motivo", false, true, 200),
            TextColumn("Notes", "Notas", false, true, 220),
            BoolColumn("IsActive", "Activo", 90)
        };

        if (hasAdjustmentType)
        {
            columns.Insert(7, TextColumn("AdjustmentType", "Tipo ajuste", false, true, 120));
        }

        return columns;
    }

    private async Task<InventoryLookups> GetLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<InventoryLookups>("/api/inventory/lookups") ?? new();
    }

    private static Dictionary<string, object?> NormalizeKeys(Dictionary<string, object?> source)
    {
        var target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
        {
            var key = string.IsNullOrWhiteSpace(item.Key) ? item.Key : char.ToUpperInvariant(item.Key[0]) + item.Key[1..];
            target[key] = item.Value;
        }
        return target;
    }

    private static string GetBaseEndpoint(string catalogKey)
        => catalogKey switch
        {
            "inventory-entries" => "/api/inventory/entries",
            "inventory-exits" => "/api/inventory/exits",
            "inventory-transfers" => "/api/inventory/transfers",
            "inventory-adjustments" => "/api/inventory/adjustments",
            "physical-counts" => "/api/inventory/physical-counts",
            _ => throw new InvalidOperationException($"No existe endpoint para '{catalogKey}'.")
        };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
            ? $"La API devolvió {(int)response.StatusCode}."
            : error);
    }

    private static CatalogColumnDefinition TextColumn(string dataField, string caption, bool required, bool allowEditing, int width)
        => new()
        {
            DataField = dataField,
            Caption = caption,
            DataType = "string",
            Required = required,
            AllowEditing = allowEditing,
            Width = width
        };

    private static CatalogColumnDefinition NumberColumn(string dataField, string caption, bool required, int width)
        => new()
        {
            DataField = dataField,
            Caption = caption,
            DataType = "number",
            Required = required,
            AllowEditing = required,
            Width = width
        };

    private static CatalogColumnDefinition DateColumn(string dataField, string caption, bool required, int width)
        => new()
        {
            DataField = dataField,
            Caption = caption,
            DataType = "date",
            Required = required,
            AllowEditing = required,
            Width = width
        };

    private static CatalogColumnDefinition BoolColumn(string dataField, string caption, int width)
        => new()
        {
            DataField = dataField,
            Caption = caption,
            DataType = "boolean",
            Width = width
        };

    private static CatalogColumnDefinition LookupColumn(string dataField, string caption, IReadOnlyList<CatalogLookupItem> items, bool required, int width)
        => new()
        {
            DataField = dataField,
            Caption = caption,
            DataType = "string",
            Required = required,
            AllowEditing = required,
            Width = width,
            UseLookup = true,
            LookupItems = items.ToList()
        };

    private static IReadOnlyList<CatalogLookupItem> StatusLookups()
        =>
        [
            new CatalogLookupItem { Id = "draft", Name = "Draft" },
            new CatalogLookupItem { Id = "pending_approval", Name = "Pending approval" },
            new CatalogLookupItem { Id = "approved", Name = "Approved" },
            new CatalogLookupItem { Id = "posted", Name = "Posted" },
            new CatalogLookupItem { Id = "closed", Name = "Closed" },
            new CatalogLookupItem { Id = "cancelled", Name = "Cancelled" }
        ];
}

public sealed class InventoryDashboardSummaryDto
{
    public int TotalStockRows { get; set; }
    public decimal TotalOnHand { get; set; }
    public decimal TotalAvailable { get; set; }
    public int TotalMovements { get; set; }
    public int ActiveLots { get; set; }
    public int ActiveSerials { get; set; }
}

public sealed class InventoryDocumentEditorDefinition
{
    public string CatalogKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string LinesTitle { get; set; } = string.Empty;
    public bool RequiresSingleWarehouse { get; set; }
    public bool RequiresSourceTargetWarehouse { get; set; }
    public bool RequiresReason { get; set; }
    public bool RequiresAdjustmentType { get; set; }
    public bool UsesCostLines { get; set; }
    public bool UsesCountLines { get; set; }
    public InventoryLookups Lookups { get; set; } = new();
    public InventoryDocumentModel Document { get; set; } = new();
}

public sealed class InventoryDocumentModel
{
    public string CatalogKey { get; set; } = string.Empty;
    public Guid? Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public Guid? TargetWarehouseId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }
    public string Status { get; set; } = "draft";
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string AdjustmentType { get; set; } = "positive";
    public bool IsActive { get; set; } = true;
    public List<InventoryLineModel> Lines { get; set; } = [];
}

public sealed class InventoryLineModel
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;

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

    public string LotIdString
    {
        get => LotId?.ToString() ?? string.Empty;
        set => LotId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    public string SerialIdString
    {
        get => SerialId?.ToString() ?? string.Empty;
        set => SerialId = Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

public sealed class InventoryDocumentRequest
{
    public Guid? InventoryEntryId { get; set; }
    public Guid? InventoryExitId { get; set; }
    public Guid? InventoryTransferId { get; set; }
    public Guid? InventoryAdjustmentId { get; set; }
    public Guid? PhysicalCountId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public Guid? TargetWarehouseId { get; set; }
    public string? Folio { get; set; }
    public DateTime? DocumentDate { get; set; }
    public string? Status { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? AdjustmentType { get; set; }
    public bool IsActive { get; set; } = true;
    public List<InventoryLineRequest> Lines { get; set; } = [];
}

public sealed class InventoryLineRequest
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? SerialId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? UnitName { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public sealed class InventoryLookups
{
    public List<CatalogLookupItem> Companies { get; set; } = [];
    public List<CatalogLookupItem> Branches { get; set; } = [];
    public List<CatalogLookupItem> Warehouses { get; set; } = [];
    public List<CatalogLookupItem> Items { get; set; } = [];
    public List<CatalogLookupItem> Units { get; set; } = [];
    public List<CatalogLookupItem> Lots { get; set; } = [];
    public List<CatalogLookupItem> Serials { get; set; } = [];
}

public sealed class InventoryStockDetailRow
{
    public Guid StockBalanceId { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid? SerialId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal AverageCost { get; set; }
    public decimal LastCost { get; set; }
}

public sealed class InventoryKardexDetailRow
{
    public Guid InventoryMovementId { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public DateTime MovementDate { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Reference { get; set; } = string.Empty;
}

internal sealed class SaveResponse
{
    public Guid Id { get; set; }
}
