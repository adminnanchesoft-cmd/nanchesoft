using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Companies;

namespace Nanchesoft.Web.Services.Catalogs;

public sealed class MasterCatalogApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MasterCatalogApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
    {
        return catalogKey.ToLowerInvariant() switch
        {
            "currencies" => GetCurrenciesAsync(),
            "exchange-rates" => GetExchangeRatesAsync(),
            "taxes" => GetTaxesAsync(),
            "units" => GetUnitsAsync(),
            "banks" => GetBanksAsync(),
            "countries" => GetCountriesAsync(),
            "states" => GetStatesAsync(),
            "cities" => GetCitiesAsync(),
            "document-series" => GetDocumentSeriesAsync(),
            "document-folios" => GetDocumentFoliosAsync(),
            "company-settings" => GetCompanySettingsAsync(),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };
    }

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        HttpResponseMessage response = catalogKey.ToLowerInvariant() switch
        {
            "currencies" => await client.PostAsJsonAsync("/api/catalogs/currencies", MapCurrencyRequest(payload)),
            "exchange-rates" => await client.PostAsJsonAsync("/api/catalogs/exchange-rates", MapExchangeRateRequest(payload)),
            "taxes" => await client.PostAsJsonAsync("/api/catalogs/taxes", MapTaxRequest(payload)),
            "units" => await client.PostAsJsonAsync("/api/catalogs/units", MapUnitRequest(payload)),
            "banks" => await client.PostAsJsonAsync("/api/catalogs/banks", MapBankRequest(payload)),
            "countries" => await client.PostAsJsonAsync("/api/catalogs/countries", MapCountryRequest(payload)),
            "states" => await client.PostAsJsonAsync("/api/catalogs/states", MapStateRequest(payload)),
            "cities" => await client.PostAsJsonAsync("/api/catalogs/cities", MapCityRequest(payload)),
            "document-series" => await client.PostAsJsonAsync("/api/administration/document-series", MapDocumentSeriesRequest(payload)),
            "document-folios" => await client.PostAsJsonAsync("/api/administration/document-folios", MapDocumentFolioRequest(payload)),
            "company-settings" => await client.PostAsJsonAsync("/api/administration/company-settings", MapCompanySettingRequest(payload)),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        HttpResponseMessage response = catalogKey.ToLowerInvariant() switch
        {
            "currencies" => await client.PutAsJsonAsync($"/api/catalogs/currencies/{key}", MapCurrencyRequest(payload)),
            "exchange-rates" => await client.PutAsJsonAsync($"/api/catalogs/exchange-rates/{key}", MapExchangeRateRequest(payload)),
            "taxes" => await client.PutAsJsonAsync($"/api/catalogs/taxes/{key}", MapTaxRequest(payload)),
            "units" => await client.PutAsJsonAsync($"/api/catalogs/units/{key}", MapUnitRequest(payload)),
            "banks" => await client.PutAsJsonAsync($"/api/catalogs/banks/{key}", MapBankRequest(payload)),
            "countries" => await client.PutAsJsonAsync($"/api/catalogs/countries/{key}", MapCountryRequest(payload)),
            "states" => await client.PutAsJsonAsync($"/api/catalogs/states/{key}", MapStateRequest(payload)),
            "cities" => await client.PutAsJsonAsync($"/api/catalogs/cities/{key}", MapCityRequest(payload)),
            "document-series" => await client.PutAsJsonAsync($"/api/administration/document-series/{key}", MapDocumentSeriesRequest(payload)),
            "document-folios" => await client.PutAsJsonAsync($"/api/administration/document-folios/{key}", MapDocumentFolioRequest(payload)),
            "company-settings" => await client.PutAsJsonAsync($"/api/administration/company-settings/{key}", MapCompanySettingRequest(payload)),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        var endpoint = catalogKey.ToLowerInvariant() switch
        {
            "currencies" => $"/api/catalogs/currencies/{key}",
            "exchange-rates" => $"/api/catalogs/exchange-rates/{key}",
            "taxes" => $"/api/catalogs/taxes/{key}",
            "units" => $"/api/catalogs/units/{key}",
            "banks" => $"/api/catalogs/banks/{key}",
            "countries" => $"/api/catalogs/countries/{key}",
            "states" => $"/api/catalogs/states/{key}",
            "cities" => $"/api/catalogs/cities/{key}",
            "document-series" => $"/api/administration/document-series/{key}",
            "document-folios" => $"/api/administration/document-folios/{key}",
            "company-settings" => $"/api/administration/company-settings/{key}",
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        var response = await client.DeleteAsync(endpoint);
        await EnsureSuccessAsync(response);

        return await GetCatalogAsync(catalogKey);
    }

    private async Task<CatalogViewDefinition> GetCurrenciesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CurrencyDto>>("/api/catalogs/currencies") ?? [];

        return BuildView(
            "currencies",
            "Monedas",
            "Catálogo general de monedas y moneda base del tenant demo.",
            "CurrencyId",
            [
                TextColumn("CurrencyId", "Currency ID", allowEditing: false, width: 220),
                TextColumn("TenantId", "Tenant", allowEditing: false, visible: false),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Moneda", required: true, width: 220),
                TextColumn("Symbol", "Símbolo", required: true, width: 100),
                BoolColumn("IsDefault", "Default", width: 100),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("CurrencyId", x.CurrencyId.ToString("D")),
                ("TenantId", x.TenantId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Symbol", x.Symbol),
                ("IsDefault", x.IsDefault),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetExchangeRatesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ExchangeRateDto>>("/api/catalogs/exchange-rates") ?? [];
        var currencies = await GetCurrencyLookupsAsync();

        return BuildView(
            "exchange-rates",
            "Tipos de cambio",
            "Histórico de tipos de cambio por moneda y fecha.",
            "ExchangeRateId",
            [
                TextColumn("ExchangeRateId", "ExchangeRate ID", allowEditing: false, width: 220),
                TextColumn("TenantId", "Tenant", allowEditing: false, visible: false),
                LookupColumn("CurrencyId", "Moneda", currencies, required: true, width: 220, quickCreateKey: "currencies"),
                DateColumn("RateDate", "Fecha", required: true, width: 140),
                NumberColumn("BuyRate", "Compra", required: true, width: 120),
                NumberColumn("SellRate", "Venta", required: true, width: 120),
                NumberColumn("ReferenceRate", "Referencia", required: true, width: 120),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("ExchangeRateId", x.ExchangeRateId.ToString("D")),
                ("TenantId", x.TenantId.ToString("D")),
                ("CurrencyId", x.CurrencyId.ToString("D")),
                ("RateDate", x.RateDate.ToString("yyyy-MM-dd")),
                ("BuyRate", x.BuyRate),
                ("SellRate", x.SellRate),
                ("ReferenceRate", x.ReferenceRate),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetTaxesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<TaxDto>>("/api/catalogs/taxes") ?? [];

        return BuildView(
            "taxes",
            "Impuestos",
            "Impuestos base para operación de compras, ventas e inventario.",
            "TaxId",
            [
                TextColumn("TaxId", "Tax ID", allowEditing: false, width: 220),
                TextColumn("TenantId", "Tenant", allowEditing: false, visible: false),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Impuesto", required: true, width: 200),
                NumberColumn("Rate", "Tasa", required: true, width: 100),
                TextColumn("TaxType", "Tipo", required: true, width: 140),
                BoolColumn("IsDefault", "Default", width: 100),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("TaxId", x.TaxId.ToString("D")),
                ("TenantId", x.TenantId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Rate", x.Rate),
                ("TaxType", x.TaxType),
                ("IsDefault", x.IsDefault),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetUnitsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<UnitDto>>("/api/catalogs/units") ?? [];

        return BuildView(
            "units",
            "Unidades",
            "Unidades operativas y comerciales del ERP.",
            "UnitId",
            [
                TextColumn("UnitId", "Unit ID", allowEditing: false, width: 220),
                TextColumn("TenantId", "Tenant", allowEditing: false, visible: false),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Unidad", required: true, width: 200),
                TextColumn("Abbreviation", "Abreviatura", required: true, width: 130),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("UnitId", x.UnitId.ToString("D")),
                ("TenantId", x.TenantId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Abbreviation", x.Abbreviation),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetBanksAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BankDto>>("/api/catalogs/banks") ?? [];

        return BuildView(
            "banks",
            "Bancos",
            "Bancos base para tesorería y cuentas bancarias propias o de terceros.",
            "BankId",
            [
                TextColumn("BankId", "Bank ID", allowEditing: false, width: 220),
                TextColumn("TenantId", "Tenant", allowEditing: false, visible: false),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Banco", required: true, width: 220),
                TextColumn("ShortName", "Nombre corto", required: true, width: 160),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("BankId", x.BankId.ToString("D")),
                ("TenantId", x.TenantId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("ShortName", x.ShortName),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetCountriesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CountryDto>>("/api/catalogs/countries") ?? [];

        return BuildView(
            "countries",
            "Países",
            "Jerarquía geográfica base del sistema.",
            "CountryId",
            [
                TextColumn("CountryId", "Country ID", allowEditing: false, width: 220),
                TextColumn("Code", "Código", required: true, width: 100),
                TextColumn("Name", "País", required: true, width: 220),
                TextColumn("Iso2", "ISO2", required: true, width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("CountryId", x.CountryId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Iso2", x.Iso2),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetStatesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<StateDto>>("/api/catalogs/states") ?? [];
        var countries = await GetCountryLookupsAsync();

        return BuildView(
            "states",
            "Estados",
            "Estados o provincias asociados al país correspondiente.",
            "StateId",
            [
                TextColumn("StateId", "State ID", allowEditing: false, width: 220),
                LookupColumn("CountryId", "País", countries, required: true, width: 220, quickCreateKey: "countries"),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Estado", required: true, width: 220),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("StateId", x.StateId.ToString("D")),
                ("CountryId", x.CountryId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetCitiesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CityDto>>("/api/catalogs/cities") ?? [];
        var states = await GetStateLookupsAsync();

        return BuildView(
            "cities",
            "Ciudades",
            "Ciudades ligadas al estado y país de operación.",
            "CityId",
            [
                TextColumn("CityId", "City ID", allowEditing: false, width: 220),
                LookupColumn("StateId", "Estado", states, required: true, width: 240, quickCreateKey: "states"),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Ciudad", required: true, width: 220),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("CityId", x.CityId.ToString("D")),
                ("StateId", x.StateId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetDocumentSeriesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<DocumentSeriesDto>>("/api/administration/document-series") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "document-series",
            "Series documentales",
            "Series y prefijos base para documentos de operación.",
            "DocumentSeriesId",
            [
                TextColumn("DocumentSeriesId", "Series ID", allowEditing: false, width: 220),
                TextColumn("TenantId", "Tenant", allowEditing: false, visible: false),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("DocumentType", "Documento", required: true, width: 180),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Nombre", required: true, width: 180),
                TextColumn("Prefix", "Prefijo", required: true, width: 110),
                NumberColumn("CurrentNumber", "Consecutivo", required: true, width: 110),
                NumberColumn("NumberLength", "Longitud", required: true, width: 100),
                BoolColumn("IsDefault", "Default", width: 100),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("DocumentSeriesId", x.DocumentSeriesId.ToString("D")),
                ("TenantId", x.TenantId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("DocumentType", x.DocumentType),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Prefix", x.Prefix),
                ("CurrentNumber", x.CurrentNumber),
                ("NumberLength", x.NumberLength),
                ("IsDefault", x.IsDefault),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetDocumentFoliosAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<DocumentFolioDto>>("/api/administration/document-folios") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var series = await GetDocumentSeriesLookupsAsync();

        return BuildView(
            "document-folios",
            "Folios documentales",
            "Control del consecutivo real por serie y documento.",
            "DocumentFolioId",
            [
                TextColumn("DocumentFolioId", "Folio ID", allowEditing: false, width: 220),
                TextColumn("TenantId", "Tenant", allowEditing: false, visible: false),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("DocumentType", "Documento", required: true, width: 180),
                LookupColumn("SeriesId", "Serie", series, required: true, width: 240, quickCreateKey: "document-series"),
                NumberColumn("CurrentNumber", "Consecutivo", required: true, width: 120),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("DocumentFolioId", x.DocumentFolioId.ToString("D")),
                ("TenantId", x.TenantId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("DocumentType", x.DocumentType),
                ("SeriesId", x.SeriesId.ToString("D")),
                ("CurrentNumber", x.CurrentNumber),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetCompanySettingsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CompanySettingDto>>("/api/administration/company-settings") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var currencies = await GetCurrencyLookupsAsync();
        var series = await GetDocumentSeriesLookupsAsync();

        return BuildView(
            "company-settings",
            "Configuración de empresa",
            "Parámetros operativos mínimos por empresa del tenant.",
            "CompanySettingId",
            [
                TextColumn("CompanySettingId", "Setting ID", allowEditing: false, width: 220),
                TextColumn("TenantId", "Tenant", allowEditing: false, visible: false),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("CurrencyId", "Moneda base", currencies, required: true, width: 220, quickCreateKey: "currencies"),
                LookupColumn("Timezone", "Zona horaria", TimeZoneLookups.GetItems(), required: true, width: 240),
                NumberColumn("MonetaryDecimals", "Decimales moneda", required: true, width: 120),
                NumberColumn("QuantityDecimals", "Decimales cantidad", required: true, width: 120),
                LookupColumn("DefaultPurchaseSeriesId", "Serie compras", series, width: 220, required: false, quickCreateKey: "document-series"),
                LookupColumn("DefaultSalesSeriesId", "Serie ventas", series, width: 220, required: false, quickCreateKey: "document-series"),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("CompanySettingId", x.CompanySettingId.ToString("D")),
                ("TenantId", x.TenantId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("CurrencyId", x.CurrencyId.ToString("D")),
                ("Timezone", x.Timezone),
                ("MonetaryDecimals", x.MonetaryDecimals),
                ("QuantityDecimals", x.QuantityDecimals),
                ("DefaultPurchaseSeriesId", x.DefaultPurchaseSeriesId?.ToString("D")),
                ("DefaultSalesSeriesId", x.DefaultSalesSeriesId?.ToString("D")),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<List<CatalogLookupItem>> GetCompanyLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var companies = await client.GetFromJsonAsync<List<CompanyRowDto>>("/api/organization/companies") ?? [];

        return companies
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem
            {
                Id = x.CompanyId.ToString("D"),
                Name = x.Name
            })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetCurrencyLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CurrencyDto>>("/api/catalogs/currencies") ?? [];

        return rows
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .Select(x => new CatalogLookupItem
            {
                Id = x.CurrencyId.ToString("D"),
                Name = $"{x.Code} · {x.Name}"
            })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetCountryLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CountryDto>>("/api/catalogs/countries") ?? [];

        return rows
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem
            {
                Id = x.CountryId.ToString("D"),
                Name = x.Name
            })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetStateLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<StateDto>>("/api/catalogs/states") ?? [];

        return rows
            .Where(x => x.IsActive)
            .OrderBy(x => x.CountryName)
            .ThenBy(x => x.Name)
            .Select(x => new CatalogLookupItem
            {
                Id = x.StateId.ToString("D"),
                Name = $"{x.CountryName} · {x.Name}"
            })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetDocumentSeriesLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<DocumentSeriesDto>>("/api/administration/document-series") ?? [];

        return rows
            .Where(x => x.IsActive)
            .OrderBy(x => x.CompanyName)
            .ThenBy(x => x.DocumentType)
            .ThenBy(x => x.Code)
            .Select(x => new CatalogLookupItem
            {
                Id = x.DocumentSeriesId.ToString("D"),
                Name = $"{x.CompanyName} · {x.DocumentType} · {x.Code}"
            })
            .ToList();
    }

    private static CatalogViewDefinition BuildView(
        string catalogKey,
        string title,
        string subtitle,
        string keyExpr,
        List<CatalogColumnDefinition> columns,
        List<Dictionary<string, object?>> rows)
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

    private static CatalogColumnDefinition TextColumn(string field, string caption, bool required = false, bool allowEditing = true, int width = 160, bool visible = true)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "string",
            Required = required,
            AllowEditing = allowEditing,
            Width = width,
            Visible = visible
        };

    private static CatalogColumnDefinition NumberColumn(string field, string caption, bool required = false, int width = 120)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "number",
            Required = required,
            Width = width
        };

    private static CatalogColumnDefinition DateColumn(string field, string caption, bool required = false, int width = 130)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "date",
            Required = required,
            Width = width
        };

    private static CatalogColumnDefinition BoolColumn(string field, string caption, int width = 90)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "boolean",
            Width = width
        };

    private static CatalogColumnDefinition LookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, bool required = false, int width = 180, string? quickCreateKey = null)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "string",
            Required = required,
            Width = width,
            UseLookup = true,
            LookupItems = lookupItems,
            QuickCreateKey = quickCreateKey
        };

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            row[key] = value;
        }

        return row;
    }

    private static CurrencyRequest MapCurrencyRequest(JsonElement payload) => new()
    {
        TenantId = ReadGuid(payload, "TenantId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Symbol = ReadString(payload, "Symbol"),
        IsDefault = ReadBool(payload, "IsDefault"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static ExchangeRateRequest MapExchangeRateRequest(JsonElement payload) => new()
    {
        TenantId = ReadGuid(payload, "TenantId"),
        CurrencyId = ReadGuid(payload, "CurrencyId"),
        RateDate = ReadDate(payload, "RateDate"),
        BuyRate = ReadDecimal(payload, "BuyRate"),
        SellRate = ReadDecimal(payload, "SellRate"),
        ReferenceRate = ReadDecimal(payload, "ReferenceRate"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static TaxRequest MapTaxRequest(JsonElement payload) => new()
    {
        TenantId = ReadGuid(payload, "TenantId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Rate = ReadDecimal(payload, "Rate"),
        TaxType = ReadString(payload, "TaxType"),
        IsDefault = ReadBool(payload, "IsDefault"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static UnitRequest MapUnitRequest(JsonElement payload) => new()
    {
        TenantId = ReadGuid(payload, "TenantId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Abbreviation = ReadString(payload, "Abbreviation"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static BankRequest MapBankRequest(JsonElement payload) => new()
    {
        TenantId = ReadGuid(payload, "TenantId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        ShortName = ReadString(payload, "ShortName"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static CountryRequest MapCountryRequest(JsonElement payload) => new()
    {
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Iso2 = ReadString(payload, "Iso2"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static StateRequest MapStateRequest(JsonElement payload) => new()
    {
        CountryId = ReadGuid(payload, "CountryId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static CityRequest MapCityRequest(JsonElement payload) => new()
    {
        StateId = ReadGuid(payload, "StateId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static DocumentSeriesRequest MapDocumentSeriesRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        DocumentType = ReadString(payload, "DocumentType"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Prefix = ReadString(payload, "Prefix"),
        CurrentNumber = ReadInt(payload, "CurrentNumber"),
        NumberLength = ReadInt(payload, "NumberLength", 8),
        IsDefault = ReadBool(payload, "IsDefault"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static DocumentFolioRequest MapDocumentFolioRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        DocumentType = ReadString(payload, "DocumentType"),
        SeriesId = ReadGuid(payload, "SeriesId"),
        CurrentNumber = ReadInt(payload, "CurrentNumber"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static CompanySettingRequest MapCompanySettingRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        CurrencyId = ReadGuid(payload, "CurrencyId"),
        Timezone = ReadString(payload, "Timezone"),
        MonetaryDecimals = ReadInt(payload, "MonetaryDecimals", 2),
        QuantityDecimals = ReadInt(payload, "QuantityDecimals", 2),
        DefaultPurchaseSeriesId = ReadGuid(payload, "DefaultPurchaseSeriesId"),
        DefaultSalesSeriesId = ReadGuid(payload, "DefaultSalesSeriesId"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static string ReadString(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static Guid? ReadGuid(JsonElement payload, string name)
    {
        if (TryGetPropertyInsensitive(payload, name, out var value))
        {
            if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
                return parsed;

            if (value.ValueKind == JsonValueKind.Null)
                return null;
        }

        return null;
    }

    private static bool ReadBool(JsonElement payload, string name, bool fallback = false)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static int ReadInt(JsonElement payload, string name, int fallback = 0)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static decimal ReadDecimal(JsonElement payload, string name, decimal fallback = 0m)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static DateTime? ReadDate(JsonElement payload, string name)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String when DateTime.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
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
            return true;

        var camel = char.ToLowerInvariant(name[0]) + name[1..];
        if (payload.TryGetProperty(camel, out value))
            return true;

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
            return;

        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("La API devolvió un error sin detalle.");

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var message))
                throw new InvalidOperationException(message.GetString() ?? "La API devolvió un error.");
        }
        catch (JsonException)
        {
        }

        throw new InvalidOperationException(content);
    }
}

public sealed class CurrencyDto
{
    public Guid CurrencyId { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ExchangeRateDto
{
    public Guid ExchangeRateId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CurrencyId { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
    public DateTime RateDate { get; set; }
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
    public decimal ReferenceRate { get; set; }
    public bool IsActive { get; set; }
}

public sealed class TaxDto
{
    public Guid TaxId { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UnitDto
{
    public Guid UnitId { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class BankDto
{
    public Guid BankId { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CountryDto
{
    public Guid CountryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Iso2 { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class StateDto
{
    public Guid StateId { get; set; }
    public Guid CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CityDto
{
    public Guid CityId { get; set; }
    public Guid StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public Guid CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class DocumentSeriesDto
{
    public Guid DocumentSeriesId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int CurrentNumber { get; set; }
    public int NumberLength { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class DocumentFolioDto
{
    public Guid DocumentFolioId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid SeriesId { get; set; }
    public string SeriesName { get; set; } = string.Empty;
    public int CurrentNumber { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CompanySettingDto
{
    public Guid CompanySettingId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid CurrencyId { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public int MonetaryDecimals { get; set; }
    public int QuantityDecimals { get; set; }
    public Guid? DefaultPurchaseSeriesId { get; set; }
    public string DefaultPurchaseSeriesName { get; set; } = string.Empty;
    public Guid? DefaultSalesSeriesId { get; set; }
    public string DefaultSalesSeriesName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CompanyRowDto
{
    public Guid CompanyId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CurrencyRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ExchangeRateRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CurrencyId { get; set; }
    public DateTime? RateDate { get; set; }
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
    public decimal ReferenceRate { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TaxRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string TaxType { get; set; } = "Traslado";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UnitRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class BankRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class CountryRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Iso2 { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class StateRequest
{
    public Guid? CountryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class CityRequest
{
    public Guid? StateId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class DocumentSeriesRequest
{
    public Guid? CompanyId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int CurrentNumber { get; set; }
    public int NumberLength { get; set; } = 8;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DocumentFolioRequest
{
    public Guid? CompanyId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public Guid? SeriesId { get; set; }
    public int CurrentNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CompanySettingRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Timezone { get; set; } = "America/Mexico_City";
    public int MonetaryDecimals { get; set; } = 2;
    public int QuantityDecimals { get; set; } = 2;
    public Guid? DefaultPurchaseSeriesId { get; set; }
    public Guid? DefaultSalesSeriesId { get; set; }
    public bool IsActive { get; set; } = true;
}
