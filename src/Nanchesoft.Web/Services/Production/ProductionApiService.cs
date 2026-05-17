using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.Production;

public sealed class ProductionApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductionApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // ── public dashboard helpers ────────────────────────────────────────────

    public async Task<ProductionKpiDto?> GetKpisAsync(Guid companyId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<ProductionKpiDto>(
            $"/api/production/dashboard/kpis?companyId={companyId}");
    }

    public async Task<OrdersBoardDto?> GetOrdersBoardAsync(Guid companyId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<OrdersBoardDto>(
            $"/api/production/dashboard/orders-board?companyId={companyId}");
    }

    public async Task<List<PhaseThroughputDto>> GetPhaseThroughputAsync(Guid companyId, string period = "week")
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<PhaseThroughputDto>>(
            $"/api/production/dashboard/phase-throughput?companyId={companyId}&period={period}") ?? [];
    }

    // ── CatalogCrudPage catalogs ─────────────────────────────────────────────

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey.ToLowerInvariant() switch
        {
            "production-orders"   => GetProductionOrdersAsync(),
            "production-schedules" => GetProductionSchedulesAsync(),
            "production-vouchers" => GetProductionVouchersAsync(),
            "piecework-records"   => GetPieceWorkRecordsAsync(),
            "piecework-rates"     => GetPieceWorkRatesAsync(),
            "production-cells"    => GetProductionCellsAsync(),
            "production-in-process" => GetInProcessAsync(),
            "production-surplus"  => GetSurplusAsync(),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = catalogKey.ToLowerInvariant() switch
        {
            "production-orders"   => await client.PostAsJsonAsync("/api/production/orders",   MapOrderRequest(payload)),
            "production-vouchers" => await client.PostAsJsonAsync("/api/production/vouchers", MapVoucherRequest(payload)),
            "piecework-records"   => await client.PostAsJsonAsync("/api/production/piecework/records", MapPieceWorkRequest(payload)),
            "piecework-rates"     => await client.PostAsJsonAsync("/api/production/piecework/rates",   MapRateRequest(payload)),
            "production-cells"    => await client.PostAsJsonAsync("/api/production/cells",    MapCellRequest(payload)),
            "production-surplus"  => await client.PostAsJsonAsync("/api/production/surplus",  MapSurplusRequest(payload)),
            _ => throw new InvalidOperationException($"No se puede insertar en '{catalogKey}'.")
        };
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = catalogKey.ToLowerInvariant() switch
        {
            "production-orders"   => await client.PutAsJsonAsync($"/api/production/orders/{key}",          MapOrderUpdateRequest(payload)),
            "piecework-rates"     => await client.PutAsJsonAsync($"/api/production/piecework/rates/{key}", MapRateRequest(payload)),
            "production-cells"    => await client.PutAsJsonAsync($"/api/production/cells/{key}",           MapCellRequest(payload)),
            _ => throw new InvalidOperationException($"No se puede actualizar '{catalogKey}'.")
        };
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var endpoint = catalogKey.ToLowerInvariant() switch
        {
            "production-orders"   => $"/api/production/orders/{key}",
            "piecework-rates"     => $"/api/production/piecework/rates/{key}",
            _ => throw new InvalidOperationException($"No se puede eliminar en '{catalogKey}'.")
        };
        var response = await client.DeleteAsync(endpoint);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    // ── order transitions ────────────────────────────────────────────────────

    public async Task<HttpResponseMessage> TransitionOrderAsync(Guid orderId, string action)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.PostAsync($"/api/production/orders/{orderId}/{action}", null);
    }

    public async Task<HttpResponseMessage> ApproveVoucherAsync(Guid voucherId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.PostAsync($"/api/production/vouchers/{voucherId}/complete", null);
    }

    public async Task<HttpResponseMessage> PrintVoucherAsync(Guid voucherId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.PostAsync($"/api/production/vouchers/{voucherId}/print", null);
    }

    public async Task<HttpResponseMessage> ApprovePieceWorkAsync(Guid recordId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.PostAsync($"/api/production/piecework/records/{recordId}/approve", null);
    }

    // ── catalog builders ──────────────────────────────────────────────────────

    private async Task<CatalogViewDefinition> GetProductionOrdersAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await GetCompanyLookupsAsync();

        var paged = await client.GetFromJsonAsync<PagedResult<ProductionOrderSummaryDto>>(
            "/api/production/orders?pageSize=500") ?? new();

        var rows = paged.Items.Select(x => Row(
            ("ProductionOrderId", (object?)x.ProductionOrderId),
            ("CompanyId",         x.CompanyId),
            ("CompanyName",       x.CompanyName),
            ("Folio",             x.Folio),
            ("WeekCode",          x.WeekCode),
            ("Status",            x.Status),
            ("TotalPairs",        x.TotalPairs),
            ("ScheduledDate",     x.ScheduledDate?.ToString("yyyy-MM-dd")),
            ("Notes",             x.Notes),
            ("IsActive",          x.IsActive)
        )).ToList();

        return BuildView("production-orders", "Órdenes de producción",
            "Ciclo de vida completo: borrador → planificado → explotado → reservado → en proceso → completado → cerrado.",
            "ProductionOrderId", [
                TextColumn("ProductionOrderId", "ID", allowEditing: false, width: 220, visible: false),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 200),
                TextColumn("Folio", "Folio", allowEditing: false, width: 110),
                TextColumn("WeekCode", "Semana", required: true, width: 100),
                TextColumn("Status", "Estatus", width: 120),
                NumberColumn("TotalPairs", "Pares", width: 80),
                DateColumn("ScheduledDate", "Fecha programada", width: 150),
                TextColumn("Notes", "Notas", width: 280),
                BoolColumn("IsActive", "Activo", width: 80)
            ], rows);
    }

    private async Task<CatalogViewDefinition> GetProductionSchedulesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await GetCompanyLookupsAsync();

        var paged = await client.GetFromJsonAsync<PagedResult<ProductionScheduleSummaryDto>>(
            "/api/production/schedules?pageSize=200") ?? new();

        var rows = paged.Items.Select(x => Row(
            ("ProductionScheduleId", (object?)x.ProductionScheduleId),
            ("CompanyId",            x.CompanyId),
            ("CompanyName",          x.CompanyName),
            ("WeekCode",             x.WeekCode),
            ("WeekStart",            x.WeekStart?.ToString("yyyy-MM-dd")),
            ("WeekEnd",              x.WeekEnd?.ToString("yyyy-MM-dd")),
            ("Status",               x.Status),
            ("TotalCapacityPairs",   x.TotalCapacityPairs),
            ("LoadPercent",          x.LoadPercent),
            ("IsActive",             x.IsActive)
        )).ToList();

        return BuildView("production-schedules", "Programaciones semanales",
            "Tablero de carga por semana ISO 8601 y celda de producción.",
            "ProductionScheduleId", [
                TextColumn("ProductionScheduleId", "ID", allowEditing: false, width: 220, visible: false),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 200),
                TextColumn("WeekCode", "Semana", required: true, width: 100),
                DateColumn("WeekStart", "Inicio", allowEditing: false, width: 120),
                DateColumn("WeekEnd",   "Fin",    allowEditing: false, width: 120),
                TextColumn("Status", "Estatus", width: 110),
                NumberColumn("TotalCapacityPairs", "Capacidad", width: 100),
                NumberColumn("LoadPercent", "% Carga", width: 90),
                BoolColumn("IsActive", "Activo", width: 80)
            ], rows, allowCreate: false);
    }

    private async Task<CatalogViewDefinition> GetProductionVouchersAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await GetCompanyLookupsAsync();

        var paged = await client.GetFromJsonAsync<PagedResult<ProductionVoucherSummaryDto>>(
            "/api/production/vouchers?pageSize=500") ?? new();

        var rows = paged.Items.Select(x => Row(
            ("ProductionVoucherId", (object?)x.ProductionVoucherId),
            ("CompanyId",           x.CompanyId),
            ("CompanyName",         x.CompanyName),
            ("Folio",               x.Folio),
            ("PhaseCode",           x.PhaseCode),
            ("PhaseName",           x.PhaseName),
            ("EmployeeName",        x.EmployeeName),
            ("PlannedQty",          x.PlannedQty),
            ("ProducedQty",         x.ProducedQty),
            ("RejectedQty",         x.RejectedQty),
            ("Status",              x.Status),
            ("IssuedAt",            x.IssuedAt?.ToString("yyyy-MM-dd")),
            ("Printed",             x.Printed),
            ("IsActive",            x.IsActive)
        )).ToList();

        return BuildView("production-vouchers", "Vales de producción",
            "Emisión, seguimiento y registro de producción por fase y operario.",
            "ProductionVoucherId", [
                TextColumn("ProductionVoucherId", "ID", allowEditing: false, width: 220, visible: false),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 200),
                TextColumn("Folio", "Folio", allowEditing: false, width: 100),
                TextColumn("PhaseCode", "Fase", allowEditing: false, width: 90),
                TextColumn("PhaseName", "Nombre fase", allowEditing: false, width: 160),
                TextColumn("EmployeeName", "Operario", allowEditing: false, width: 180),
                NumberColumn("PlannedQty",  "Planeado",  width: 90),
                NumberColumn("ProducedQty", "Producido", width: 90),
                NumberColumn("RejectedQty", "Rechazado", width: 90),
                TextColumn("Status", "Estatus", allowEditing: false, width: 110),
                DateColumn("IssuedAt", "Fecha emisión", allowEditing: false, width: 130),
                BoolColumn("Printed",  "Impreso", width: 80),
                BoolColumn("IsActive", "Activo",  width: 80)
            ], rows, allowCreate: true);
    }

    private async Task<CatalogViewDefinition> GetPieceWorkRecordsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await GetCompanyLookupsAsync();

        var paged = await client.GetFromJsonAsync<PagedResult<PieceWorkRecordSummaryDto>>(
            "/api/production/piecework/records?pageSize=500") ?? new();

        var rows = paged.Items.Select(x => Row(
            ("PieceWorkRecordId", (object?)x.PieceWorkRecordId),
            ("CompanyId",         x.CompanyId),
            ("CompanyName",       x.CompanyName),
            ("EmployeeName",      x.EmployeeName),
            ("PhaseCode",         x.PhaseCode),
            ("WorkDate",          x.WorkDate?.ToString("yyyy-MM-dd")),
            ("UnitsProduced",     x.UnitsProduced),
            ("UnitsRejected",     x.UnitsRejected),
            ("UnitPrice",         x.UnitPrice),
            ("GrossAmount",       x.GrossAmount),
            ("QualityDeduction",  x.QualityDeduction),
            ("NetAmount",         x.NetAmount),
            ("Status",            x.Status),
            ("IsActive",          x.IsActive)
        )).ToList();

        return BuildView("piecework-records", "Registros de destajo",
            "Captura y aprobación del trabajo a destajo por operario, fase y jornada.",
            "PieceWorkRecordId", [
                TextColumn("PieceWorkRecordId", "ID", allowEditing: false, width: 220, visible: false),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 200),
                TextColumn("EmployeeName", "Operario",     allowEditing: false, width: 180),
                TextColumn("PhaseCode",    "Fase",         allowEditing: false, width: 80),
                DateColumn("WorkDate",     "Fecha trabajo", width: 130),
                NumberColumn("UnitsProduced",    "Producido",  width: 90),
                NumberColumn("UnitsRejected",    "Rechazado",  width: 90),
                NumberColumn("UnitPrice",        "Precio unit", width: 100),
                NumberColumn("GrossAmount",      "Bruto",       width: 100),
                NumberColumn("QualityDeduction", "Deducción",   width: 100),
                NumberColumn("NetAmount",        "Neto",        width: 100),
                TextColumn("Status", "Estatus", allowEditing: false, width: 110),
                BoolColumn("IsActive", "Activo", width: 80)
            ], rows, allowCreate: true);
    }

    private async Task<CatalogViewDefinition> GetPieceWorkRatesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await GetCompanyLookupsAsync();

        var rates = await client.GetFromJsonAsync<List<PieceWorkRateSummaryDto>>(
            "/api/production/piecework/rates") ?? [];

        var rows = rates.Select(x => Row(
            ("PieceWorkRateId",  (object?)x.PieceWorkRateId),
            ("CompanyId",        x.CompanyId),
            ("CompanyName",      x.CompanyName),
            ("PhaseCode",        x.PhaseCode),
            ("PhaseName",        x.PhaseName),
            ("UnitPrice",        x.UnitPrice),
            ("EffectiveFrom",    x.EffectiveFrom?.ToString("yyyy-MM-dd")),
            ("EffectiveTo",      x.EffectiveTo?.ToString("yyyy-MM-dd")),
            ("IsActive",         x.IsActive)
        )).ToList();

        return BuildView("piecework-rates", "Tarifas de destajo",
            "Precio unitario por fase y periodo vigente.",
            "PieceWorkRateId", [
                TextColumn("PieceWorkRateId", "ID", allowEditing: false, width: 220, visible: false),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 200),
                TextColumn("PhaseCode", "Código fase", required: true, width: 110),
                TextColumn("PhaseName", "Fase",         allowEditing: false, width: 160),
                NumberColumn("UnitPrice", "Precio unit", width: 110),
                DateColumn("EffectiveFrom", "Vigente desde", required: true, width: 130),
                DateColumn("EffectiveTo",   "Vigente hasta", width: 130),
                BoolColumn("IsActive", "Activo", width: 80)
            ], rows, allowCreate: true);
    }

    private async Task<CatalogViewDefinition> GetProductionCellsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await GetCompanyLookupsAsync();

        var cells = await client.GetFromJsonAsync<List<ProductionCellSummaryDto>>(
            "/api/production/cells") ?? [];

        var rows = cells.Select(x => Row(
            ("ProductionCellId", (object?)x.ProductionCellId),
            ("CompanyId",        x.CompanyId),
            ("CompanyName",      x.CompanyName),
            ("Code",             x.Code),
            ("Name",             x.Name),
            ("PhaseCode",        x.PhaseCode),
            ("PhaseName",        x.PhaseName),
            ("DailyCapacity",    x.DailyCapacity),
            ("IsActive",         x.IsActive)
        )).ToList();

        return BuildView("production-cells", "Celdas de producción",
            "Unidades productivas (líneas/módulos) por fase con capacidad diaria.",
            "ProductionCellId", [
                TextColumn("ProductionCellId", "ID", allowEditing: false, width: 220, visible: false),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 200),
                TextColumn("Code", "Código", required: true, width: 100),
                TextColumn("Name", "Celda",  required: true, width: 180),
                TextColumn("PhaseCode", "Código fase", allowEditing: false, width: 100),
                TextColumn("PhaseName", "Fase",        allowEditing: false, width: 160),
                NumberColumn("DailyCapacity", "Capacidad/día", width: 120),
                BoolColumn("IsActive", "Activo", width: 80)
            ], rows, allowCreate: true);
    }

    private async Task<CatalogViewDefinition> GetInProcessAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await GetCompanyLookupsAsync();

        var items = await client.GetFromJsonAsync<List<InProcessItemDto>>(
            "/api/production/in-process") ?? [];

        var rows = items.Select(x => Row(
            ("ProductionInProcessId", (object?)x.ProductionInProcessId),
            ("CompanyId",             x.CompanyId),
            ("CompanyName",           x.CompanyName),
            ("OrderFolio",            x.OrderFolio),
            ("PhaseCode",             x.PhaseCode),
            ("PhaseName",             x.PhaseName),
            ("CellName",              x.CellName),
            ("UnitsCurrent",          x.UnitsCurrent),
            ("RecordDate",            x.RecordDate?.ToString("yyyy-MM-dd")),
            ("IsActive",              true)
        )).ToList();

        return BuildView("production-in-process", "En proceso",
            "Instantánea de unidades en proceso por orden, fase y celda.",
            "ProductionInProcessId", [
                TextColumn("ProductionInProcessId", "ID", allowEditing: false, width: 220, visible: false),
                SmartLookupColumn("CompanyId", "Empresa", companies, allowEditing: false, width: 200),
                TextColumn("OrderFolio",  "Orden",    allowEditing: false, width: 110),
                TextColumn("PhaseCode",   "Fase",     allowEditing: false, width: 80),
                TextColumn("PhaseName",   "Nombre",   allowEditing: false, width: 160),
                TextColumn("CellName",    "Celda",    allowEditing: false, width: 160),
                NumberColumn("UnitsCurrent", "Unidades actuales", width: 130),
                DateColumn("RecordDate",  "Fecha",    allowEditing: false, width: 120)
            ], rows, allowCreate: false, allowUpdate: false, allowDelete: false);
    }

    private async Task<CatalogViewDefinition> GetSurplusAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await GetCompanyLookupsAsync();

        var items = await client.GetFromJsonAsync<List<SurplusItemDto>>(
            "/api/production/surplus") ?? [];

        var rows = items.Select(x => Row(
            ("SurplusRecordId", (object?)x.SurplusRecordId),
            ("CompanyId",       x.CompanyId),
            ("CompanyName",     x.CompanyName),
            ("OrderFolio",      x.OrderFolio),
            ("StyleCode",       x.StyleCode),
            ("ColorName",       x.ColorName),
            ("Quantity",        x.Quantity),
            ("Disposition",     x.Disposition),
            ("Notes",           x.Notes),
            ("IsActive",        x.IsActive)
        )).ToList();

        return BuildView("production-surplus", "Sobrantes de producción",
            "Registro y disposición de pares sobrantes o merma de producción.",
            "SurplusRecordId", [
                TextColumn("SurplusRecordId", "ID", allowEditing: false, width: 220, visible: false),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 200),
                TextColumn("OrderFolio",  "Orden",       allowEditing: false, width: 110),
                TextColumn("StyleCode",   "Estilo",      allowEditing: false, width: 100),
                TextColumn("ColorName",   "Color",       allowEditing: false, width: 130),
                NumberColumn("Quantity",  "Cantidad",    width: 90),
                TextColumn("Disposition", "Disposición", width: 160),
                TextColumn("Notes",       "Notas",       width: 280),
                BoolColumn("IsActive", "Activo", width: 80)
            ], rows, allowCreate: true);
    }

    // ── payload mappers ──────────────────────────────────────────────────────

    private static object MapOrderRequest(JsonElement p) => new
    {
        CompanyId    = ReadGuid(p, "CompanyId"),
        WeekCode     = ReadString(p, "WeekCode"),
        ScheduledDate= ReadDate(p, "ScheduledDate"),
        Notes        = ReadString(p, "Notes")
    };

    private static object MapOrderUpdateRequest(JsonElement p) => new
    {
        WeekCode     = ReadString(p, "WeekCode"),
        ScheduledDate= ReadDate(p, "ScheduledDate"),
        Notes        = ReadString(p, "Notes")
    };

    private static object MapVoucherRequest(JsonElement p) => new
    {
        CompanyId           = ReadGuid(p, "CompanyId"),
        ProductionOrderId   = ReadGuid(p, "ProductionOrderId"),
        ProductionOrderLineId = ReadGuid(p, "ProductionOrderLineId"),
        PhaseId             = ReadGuid(p, "PhaseId"),
        EmployeeId          = ReadGuid(p, "EmployeeId"),
        CellId              = ReadGuid(p, "CellId")
    };

    private static object MapPieceWorkRequest(JsonElement p) => new
    {
        CompanyId         = ReadGuid(p, "CompanyId"),
        ProductionOrderId = ReadGuid(p, "ProductionOrderId"),
        PhaseId           = ReadGuid(p, "PhaseId"),
        EmployeeId        = ReadGuid(p, "EmployeeId"),
        WorkDate          = ReadDate(p, "WorkDate"),
        UnitsProduced     = ReadInt(p, "UnitsProduced"),
        UnitsRejected     = ReadInt(p, "UnitsRejected")
    };

    private static object MapRateRequest(JsonElement p) => new
    {
        CompanyId     = ReadGuid(p, "CompanyId"),
        PhaseId       = ReadGuid(p, "PhaseId"),
        UnitPrice     = ReadDecimal(p, "UnitPrice"),
        EffectiveFrom = ReadDate(p, "EffectiveFrom"),
        EffectiveTo   = ReadDate(p, "EffectiveTo")
    };

    private static object MapCellRequest(JsonElement p) => new
    {
        CompanyId     = ReadGuid(p, "CompanyId"),
        PhaseId       = ReadGuid(p, "PhaseId"),
        Code          = ReadString(p, "Code"),
        Name          = ReadString(p, "Name"),
        DailyCapacity = ReadInt(p, "DailyCapacity"),
        IsActive      = ReadBool(p, "IsActive", true)
    };

    private static object MapSurplusRequest(JsonElement p) => new
    {
        CompanyId         = ReadGuid(p, "CompanyId"),
        ProductionOrderId = ReadGuid(p, "ProductionOrderId"),
        Quantity          = ReadInt(p, "Quantity"),
        Notes             = ReadString(p, "Notes")
    };

    // ── lookups ──────────────────────────────────────────────────────────────

    private async Task<List<CatalogLookupItem>> GetCompanyLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var list = await client.GetFromJsonAsync<List<CompanyLookupDto>>("/api/organization/companies") ?? [];
        return list.Select(x => new CatalogLookupItem { Id = x.CompanyId.ToString(), Name = x.Name }).ToList();
    }

    // ── view builder ─────────────────────────────────────────────────────────

    private static CatalogViewDefinition BuildView(
        string catalogKey, string title, string subtitle, string keyExpr,
        List<CatalogColumnDefinition> columns, List<Dictionary<string, object?>> rows,
        bool allowCreate = true, bool allowUpdate = true, bool allowDelete = true)
        => new()
        {
            CatalogKey    = catalogKey,
            Title         = title,
            Subtitle      = subtitle,
            KeyExpr       = keyExpr,
            AllowCreate   = allowCreate,
            AllowUpdate   = allowUpdate,
            AllowDelete   = allowDelete,
            TotalCount    = rows.Count,
            ActiveCount   = rows.Count(x => Convert.ToBoolean(x.GetValueOrDefault("IsActive") ?? true)),
            InactiveCount = rows.Count(x => !Convert.ToBoolean(x.GetValueOrDefault("IsActive") ?? true)),
            Columns       = columns,
            Rows          = rows
        };

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
    {
        var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in values) d[k] = v;
        return d;
    }

    // ── column helpers ────────────────────────────────────────────────────────

    private static CatalogColumnDefinition TextColumn(string field, string caption,
        bool required = false, bool allowEditing = true, int width = 160, bool visible = true)
        => new() { DataField = field, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Visible = visible, Width = width };

    private static CatalogColumnDefinition NumberColumn(string field, string caption,
        bool required = false, int width = 120)
        => new() { DataField = field, Caption = caption, DataType = "number", Required = required, Width = width };

    private static CatalogColumnDefinition BoolColumn(string field, string caption, int width = 90)
        => new() { DataField = field, Caption = caption, DataType = "boolean", Width = width };

    private static CatalogColumnDefinition DateColumn(string field, string caption,
        bool required = false, bool allowEditing = true, int width = 120)
        => new() { DataField = field, Caption = caption, DataType = "date", Required = required, AllowEditing = allowEditing, Width = width };

    private static CatalogColumnDefinition SmartLookupColumn(string field, string caption,
        List<CatalogLookupItem> items, bool required = false, bool allowEditing = true, int width = 180)
        => items.Count <= 1
            ? new() { DataField = field, Caption = caption, DataType = "string", Visible = false, AllowEditing = false, Width = width, UseLookup = true, LookupItems = items }
            : new() { DataField = field, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Width = width, UseLookup = true, LookupItems = items };

    // ── json helpers ──────────────────────────────────────────────────────────

    private static string ReadString(JsonElement p, string name, string fallback = "")
    {
        if (!TryGet(p, name, out var v)) return fallback;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString() ?? fallback,
            JsonValueKind.Null or JsonValueKind.Undefined => fallback,
            _ => v.ToString()
        };
    }

    private static Guid? ReadGuid(JsonElement p, string name)
    {
        if (!TryGet(p, name, out var v)) return null;
        return v.ValueKind == JsonValueKind.String && Guid.TryParse(v.GetString(), out var g) ? g : null;
    }

    private static DateTime? ReadDate(JsonElement p, string name)
    {
        if (!TryGet(p, name, out var v)) return null;
        return v.ValueKind == JsonValueKind.String && DateTime.TryParse(v.GetString(), out var d) ? d : null;
    }

    private static bool ReadBool(JsonElement p, string name, bool fallback = false)
    {
        if (!TryGet(p, name, out var v)) return fallback;
        return v.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => fallback };
    }

    private static int ReadInt(JsonElement p, string name, int fallback = 0)
    {
        if (!TryGet(p, name, out var v)) return fallback;
        return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : fallback;
    }

    private static decimal ReadDecimal(JsonElement p, string name, decimal fallback = 0m)
    {
        if (!TryGet(p, name, out var v)) return fallback;
        return v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var n) ? n : fallback;
    }

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            { value = prop.Value; return true; }
        }
        value = default;
        return false;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"API {(int)response.StatusCode}: {body}");
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public PagedResult() : this([], 0, 1, 20) { }
}

