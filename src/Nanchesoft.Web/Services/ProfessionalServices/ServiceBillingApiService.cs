using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.ProfessionalServices;

public sealed class ServiceBillingApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;
    private Guid? _resolvedTenantId;
    private Guid? _resolvedCompanyId;

    public ServiceBillingApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey = "service-notes")
        => NormalizeCatalog(catalogKey) switch
        {
            "service-catalog" => GetServiceCatalogDefinitionAsync(),
            "customer-service-rates" => GetCustomerRatesDefinitionAsync(),
            _ => GetServiceNotesDefinitionAsync()
        };

    public Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
        => ExecuteWriteAsync(NormalizeCatalog(catalogKey), null, payload, isInsert: true);

    public Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
        => ExecuteWriteAsync(NormalizeCatalog(catalogKey), key, payload, isInsert: false);

    public async Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        var normalized = NormalizeCatalog(catalogKey);
        var endpoint = GetEndpoint(normalized);
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.DeleteAsync($"{endpoint}/{key}");
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(normalized);
    }

    private async Task<CatalogViewDefinition> GetServiceNotesDefinitionAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var notesTask = client.GetFromJsonAsync<List<ServiceNoteRowDto>>("/api/services/service-notes");
        var lookupsTask = client.GetFromJsonAsync<ServiceModuleLookupBundleDto>("/api/services/service-notes/lookups");
        await Task.WhenAll(notesTask!, lookupsTask!);

        var notes = notesTask.Result ?? new List<ServiceNoteRowDto>();
        var context = ResolveContext(lookupsTask.Result ?? new ServiceModuleLookupBundleDto());

        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "ServiceNoteId", Caption = "Nota ID", DataType = "string", AllowEditing = false, Width = 220, Visible = false },
            TenantColumn(context.Lookups, context.HideTenantField),
            CompanyColumn(context.Lookups, context.HideCompanyField),
            CustomerColumn(context.Lookups),
            ServiceColumn(context.Lookups),
            new() { DataField = "Folio", Caption = "Folio", DataType = "string", Width = 110 },
            new() { DataField = "NoteDate", Caption = "Fecha", DataType = "date", Required = true, Width = 120 },
            new() { DataField = "Description", Caption = "Descripción", DataType = "string", Width = 320 },
            new() { DataField = "StartTimeText", Caption = "Hora inicio", DataType = "string", Width = 110 },
            new() { DataField = "EndTimeText", Caption = "Hora fin", DataType = "string", Width = 110 },
            new() { DataField = "BreakMinutes", Caption = "Min. comida", DataType = "number", Width = 110 },
            new() { DataField = "HoursWorked", Caption = "Horas", DataType = "number", Width = 95 },
            new() { DataField = "HourlyRate", Caption = "Tarifa hora", DataType = "number", Width = 120 },
            new() { DataField = "Subtotal", Caption = "Subtotal", DataType = "number", AllowEditing = false, Width = 120 },
            new() { DataField = "Total", Caption = "Total", DataType = "number", AllowEditing = false, Width = 120 },
            PaymentStatusColumn(),
            PaymentMethodColumn(),
            new() { DataField = "PaymentDate", Caption = "Fecha cobro", DataType = "date", Width = 120 },
            PaymentDestinationColumn(context.Lookups),
            new() { DataField = "PaymentReference", Caption = "Referencia", DataType = "string", Width = 160 },
            new() { DataField = "Notes", Caption = "Notas", DataType = "string", Width = 220 },
            new() { DataField = "IsActive", Caption = "Activo", DataType = "boolean", Width = 90 }
        };

        var rows = notes
            .Where(x => MatchesContext(x.TenantId, x.CompanyId, context))
            .Select(x => new Dictionary<string, object?>
            {
                ["ServiceNoteId"] = x.ServiceNoteId.ToString("D"),
                ["TenantId"] = x.TenantId.ToString("D"),
                ["CompanyId"] = x.CompanyId.ToString("D"),
                ["CustomerId"] = x.CustomerId?.ToString("D"),
                ["ServiceCatalogItemId"] = x.ServiceCatalogItemId?.ToString("D"),
                ["Folio"] = x.Folio,
                ["NoteDate"] = x.NoteDate,
                ["Description"] = x.Description,
                ["StartTimeText"] = x.StartTimeText,
                ["EndTimeText"] = x.EndTimeText,
                ["BreakMinutes"] = x.BreakMinutes,
                ["HoursWorked"] = x.HoursWorked,
                ["HourlyRate"] = x.HourlyRate,
                ["Subtotal"] = x.Subtotal,
                ["Total"] = x.Total,
                ["PaymentStatus"] = x.PaymentStatus,
                ["PaymentMethod"] = x.PaymentMethod,
                ["PaymentDate"] = x.PaymentDate,
                ["PaymentDestination"] = x.PaymentDestination,
                ["PaymentReference"] = x.PaymentReference,
                ["Notes"] = x.Notes,
                ["IsActive"] = x.IsActive
            }).ToList();

        return BuildView(
            "service-notes",
            "Notas de servicio",
            "Control de horas, servicio, tarifa por cliente y cobro en efectivo, depósito o transferencia.",
            "ServiceNoteId",
            columns,
            rows);
    }

    private async Task<CatalogViewDefinition> GetServiceCatalogDefinitionAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rowsTask = client.GetFromJsonAsync<List<ServiceCatalogRowDto>>("/api/services/catalog");
        var lookupsTask = client.GetFromJsonAsync<ServiceModuleLookupBundleDto>("/api/services/catalog/lookups");
        await Task.WhenAll(rowsTask!, lookupsTask!);

        var rowsDto = rowsTask.Result ?? new List<ServiceCatalogRowDto>();
        var context = ResolveContext(lookupsTask.Result ?? new ServiceModuleLookupBundleDto());

        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "ServiceCatalogItemId", Caption = "Servicio ID", DataType = "string", AllowEditing = false, Width = 220, Visible = false },
            TenantColumn(context.Lookups, context.HideTenantField),
            CompanyColumn(context.Lookups, context.HideCompanyField),
            new() { DataField = "Code", Caption = "Código", DataType = "string", Required = true, Width = 120 },
            new() { DataField = "Name", Caption = "Servicio", DataType = "string", Required = true, Width = 240 },
            new() { DataField = "Description", Caption = "Descripción", DataType = "string", Width = 300 },
            BillingUnitColumn(),
            new() { DataField = "DefaultRate", Caption = "Tarifa base", DataType = "number", Width = 120 },
            new() { DataField = "Notes", Caption = "Notas", DataType = "string", Width = 220 },
            new() { DataField = "IsActive", Caption = "Activo", DataType = "boolean", Width = 90 }
        };

        var rows = rowsDto
            .Where(x => MatchesContext(x.TenantId, x.CompanyId, context))
            .Select(x => new Dictionary<string, object?>
            {
                ["ServiceCatalogItemId"] = x.ServiceCatalogItemId.ToString("D"),
                ["TenantId"] = x.TenantId.ToString("D"),
                ["CompanyId"] = x.CompanyId.ToString("D"),
                ["Code"] = x.Code,
                ["Name"] = x.Name,
                ["Description"] = x.Description,
                ["BillingUnit"] = x.BillingUnit,
                ["DefaultRate"] = x.DefaultRate,
                ["Notes"] = x.Notes,
                ["IsActive"] = x.IsActive
            }).ToList();

        return BuildView(
            "service-catalog",
            "Catálogo de servicios",
            "Servicios facturables para empresas que venden desarrollo, soporte, consultoría o implementación por hora.",
            "ServiceCatalogItemId",
            columns,
            rows);
    }

    private async Task<CatalogViewDefinition> GetCustomerRatesDefinitionAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rowsTask = client.GetFromJsonAsync<List<CustomerServiceRateRowDto>>("/api/services/customer-rates");
        var lookupsTask = client.GetFromJsonAsync<ServiceModuleLookupBundleDto>("/api/services/customer-rates/lookups");
        await Task.WhenAll(rowsTask!, lookupsTask!);

        var rowsDto = rowsTask.Result ?? new List<CustomerServiceRateRowDto>();
        var context = ResolveContext(lookupsTask.Result ?? new ServiceModuleLookupBundleDto());

        var columns = new List<CatalogColumnDefinition>
        {
            new() { DataField = "CustomerServiceRateId", Caption = "Tarifa ID", DataType = "string", AllowEditing = false, Width = 220, Visible = false },
            TenantColumn(context.Lookups, context.HideTenantField),
            CompanyColumn(context.Lookups, context.HideCompanyField),
            CustomerColumn(context.Lookups),
            ServiceColumn(context.Lookups),
            CurrencyColumn(context.Lookups),
            new() { DataField = "Rate", Caption = "Tarifa por hora", DataType = "number", Required = true, Width = 130 },
            new() { DataField = "EffectiveFrom", Caption = "Vigencia inicial", DataType = "date", Required = true, Width = 130 },
            new() { DataField = "EffectiveTo", Caption = "Vigencia final", DataType = "date", Width = 130 },
            new() { DataField = "Notes", Caption = "Notas", DataType = "string", Width = 220 },
            new() { DataField = "IsActive", Caption = "Activo", DataType = "boolean", Width = 90 }
        };

        var rows = rowsDto
            .Where(x => MatchesContext(x.TenantId, x.CompanyId, context))
            .Select(x => new Dictionary<string, object?>
            {
                ["CustomerServiceRateId"] = x.CustomerServiceRateId.ToString("D"),
                ["TenantId"] = x.TenantId.ToString("D"),
                ["CompanyId"] = x.CompanyId.ToString("D"),
                ["CustomerId"] = x.CustomerId.ToString("D"),
                ["ServiceCatalogItemId"] = x.ServiceCatalogItemId.ToString("D"),
                ["CurrencyId"] = x.CurrencyId?.ToString("D"),
                ["Rate"] = x.Rate,
                ["EffectiveFrom"] = x.EffectiveFrom,
                ["EffectiveTo"] = x.EffectiveTo,
                ["Notes"] = x.Notes,
                ["IsActive"] = x.IsActive
            }).ToList();

        return BuildView(
            "customer-service-rates",
            "Tarifas por cliente",
            "Vigencias y tarifas por hora negociadas para cada cliente y servicio.",
            "CustomerServiceRateId",
            columns,
            rows);
    }

    private async Task<CatalogViewDefinition> ExecuteWriteAsync(string catalogKey, string? key, JsonElement payload, bool isInsert)
    {
        var endpoint = GetEndpoint(catalogKey);
        object request = catalogKey switch
        {
            "service-catalog" => MapServiceCatalogRequest(payload),
            "customer-service-rates" => MapCustomerServiceRateRequest(payload),
            _ => MapServiceNoteRequest(payload)
        };

        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = isInsert
            ? await client.PostAsJsonAsync(endpoint, request)
            : await client.PutAsJsonAsync($"{endpoint}/{key}", request);

        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    private static string GetEndpoint(string catalogKey)
        => catalogKey switch
        {
            "service-catalog" => "/api/services/catalog",
            "customer-service-rates" => "/api/services/customer-rates",
            _ => "/api/services/service-notes"
        };

    private static string NormalizeCatalog(string? catalogKey)
        => string.IsNullOrWhiteSpace(catalogKey) ? "service-notes" : catalogKey.Trim().ToLowerInvariant();

    private ContextSettings ResolveContext(ServiceModuleLookupBundleDto sourceLookups)
    {
        var tenantId = _authState.TenantId ?? _appState.CurrentTenantId ?? _resolvedTenantId;
        var companyId = _authState.CompanyId ?? _appState.CurrentCompanyId ?? _resolvedCompanyId;

        var tenantName = FirstNonEmpty(_authState.TenantName, _appState.CurrentTenantName);
        var companyName = FirstNonEmpty(_authState.CompanyName, _appState.CurrentCompanyName);

        if (!tenantId.HasValue && !string.IsNullOrWhiteSpace(tenantName))
        {
            tenantId = sourceLookups.Tenants
                .Where(x => string.Equals(x.TenantName, tenantName, StringComparison.OrdinalIgnoreCase))
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefault();
        }

        var companiesByTenant = tenantId.HasValue
            ? sourceLookups.Companies.Where(x => x.TenantId == tenantId.Value).ToList()
            : sourceLookups.Companies.ToList();

        if (!companyId.HasValue && !string.IsNullOrWhiteSpace(companyName))
        {
            companyId = companiesByTenant
                .Where(x => string.Equals(x.CompanyName, companyName, StringComparison.OrdinalIgnoreCase))
                .Select(x => (Guid?)x.CompanyId)
                .FirstOrDefault();
        }

        if (!tenantId.HasValue && sourceLookups.Tenants.Count == 1)
        {
            tenantId = sourceLookups.Tenants[0].TenantId;
        }

        if (!companyId.HasValue && companiesByTenant.Count == 1)
        {
            companyId = companiesByTenant[0].CompanyId;
        }

        if (companyId.HasValue && !tenantId.HasValue)
        {
            tenantId = sourceLookups.Companies
                .Where(x => x.CompanyId == companyId.Value)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefault();
        }

        if (tenantId.HasValue && !companyId.HasValue)
        {
            var soleCompany = sourceLookups.Companies.Where(x => x.TenantId == tenantId.Value).ToList();
            if (soleCompany.Count == 1)
            {
                companyId = soleCompany[0].CompanyId;
            }
        }

        var filteredLookups = new ServiceModuleLookupBundleDto
        {
            Tenants = tenantId.HasValue
                ? sourceLookups.Tenants.Where(x => x.TenantId == tenantId.Value).ToList()
                : sourceLookups.Tenants.ToList(),
            Companies = sourceLookups.Companies
                .Where(x => !tenantId.HasValue || x.TenantId == tenantId.Value)
                .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
                .ToList(),
            Customers = sourceLookups.Customers
                .Where(x => !tenantId.HasValue || x.TenantId == tenantId.Value)
                .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
                .ToList(),
            Services = sourceLookups.Services
                .Where(x => !tenantId.HasValue || x.TenantId == tenantId.Value)
                .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
                .ToList(),
            Currencies = sourceLookups.Currencies.ToList(),
            PaymentDestinations = sourceLookups.PaymentDestinations.ToList()
        };

        _resolvedTenantId = tenantId;
        _resolvedCompanyId = companyId;

        return new ContextSettings(
            tenantId,
            companyId,
            HideTenantField: tenantId.HasValue || filteredLookups.Tenants.Count <= 1,
            HideCompanyField: companyId.HasValue || filteredLookups.Companies.Count <= 1,
            filteredLookups);
    }

    private bool MatchesContext(Guid tenantId, Guid companyId, ContextSettings context)
    {
        if (context.TenantId.HasValue && tenantId != context.TenantId.Value)
        {
            return false;
        }

        if (context.CompanyId.HasValue && companyId != context.CompanyId.Value)
        {
            return false;
        }

        return true;
    }

    private Guid? ResolveTenantId() => _authState.TenantId ?? _appState.CurrentTenantId ?? _resolvedTenantId;

    private Guid? ResolveCompanyId() => _authState.CompanyId ?? _appState.CurrentCompanyId ?? _resolvedCompanyId;

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

    private static CatalogViewDefinition BuildView(string catalogKey, string title, string subtitle, string keyExpr, List<CatalogColumnDefinition> columns, List<Dictionary<string, object?>> rows)
    {
        return new CatalogViewDefinition
        {
            CatalogKey = catalogKey,
            Title = title,
            Subtitle = subtitle,
            KeyExpr = keyExpr,
            AllowCreate = true,
            AllowUpdate = true,
            AllowDelete = true,
            Columns = columns,
            Rows = rows,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => x.TryGetValue("IsActive", out var value) && value is bool active && active),
            InactiveCount = rows.Count(x => x.TryGetValue("IsActive", out var value) && value is bool active && !active)
        };
    }

    private static CatalogColumnDefinition TenantColumn(ServiceModuleLookupBundleDto lookups, bool hideField) => new()
    {
        DataField = "TenantId",
        Caption = "Tenant",
        DataType = "string",
        Required = !hideField,
        AllowEditing = !hideField,
        Visible = !hideField,
        Width = 180,
        UseLookup = true,
        LookupItems = lookups.Tenants.Select(x => new CatalogLookupItem { Id = x.TenantId.ToString("D"), Name = x.TenantName }).ToList()
    };

    private static CatalogColumnDefinition CompanyColumn(ServiceModuleLookupBundleDto lookups, bool hideField) => new()
    {
        DataField = "CompanyId",
        Caption = "Empresa",
        DataType = "string",
        Required = !hideField,
        AllowEditing = !hideField,
        Visible = !hideField,
        Width = 220,
        UseLookup = true,
        LookupItems = lookups.Companies.Select(x => new CatalogLookupItem { Id = x.CompanyId.ToString("D"), Name = $"{x.CompanyName} · {x.TenantName}" }).ToList()
    };

    private static CatalogColumnDefinition CustomerColumn(ServiceModuleLookupBundleDto lookups) => new()
    {
        DataField = "CustomerId",
        Caption = "Cliente",
        DataType = "string",
        Width = 240,
        UseLookup = true,
        LookupItems = lookups.Customers.Select(x => new CatalogLookupItem { Id = x.CustomerId.ToString("D"), Name = $"{x.CustomerName} · {x.CompanyName}" }).ToList()
    };

    private static CatalogColumnDefinition ServiceColumn(ServiceModuleLookupBundleDto lookups) => new()
    {
        DataField = "ServiceCatalogItemId",
        Caption = "Servicio",
        DataType = "string",
        Width = 260,
        UseLookup = true,
        LookupItems = lookups.Services.Select(x => new CatalogLookupItem { Id = x.ServiceCatalogItemId.ToString("D"), Name = $"{x.ServiceCode} · {x.ServiceName} · {x.CompanyName}" }).ToList()
    };

    private static CatalogColumnDefinition CurrencyColumn(ServiceModuleLookupBundleDto lookups) => new()
    {
        DataField = "CurrencyId",
        Caption = "Moneda",
        DataType = "string",
        Width = 120,
        UseLookup = true,
        LookupItems = lookups.Currencies.Select(x => new CatalogLookupItem { Id = x.CurrencyId.ToString("D"), Name = $"{x.CurrencyCode} · {x.CurrencyName}" }).ToList()
    };

    private static CatalogColumnDefinition BillingUnitColumn() => new()
    {
        DataField = "BillingUnit",
        Caption = "Unidad cobro",
        DataType = "string",
        Width = 130,
        UseLookup = true,
        LookupItems =
        [
            new CatalogLookupItem { Id = "HORA", Name = "Hora" },
            new CatalogLookupItem { Id = "DIA", Name = "Día" },
            new CatalogLookupItem { Id = "EVENTO", Name = "Evento" },
            new CatalogLookupItem { Id = "MENSUALIDAD", Name = "Mensualidad" }
        ]
    };

    private static CatalogColumnDefinition PaymentStatusColumn() => new()
    {
        DataField = "PaymentStatus",
        Caption = "Estatus cobro",
        DataType = "string",
        Width = 140,
        UseLookup = true,
        LookupItems =
        [
            new CatalogLookupItem { Id = "PENDIENTE", Name = "Pendiente" },
            new CatalogLookupItem { Id = "PAGADA", Name = "Pagada" },
            new CatalogLookupItem { Id = "CANCELADA", Name = "Cancelada" }
        ]
    };

    private static CatalogColumnDefinition PaymentMethodColumn() => new()
    {
        DataField = "PaymentMethod",
        Caption = "Forma de pago",
        DataType = "string",
        Width = 150,
        UseLookup = true,
        LookupItems =
        [
            new CatalogLookupItem { Id = "POR_DEFINIR", Name = "Por definir" },
            new CatalogLookupItem { Id = "EFECTIVO", Name = "Efectivo" },
            new CatalogLookupItem { Id = "DEPOSITO", Name = "Depósito" },
            new CatalogLookupItem { Id = "TRANSFERENCIA", Name = "Transferencia" }
        ]
    };

    private static CatalogColumnDefinition PaymentDestinationColumn(ServiceModuleLookupBundleDto lookups) => new()
    {
        DataField = "PaymentDestination",
        Caption = "Caja / cuenta destino",
        DataType = "string",
        Width = 240,
        UseLookup = true,
        LookupItems = lookups.PaymentDestinations.Select(x => new CatalogLookupItem { Id = x.DestinationValue, Name = x.DestinationLabel }).ToList()
    };

    private CreateOrUpdateServiceCatalogItemRequest MapServiceCatalogRequest(JsonElement payload)
    {
        return new CreateOrUpdateServiceCatalogItemRequest
        {
            TenantId = ReadGuid(payload, "TenantId") ?? ResolveTenantId(),
            CompanyId = ReadGuid(payload, "CompanyId") ?? ResolveCompanyId(),
            Code = ReadNullableString(payload, "Code"),
            Name = ReadNullableString(payload, "Name"),
            Description = ReadNullableString(payload, "Description"),
            BillingUnit = ReadNullableString(payload, "BillingUnit"),
            DefaultRate = ReadDecimal(payload, "DefaultRate"),
            Notes = ReadNullableString(payload, "Notes"),
            IsActive = ReadBool(payload, "IsActive", true)
        };
    }

    private CreateOrUpdateCustomerServiceRateRequest MapCustomerServiceRateRequest(JsonElement payload)
    {
        return new CreateOrUpdateCustomerServiceRateRequest
        {
            TenantId = ReadGuid(payload, "TenantId") ?? ResolveTenantId(),
            CompanyId = ReadGuid(payload, "CompanyId") ?? ResolveCompanyId(),
            CustomerId = ReadGuid(payload, "CustomerId"),
            ServiceCatalogItemId = ReadGuid(payload, "ServiceCatalogItemId"),
            CurrencyId = ReadGuid(payload, "CurrencyId"),
            Rate = ReadDecimal(payload, "Rate"),
            EffectiveFrom = ReadDate(payload, "EffectiveFrom"),
            EffectiveTo = ReadDate(payload, "EffectiveTo"),
            Notes = ReadNullableString(payload, "Notes"),
            IsActive = ReadBool(payload, "IsActive", true)
        };
    }

    private CreateOrUpdateServiceNoteRequest MapServiceNoteRequest(JsonElement payload)
    {
        return new CreateOrUpdateServiceNoteRequest
        {
            TenantId = ReadGuid(payload, "TenantId") ?? ResolveTenantId(),
            CompanyId = ReadGuid(payload, "CompanyId") ?? ResolveCompanyId(),
            CustomerId = ReadGuid(payload, "CustomerId"),
            ServiceCatalogItemId = ReadGuid(payload, "ServiceCatalogItemId"),
            Folio = ReadNullableString(payload, "Folio"),
            NoteDate = ReadDate(payload, "NoteDate"),
            Description = ReadNullableString(payload, "Description"),
            StartTimeText = ReadNullableString(payload, "StartTimeText"),
            EndTimeText = ReadNullableString(payload, "EndTimeText"),
            BreakMinutes = ReadInt(payload, "BreakMinutes"),
            HoursWorked = ReadDecimal(payload, "HoursWorked"),
            HourlyRate = ReadDecimal(payload, "HourlyRate"),
            PaymentStatus = ReadNullableString(payload, "PaymentStatus"),
            PaymentMethod = ReadNullableString(payload, "PaymentMethod"),
            PaymentDate = ReadDate(payload, "PaymentDate"),
            PaymentDestination = ReadNullableString(payload, "PaymentDestination"),
            PaymentReference = ReadNullableString(payload, "PaymentReference"),
            Notes = ReadNullableString(payload, "Notes"),
            IsActive = ReadBool(payload, "IsActive", true)
        };
    }

    private static string ReadString(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? ReadNullableString(JsonElement payload, string name)
    {
        var value = ReadString(payload, name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static Guid? ReadGuid(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTime? ReadDate(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value))
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                var raw = value.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var dto))
                {
                    return NormalizeBusinessDateUtc(dto.UtcDateTime);
                }

                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
                {
                    return NormalizeBusinessDateUtc(parsed);
                }
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var epoch))
            {
                return NormalizeBusinessDateUtc(DateTimeOffset.FromUnixTimeMilliseconds(epoch).UtcDateTime);
            }
        }

        return null;
    }

    private static DateTime NormalizeBusinessDateUtc(DateTime value)
    {
        var normalized = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTime(normalized.Year, normalized.Month, normalized.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static decimal ReadDecimal(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }

            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0m;
    }

    private static int ReadInt(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static bool ReadBool(JsonElement payload, string name, bool fallback = true)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static bool TryGetPropertyInsensitive(JsonElement payload, string name, out JsonElement value)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (payload.TryGetProperty(name, out value))
        {
            return true;
        }

        var camel = char.ToLowerInvariant(name[0]) + name[1..];
        if (payload.TryGetProperty(camel, out value))
        {
            return true;
        }

        foreach (var property in payload.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("La API devolvió un error sin detalle.");
        }

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

public readonly record struct ContextSettings(Guid? TenantId, Guid? CompanyId, bool HideTenantField, bool HideCompanyField, ServiceModuleLookupBundleDto Lookups);

public sealed class ServiceModuleLookupBundleDto
{
    public List<ServiceTenantLookupDto> Tenants { get; set; } = new();
    public List<ServiceCompanyLookupDto> Companies { get; set; } = new();
    public List<ServiceCustomerLookupDto> Customers { get; set; } = new();
    public List<ServiceCatalogLookupDto> Services { get; set; } = new();
    public List<ServiceCurrencyLookupDto> Currencies { get; set; } = new();
    public List<ServicePaymentDestinationLookupDto> PaymentDestinations { get; set; } = new();
}

public sealed class ServiceTenantLookupDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class ServiceCompanyLookupDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class ServiceCustomerLookupDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}

public sealed class ServiceCatalogLookupDto
{
    public Guid ServiceCatalogItemId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string BillingUnit { get; set; } = string.Empty;
    public decimal DefaultRate { get; set; }
}

public sealed class ServiceCurrencyLookupDto
{
    public Guid CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
}

public sealed class ServicePaymentDestinationLookupDto
{
    public string DestinationValue { get; set; } = string.Empty;
    public string DestinationLabel { get; set; } = string.Empty;
}

public sealed class ServiceNoteRowDto
{
    public Guid ServiceNoteId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? ServiceCatalogItemId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime NoteDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string StartTimeText { get; set; } = string.Empty;
    public string EndTimeText { get; set; } = string.Empty;
    public int BreakMinutes { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string PaymentDestination { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class ServiceCatalogRowDto
{
    public Guid ServiceCatalogItemId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BillingUnit { get; set; } = string.Empty;
    public decimal DefaultRate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CustomerServiceRateRowDto
{
    public Guid CustomerServiceRateId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ServiceCatalogItemId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CreateOrUpdateServiceCatalogItemRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? BillingUnit { get; set; }
    public decimal DefaultRate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreateOrUpdateCustomerServiceRateRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public Guid? CurrencyId { get; set; }
    public decimal Rate { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreateOrUpdateServiceNoteRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public string? Folio { get; set; }
    public DateTime? NoteDate { get; set; }
    public string? Description { get; set; }
    public string? StartTimeText { get; set; }
    public string? EndTimeText { get; set; }
    public int BreakMinutes { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentDestination { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