public sealed record ProductionOrderSummaryDto(
    Guid ProductionOrderId, Guid CompanyId, string CompanyName,
    string Folio, string WeekCode, string Status,
    int TotalPairs, DateTime? ScheduledDate, string Notes, bool IsActive);

public sealed record ProductionScheduleSummaryDto(
    Guid ProductionScheduleId, Guid CompanyId, string CompanyName,
    string WeekCode, DateTime? WeekStart, DateTime? WeekEnd,
    string Status, int TotalCapacityPairs, decimal LoadPercent, bool IsActive);

public sealed record ProductionVoucherSummaryDto(
    Guid ProductionVoucherId, Guid CompanyId, string CompanyName,
    string Folio, string PhaseCode, string PhaseName,
    string EmployeeName, int PlannedQty, int ProducedQty, int RejectedQty,
    string Status, DateTime? IssuedAt, bool Printed, bool IsActive);

public sealed record PieceWorkRecordSummaryDto(
    Guid PieceWorkRecordId, Guid CompanyId, string CompanyName,
    string EmployeeName, string PhaseCode, DateTime? WorkDate,
    int UnitsProduced, int UnitsRejected, decimal UnitPrice,
    decimal GrossAmount, decimal QualityDeduction, decimal NetAmount,
    string Status, bool IsActive);

public sealed record PieceWorkRateSummaryDto(
    Guid PieceWorkRateId, Guid CompanyId, string CompanyName,
    string PhaseCode, string PhaseName, decimal UnitPrice,
    DateTime? EffectiveFrom, DateTime? EffectiveTo, bool IsActive);

public sealed record ProductionCellSummaryDto(
    Guid ProductionCellId, Guid CompanyId, string CompanyName,
    string Code, string Name, string PhaseCode, string PhaseName,
    int DailyCapacity, bool IsActive);

public sealed record InProcessItemDto(
    Guid ProductionInProcessId, Guid CompanyId, string CompanyName,
    string OrderFolio, string PhaseCode, string PhaseName, string CellName,
    int UnitsCurrent, DateTime? RecordDate);

public sealed record SurplusItemDto(
    Guid SurplusRecordId, Guid CompanyId, string CompanyName,
    string OrderFolio, string StyleCode, string ColorName,
    int Quantity, string Disposition, string Notes, bool IsActive);

public sealed record CompanyLookupDto(Guid CompanyId, string Name);

// dashboard

public sealed record ProductionKpiDto(
    Guid CompanyId, string WeekCode,
    KpiOrdersDto Orders, KpiProductionDto Production, KpiAlertsDto Alerts,
    List<InProcessByPhaseDto> InProcessByPhase, List<ScheduleLoadDto> ScheduleLoad);

public sealed record KpiOrdersDto(int Total, int InProgress, int Completed, int Planned);
public sealed record KpiProductionDto(int ProducedThisWeek, int VouchersIssuedToday, decimal PieceWorkNetThisWeek);
public sealed record KpiAlertsDto(int MaterialShortages);
public sealed record InProcessByPhaseDto(string PhaseCode, string PhaseName, int Units);
public sealed record ScheduleLoadDto(string WeekCode, decimal LoadPercent, int TotalCapacity);

public sealed record OrdersBoardDto(
    List<OrderBoardCardDto> Draft, List<OrderBoardCardDto> Planned,
    List<OrderBoardCardDto> InProgress, List<OrderBoardCardDto> Completed,
    List<OrderBoardCardDto> Cancelled);

public sealed record OrderBoardCardDto(
    Guid ProductionOrderId, string Folio, string WeekCode,
    int TotalPairs, bool IsOverdue, DateTime? ScheduledDate);

public sealed record PhaseThroughputDto(
    string PhaseCode, string PhaseName, int TotalProduced, int TotalRejected,
    List<DailyBreakdownDto> DailyBreakdown);

public sealed record DailyBreakdownDto(string Date, int Produced, int Rejected);
