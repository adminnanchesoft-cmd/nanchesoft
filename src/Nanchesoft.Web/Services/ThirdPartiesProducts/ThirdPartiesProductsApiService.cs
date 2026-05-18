using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.ThirdPartiesProducts;

public sealed class ThirdPartiesProductsApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public ThirdPartiesProductsApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey.ToLowerInvariant() switch
        {
            "customers" => GetCustomersAsync(),
            "suppliers" => GetSuppliersAsync(),
            "contacts" => GetContactsAsync(),
            "addresses" => GetAddressesAsync(),
            "bank-accounts" => GetBankAccountsAsync(),
            "categories" => GetCategoriesAsync(),
            "brands" => GetBrandsAsync(),
            "models" => GetModelsAsync(),
            "items" => GetItemsAsync(),
            "price-lists" => GetPriceListsAsync(),
            "barcodes" => GetBarcodesAsync(),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");

        HttpResponseMessage response = catalogKey.ToLowerInvariant() switch
        {
            "customers" => await client.PostAsJsonAsync("/api/third-parties/customers", MapCustomerRequest(payload)),
            "suppliers" => await client.PostAsJsonAsync("/api/third-parties/suppliers", MapSupplierRequest(payload)),
            "contacts" => await client.PostAsJsonAsync("/api/third-parties/contacts", MapContactRequest(payload)),
            "addresses" => await client.PostAsJsonAsync("/api/third-parties/addresses", MapAddressRequest(payload)),
            "bank-accounts" => await client.PostAsJsonAsync("/api/third-parties/bank-accounts", MapBankAccountRequest(payload)),
            "categories" => await client.PostAsJsonAsync("/api/products/categories", MapCategoryRequest(payload)),
            "brands" => await client.PostAsJsonAsync("/api/products/brands", MapBrandRequest(payload)),
            "models" => await client.PostAsJsonAsync("/api/products/models", MapModelRequest(payload)),
            "items" => await client.PostAsJsonAsync("/api/products/items", MapItemRequest(payload)),
            "price-lists" => await client.PostAsJsonAsync("/api/products/price-lists", MapPriceListRequest(payload)),
            "barcodes" => await client.PostAsJsonAsync("/api/products/barcodes", MapBarcodeRequest(payload)),
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
            "customers" => await client.PutAsJsonAsync($"/api/third-parties/customers/{key}", MapCustomerRequest(payload)),
            "suppliers" => await client.PutAsJsonAsync($"/api/third-parties/suppliers/{key}", MapSupplierRequest(payload)),
            "contacts" => await client.PutAsJsonAsync($"/api/third-parties/contacts/{key}", MapContactRequest(payload)),
            "addresses" => await client.PutAsJsonAsync($"/api/third-parties/addresses/{key}", MapAddressRequest(payload)),
            "bank-accounts" => await client.PutAsJsonAsync($"/api/third-parties/bank-accounts/{key}", MapBankAccountRequest(payload)),
            "categories" => await client.PutAsJsonAsync($"/api/products/categories/{key}", MapCategoryRequest(payload)),
            "brands" => await client.PutAsJsonAsync($"/api/products/brands/{key}", MapBrandRequest(payload)),
            "models" => await client.PutAsJsonAsync($"/api/products/models/{key}", MapModelRequest(payload)),
            "items" => await client.PutAsJsonAsync($"/api/products/items/{key}", MapItemRequest(payload)),
            "price-lists" => await client.PutAsJsonAsync($"/api/products/price-lists/{key}", MapPriceListRequest(payload)),
            "barcodes" => await client.PutAsJsonAsync($"/api/products/barcodes/{key}", MapBarcodeRequest(payload)),
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
            "customers" => $"/api/third-parties/customers/{key}",
            "suppliers" => $"/api/third-parties/suppliers/{key}",
            "contacts" => $"/api/third-parties/contacts/{key}",
            "addresses" => $"/api/third-parties/addresses/{key}",
            "bank-accounts" => $"/api/third-parties/bank-accounts/{key}",
            "categories" => $"/api/products/categories/{key}",
            "brands" => $"/api/products/brands/{key}",
            "models" => $"/api/products/models/{key}",
            "items" => $"/api/products/items/{key}",
            "price-lists" => $"/api/products/price-lists/{key}",
            "barcodes" => $"/api/products/barcodes/{key}",
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        var response = await client.DeleteAsync(endpoint);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    private async Task<CatalogViewDefinition> GetCustomersAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CustomerDto>>("/api/third-parties/customers") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var currencies = await GetCurrencyLookupsAsync();
        var priceLists = await GetPriceListLookupsAsync();
        var allowedCompanyIds = companies
            .Select(x => Guid.TryParse(x.Id, out var parsed) ? parsed : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToHashSet();

        rows = allowedCompanyIds.Count == 0
            ? rows
            : rows.Where(x => allowedCompanyIds.Contains(x.CompanyId)).ToList();

        return BuildView(
            "customers",
            "Clientes",
            "Clientes comerciales con lista de precios, crédito y moneda base.",
            "CustomerId",
            [
                TextColumn("CustomerId", "Customer ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                LookupColumn("CurrencyId", "Moneda", currencies, width: 140, quickCreateKey: "currencies"),
                LookupColumn("PriceListId", "Lista precios", priceLists, width: 220, quickCreateKey: "price-lists"),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Cliente", required: true, width: 180),
                TextColumn("LegalName", "Razón social", required: true, width: 240),
                TextColumn("TaxId", "RFC", required: true, width: 140),
                TextColumn("Email", "Email", width: 180),
                TextColumn("Phone", "Teléfono", width: 120),
                NumberColumn("CreditLimit", "Crédito", width: 110),
                NumberColumn("PaymentTermDays", "Plazo", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("CustomerId", x.CustomerId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("CurrencyId", x.CurrencyId?.ToString("D")),
                ("PriceListId", x.PriceListId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("LegalName", x.LegalName),
                ("TaxId", x.TaxId),
                ("Email", x.Email),
                ("Phone", x.Phone),
                ("CreditLimit", x.CreditLimit),
                ("PaymentTermDays", x.PaymentTermDays),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetSuppliersAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<SupplierDto>>("/api/third-parties/suppliers") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var currencies = await GetCurrencyLookupsAsync();

        return BuildView(
            "suppliers",
            "Proveedores",
            "Proveedores con moneda base y condiciones de pago.",
            "SupplierId",
            [
                TextColumn("SupplierId", "Supplier ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                LookupColumn("CurrencyId", "Moneda", currencies, width: 140, quickCreateKey: "currencies"),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Proveedor", required: true, width: 180),
                TextColumn("LegalName", "Razón social", required: true, width: 240),
                TextColumn("TaxId", "RFC", required: true, width: 140),
                TextColumn("Email", "Email", width: 180),
                TextColumn("Phone", "Teléfono", width: 120),
                NumberColumn("PaymentTermDays", "Plazo", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("SupplierId", x.SupplierId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("CurrencyId", x.CurrencyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("LegalName", x.LegalName),
                ("TaxId", x.TaxId),
                ("Email", x.Email),
                ("Phone", x.Phone),
                ("PaymentTermDays", x.PaymentTermDays),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetContactsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ContactDto>>("/api/third-parties/contacts") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var thirdParties = await GetThirdPartyLookupsAsync();

        return BuildView(
            "contacts",
            "Contactos",
            "Contactos operativos de clientes y proveedores.",
            "ContactId",
            [
                TextColumn("ContactId", "Contact ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                LookupColumn("ThirdPartyType", "Tipo", ThirdPartyTypeLookups(), required: true, width: 120),
                LookupColumn("ThirdPartyId", "Tercero", thirdParties, required: true, width: 220),
                TextColumn("Name", "Nombre", required: true, width: 180),
                TextColumn("Position", "Puesto", width: 140),
                TextColumn("Email", "Email", width: 180),
                TextColumn("Phone", "Teléfono", width: 120),
                TextColumn("Mobile", "Móvil", width: 120),
                BoolColumn("IsPrimary", "Principal", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("ContactId", x.ContactId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("ThirdPartyType", x.ThirdPartyType),
                ("ThirdPartyId", x.ThirdPartyId.ToString("D")),
                ("Name", x.Name),
                ("Position", x.Position),
                ("Email", x.Email),
                ("Phone", x.Phone),
                ("Mobile", x.Mobile),
                ("IsPrimary", x.IsPrimary),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetAddressesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<AddressDto>>("/api/third-parties/addresses") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var thirdParties = await GetThirdPartyLookupsAsync();
        var countries = await GetCountryLookupsAsync();
        var states = await GetStateLookupsAsync();
        var cities = await GetCityLookupsAsync();

        return BuildView(
            "addresses",
            "Direcciones",
            "Direcciones fiscales, de envío y cobranza por tercero.",
            "AddressId",
            [
                TextColumn("AddressId", "Address ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                LookupColumn("ThirdPartyType", "Tipo", ThirdPartyTypeLookups(), required: true, width: 120),
                LookupColumn("ThirdPartyId", "Tercero", thirdParties, required: true, width: 220),
                TextColumn("AddressType", "Tipo dirección", required: true, width: 120),
                TextColumn("Street", "Calle", required: true, width: 200),
                TextColumn("ExteriorNumber", "No. Ext", width: 90),
                TextColumn("InteriorNumber", "No. Int", width: 90),
                TextColumn("Neighborhood", "Colonia", width: 140),
                TextColumn("ZipCode", "CP", width: 90),
                LookupColumn("CountryId", "País", countries, width: 140, quickCreateKey: "countries"),
                LookupColumn("StateId", "Estado", states, width: 160, quickCreateKey: "states"),
                LookupColumn("CityId", "Ciudad", cities, width: 160, quickCreateKey: "cities"),
                TextColumn("Reference", "Referencia", width: 180),
                BoolColumn("IsPrimary", "Principal", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("AddressId", x.AddressId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("ThirdPartyType", x.ThirdPartyType),
                ("ThirdPartyId", x.ThirdPartyId.ToString("D")),
                ("AddressType", x.AddressType),
                ("Street", x.Street),
                ("ExteriorNumber", x.ExteriorNumber),
                ("InteriorNumber", x.InteriorNumber),
                ("Neighborhood", x.Neighborhood),
                ("ZipCode", x.ZipCode),
                ("CountryId", x.CountryId?.ToString("D")),
                ("StateId", x.StateId?.ToString("D")),
                ("CityId", x.CityId?.ToString("D")),
                ("Reference", x.Reference),
                ("IsPrimary", x.IsPrimary),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetBankAccountsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BankAccountDto>>("/api/third-parties/bank-accounts") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var thirdParties = await GetThirdPartyLookupsAsync();
        var banks = await GetBankLookupsAsync();
        var currencies = await GetCurrencyLookupsAsync();

        return BuildView(
            "bank-accounts",
            "Cuentas bancarias terceros",
            "Cuentas bancarias de clientes y proveedores.",
            "BankAccountId",
            [
                TextColumn("BankAccountId", "BankAccount ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                LookupColumn("ThirdPartyType", "Tipo", ThirdPartyTypeLookups(), required: true, width: 120),
                LookupColumn("ThirdPartyId", "Tercero", thirdParties, required: true, width: 220),
                LookupColumn("BankId", "Banco", banks, required: true, width: 180, quickCreateKey: "banks"),
                LookupColumn("CurrencyId", "Moneda", currencies, width: 120, quickCreateKey: "currencies"),
                TextColumn("AccountHolder", "Titular", required: true, width: 200),
                TextColumn("AccountNumber", "Cuenta", required: true, width: 150),
                TextColumn("Clabe", "CLABE", width: 170),
                BoolColumn("IsPrimary", "Principal", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("BankAccountId", x.BankAccountId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("ThirdPartyType", x.ThirdPartyType),
                ("ThirdPartyId", x.ThirdPartyId.ToString("D")),
                ("BankId", x.BankId.ToString("D")),
                ("CurrencyId", x.CurrencyId?.ToString("D")),
                ("AccountHolder", x.AccountHolder),
                ("AccountNumber", x.AccountNumber),
                ("Clabe", x.Clabe),
                ("IsPrimary", x.IsPrimary),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetCategoriesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CategoryDto>>("/api/products/categories") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var categories = rows.Select(x => new CatalogLookupItem { Id = x.CategoryId.ToString("D"), Name = $"{x.Code} · {x.Name}" }).ToList();

        return BuildView(
            "categories",
            "Categorías",
            "Clasificación comercial base para productos y servicios.",
            "CategoryId",
            [
                TextColumn("CategoryId", "Category ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                LookupColumn("ParentId", "Padre", categories, width: 200),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Categoría", required: true, width: 180),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("CategoryId", x.CategoryId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("ParentId", x.ParentId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetBrandsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BrandDto>>("/api/products/brands") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "brands",
            "Marcas",
            "Marcas comerciales para clasificación de productos.",
            "BrandId",
            [
                TextColumn("BrandId", "Brand ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Marca", required: true, width: 180),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("BrandId", x.BrandId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetModelsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ModelDto>>("/api/products/models") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "models",
            "Modelos u opciones",
            "Modelos u opciones para estructura de catálogo de producto.",
            "ModelId",
            [
                TextColumn("ModelId", "Model ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Modelo", required: true, width: 180),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("ModelId", x.ModelId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetItemsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ItemDto>>("/api/products/items") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var categories = await GetCategoryLookupsAsync();
        var brands = await GetBrandLookupsAsync();
        var models = await GetModelLookupsAsync();
        var units = await GetUnitLookupsAsync();
        var taxes = await GetTaxLookupsAsync();
        var currencies = await GetCurrencyLookupsAsync();

        return BuildView(
            "items",
            "Productos y servicios",
            "Catálogo maestro comercial y operativo listo para compras, ventas e inventario.",
            "ItemId",
            [
                TextColumn("ItemId", "Item ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                LookupColumn("CategoryId", "Categoría", categories, width: 180, quickCreateKey: "categories"),
                LookupColumn("BrandId", "Marca", brands, width: 160, quickCreateKey: "brands"),
                LookupColumn("ModelId", "Modelo", models, width: 160, quickCreateKey: "models"),
                LookupColumn("UnitId", "Unidad", units, width: 140, quickCreateKey: "units"),
                LookupColumn("TaxId", "Impuesto", taxes, width: 140),
                LookupColumn("CurrencyId", "Moneda", currencies, width: 120, quickCreateKey: "currencies"),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Barcode", "Código barras", width: 140),
                TextColumn("Name", "Producto / servicio", required: true, width: 220),
                TextColumn("Description", "Descripción", width: 240),
                LookupColumn("ItemType", "Tipo", ItemTypeLookups(), required: true, width: 120),
                NumberColumn("BasePrice", "Precio", width: 100),
                NumberColumn("BaseCost", "Costo", width: 100),
                BoolColumn("ManagesInventory", "Inventario", width: 90),
                BoolColumn("UsesLots", "Lotes", width: 70),
                BoolColumn("UsesSerials", "Series", width: 70),
                BoolColumn("IsSaleItem", "Venta", width: 70),
                BoolColumn("IsPurchaseItem", "Compra", width: 70),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("ItemId", x.ItemId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("CategoryId", x.CategoryId?.ToString("D")),
                ("BrandId", x.BrandId?.ToString("D")),
                ("ModelId", x.ModelId?.ToString("D")),
                ("UnitId", x.UnitId?.ToString("D")),
                ("TaxId", x.TaxId?.ToString("D")),
                ("CurrencyId", x.CurrencyId?.ToString("D")),
                ("Code", x.Code),
                ("Barcode", x.Barcode),
                ("Name", x.Name),
                ("Description", x.Description),
                ("ItemType", x.ItemType),
                ("BasePrice", x.BasePrice),
                ("BaseCost", x.BaseCost),
                ("ManagesInventory", x.ManagesInventory),
                ("UsesLots", x.UsesLots),
                ("UsesSerials", x.UsesSerials),
                ("IsSaleItem", x.IsSaleItem),
                ("IsPurchaseItem", x.IsPurchaseItem),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPriceListsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PriceListDto>>("/api/products/price-lists") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var currencies = await GetCurrencyLookupsAsync();

        return BuildView(
            "price-lists",
            "Listas de precios",
            "Listas comerciales base por empresa y moneda.",
            "PriceListId",
            [
                TextColumn("PriceListId", "PriceList ID", allowEditing: false, width: 220),
                CompanyLookupColumn(companies),
                LookupColumn("CurrencyId", "Moneda", currencies, width: 140, quickCreateKey: "currencies"),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Lista", required: true, width: 180),
                BoolColumn("IsDefault", "Default", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PriceListId", x.PriceListId.ToString("D")),
                ("CompanyId", x.CompanyId.ToString("D")),
                ("CurrencyId", x.CurrencyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("IsDefault", x.IsDefault),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetBarcodesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BarcodeDto>>("/api/products/barcodes") ?? [];
        var items = await GetItemLookupsAsync();

        return BuildView(
            "barcodes",
            "Códigos de barras",
            "Barcodes alternos y principales por producto.",
            "BarcodeId",
            [
                TextColumn("BarcodeId", "Barcode ID", allowEditing: false, width: 220),
                LookupColumn("ItemId", "Producto", items, required: true, width: 240),
                TextColumn("Barcode", "Código", required: true, width: 180),
                BoolColumn("IsPrimary", "Principal", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("BarcodeId", x.BarcodeId.ToString("D")),
                ("ItemId", x.ItemId.ToString("D")),
                ("Barcode", x.Barcode),
                ("IsPrimary", x.IsPrimary),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<List<CatalogLookupItem>> GetCompanyLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CompanyLookupDto>>("/api/organization/companies") ?? [];

        if (!_authState.IsPlatformOwner && _appState.CurrentTenantId.HasValue)
        {
            rows = rows
                .Where(x => x.TenantId == _appState.CurrentTenantId.Value)
                .ToList();
        }

        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.CompanyId.ToString("D"), Name = x.Name })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetCurrencyLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CurrencyLookupDto>>("/api/catalogs/currencies") ?? [];

        if (!_authState.IsPlatformOwner && _appState.CurrentTenantId.HasValue)
        {
            rows = rows
                .Where(x => x.TenantId == _appState.CurrentTenantId.Value)
                .ToList();
        }

        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new CatalogLookupItem { Id = x.CurrencyId.ToString("D"), Name = $"{x.Code} · {x.Name}" })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPriceListLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PriceListDto>>("/api/products/price-lists") ?? [];

        if (_appState.CurrentCompanyId.HasValue)
        {
            rows = rows
                .Where(x => x.CompanyId == _appState.CurrentCompanyId.Value)
                .ToList();
        }

        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.PriceListId.ToString("D"), Name = $"{x.Code} · {x.Name}" })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetThirdPartyLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var customers = await client.GetFromJsonAsync<List<CustomerDto>>("/api/third-parties/customers") ?? [];
        var suppliers = await client.GetFromJsonAsync<List<SupplierDto>>("/api/third-parties/suppliers") ?? [];

        return customers.Where(x => x.IsActive)
            .Select(x => new CatalogLookupItem { Id = x.CustomerId.ToString("D"), Name = $"Cliente · {x.Code} · {x.Name}" })
            .Concat(suppliers.Where(x => x.IsActive)
                .Select(x => new CatalogLookupItem { Id = x.SupplierId.ToString("D"), Name = $"Proveedor · {x.Code} · {x.Name}" }))
            .OrderBy(x => x.Name)
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetCountryLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CountryLookupDto>>("/api/catalogs/countries") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.CountryId.ToString("D"), Name = x.Name })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetStateLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<StateLookupDto>>("/api/catalogs/states") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.StateId.ToString("D"), Name = x.Name })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetCityLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CityLookupDto>>("/api/catalogs/cities") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.CityId.ToString("D"), Name = x.Name })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetBankLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BankLookupDto>>("/api/catalogs/banks") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.BankId.ToString("D"), Name = x.Name })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetBrandLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BrandDto>>("/api/products/brands") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new CatalogLookupItem { Id = x.BrandId.ToString("D"), Name = $"{x.Code} - {x.Name}" })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetCategoryLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CategoryDto>>("/api/products/categories") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.CategoryId.ToString("D"), Name = x.Name })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetModelLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ModelDto>>("/api/products/models") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new CatalogLookupItem { Id = x.ModelId.ToString("D"), Name = $"{x.Code} - {x.Name}" })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetUnitLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<UnitLookupDto>>("/api/catalogs/units") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.UnitId.ToString("D"), Name = x.Name })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetTaxLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<TaxLookupDto>>("/api/catalogs/taxes") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.TaxId.ToString("D"), Name = $"{x.Name} ({x.Rate})" })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetItemLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ItemDto>>("/api/products/items") ?? [];
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CatalogLookupItem { Id = x.ItemId.ToString("D"), Name = $"{x.Code} · {x.Name}" })
            .ToList();
    }

    private static List<CatalogLookupItem> ThirdPartyTypeLookups() =>
        [
            new() { Id = "customer", Name = "Cliente" },
            new() { Id = "supplier", Name = "Proveedor" }
        ];

    private static List<CatalogLookupItem> ItemTypeLookups() =>
        [
            new() { Id = "Producto", Name = "Producto" },
            new() { Id = "Servicio", Name = "Servicio" }
        ];

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
        => new() { DataField = field, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Width = width, Visible = visible };

    private static CatalogColumnDefinition NumberColumn(string field, string caption, bool required = false, int width = 120)
        => new() { DataField = field, Caption = caption, DataType = "number", Required = required, Width = width };

    private static CatalogColumnDefinition BoolColumn(string field, string caption, int width = 90)
        => new() { DataField = field, Caption = caption, DataType = "boolean", Width = width };

    private static CatalogColumnDefinition LookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, bool required = false, int width = 180, string? quickCreateKey = null)
        => new() { DataField = field, Caption = caption, DataType = "string", Required = required, Width = width, UseLookup = true, LookupItems = lookupItems, QuickCreateKey = quickCreateKey };

    private static CatalogColumnDefinition CompanyLookupColumn(List<CatalogLookupItem> companies)
    {
        var single = companies.Count <= 1;
        return new() { DataField = "CompanyId", Caption = "Empresa", DataType = "string", Required = !single, AllowEditing = !single, Visible = !single, Width = 220, UseLookup = true, LookupItems = companies };
    }

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
            row[key] = value;
        return row;
    }

    private static CustomerRequest MapCustomerRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        CurrencyId = ReadGuid(payload, "CurrencyId"),
        PriceListId = ReadGuid(payload, "PriceListId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        LegalName = ReadString(payload, "LegalName"),
        TaxId = ReadString(payload, "TaxId"),
        Email = ReadString(payload, "Email"),
        Phone = ReadString(payload, "Phone"),
        CreditLimit = ReadDecimal(payload, "CreditLimit"),
        PaymentTermDays = ReadInt(payload, "PaymentTermDays"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static SupplierRequest MapSupplierRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        CurrencyId = ReadGuid(payload, "CurrencyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        LegalName = ReadString(payload, "LegalName"),
        TaxId = ReadString(payload, "TaxId"),
        Email = ReadString(payload, "Email"),
        Phone = ReadString(payload, "Phone"),
        PaymentTermDays = ReadInt(payload, "PaymentTermDays"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static ContactRequest MapContactRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        ThirdPartyType = ReadString(payload, "ThirdPartyType"),
        ThirdPartyId = ReadGuid(payload, "ThirdPartyId"),
        Name = ReadString(payload, "Name"),
        Position = ReadString(payload, "Position"),
        Email = ReadString(payload, "Email"),
        Phone = ReadString(payload, "Phone"),
        Mobile = ReadString(payload, "Mobile"),
        IsPrimary = ReadBool(payload, "IsPrimary"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static AddressRequest MapAddressRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        ThirdPartyType = ReadString(payload, "ThirdPartyType"),
        ThirdPartyId = ReadGuid(payload, "ThirdPartyId"),
        AddressType = ReadString(payload, "AddressType"),
        Street = ReadString(payload, "Street"),
        ExteriorNumber = ReadString(payload, "ExteriorNumber"),
        InteriorNumber = ReadString(payload, "InteriorNumber"),
        Neighborhood = ReadString(payload, "Neighborhood"),
        ZipCode = ReadString(payload, "ZipCode"),
        CountryId = ReadGuid(payload, "CountryId"),
        StateId = ReadGuid(payload, "StateId"),
        CityId = ReadGuid(payload, "CityId"),
        Reference = ReadString(payload, "Reference"),
        IsPrimary = ReadBool(payload, "IsPrimary"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static BankAccountRequest MapBankAccountRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        ThirdPartyType = ReadString(payload, "ThirdPartyType"),
        ThirdPartyId = ReadGuid(payload, "ThirdPartyId"),
        BankId = ReadGuid(payload, "BankId"),
        CurrencyId = ReadGuid(payload, "CurrencyId"),
        AccountHolder = ReadString(payload, "AccountHolder"),
        AccountNumber = ReadString(payload, "AccountNumber"),
        Clabe = ReadString(payload, "Clabe"),
        IsPrimary = ReadBool(payload, "IsPrimary"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static CategoryRequest MapCategoryRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        ParentId = ReadGuid(payload, "ParentId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static BrandRequest MapBrandRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static ModelRequest MapModelRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static ItemRequest MapItemRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        CategoryId = ReadGuid(payload, "CategoryId"),
        BrandId = ReadGuid(payload, "BrandId"),
        ModelId = ReadGuid(payload, "ModelId"),
        UnitId = ReadGuid(payload, "UnitId"),
        TaxId = ReadGuid(payload, "TaxId"),
        CurrencyId = ReadGuid(payload, "CurrencyId"),
        Code = ReadString(payload, "Code"),
        Barcode = ReadString(payload, "Barcode"),
        Name = ReadString(payload, "Name"),
        Description = ReadString(payload, "Description"),
        ItemType = ReadString(payload, "ItemType"),
        BasePrice = ReadDecimal(payload, "BasePrice"),
        BaseCost = ReadDecimal(payload, "BaseCost"),
        ManagesInventory = ReadBool(payload, "ManagesInventory"),
        UsesLots = ReadBool(payload, "UsesLots"),
        UsesSerials = ReadBool(payload, "UsesSerials"),
        IsSaleItem = ReadBool(payload, "IsSaleItem", true),
        IsPurchaseItem = ReadBool(payload, "IsPurchaseItem", true),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PriceListRequest MapPriceListRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        CurrencyId = ReadGuid(payload, "CurrencyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        IsDefault = ReadBool(payload, "IsDefault"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static BarcodeRequest MapBarcodeRequest(JsonElement payload) => new()
    {
        ItemId = ReadGuid(payload, "ItemId"),
        Barcode = ReadString(payload, "Barcode"),
        IsPrimary = ReadBool(payload, "IsPrimary"),
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
        if (TryGetPropertyInsensitive(payload, name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
            return parsed;
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
            JsonValueKind.Number when value.TryGetInt32(out var parsed) => parsed,
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
            JsonValueKind.Number when value.TryGetDecimal(out var parsed) => parsed,
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
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
                throw new InvalidOperationException(message.GetString() ?? content);
        }
        catch (JsonException)
        {
        }

        throw new InvalidOperationException(content);
    }
}

public sealed class CompanyLookupDto
{
    public Guid CompanyId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CurrencyLookupDto
{
    public Guid CurrencyId { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CountryLookupDto
{
    public Guid CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class StateLookupDto
{
    public Guid StateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CityLookupDto
{
    public Guid CityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class BankLookupDto
{
    public Guid BankId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class TaxLookupDto
{
    public Guid TaxId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UnitLookupDto
{
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CustomerDto
{
    public Guid CustomerId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? PriceListId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SupplierDto
{
    public Guid SupplierId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ContactDto
{
    public Guid ContactId { get; set; }
    public Guid CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AddressDto
{
    public Guid AddressId { get; set; }
    public Guid CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ExteriorNumber { get; set; } = string.Empty;
    public string InteriorNumber { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public Guid? CityId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BankAccountDto
{
    public Guid BankAccountId { get; set; }
    public Guid CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }
    public Guid BankId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CategoryDto
{
    public Guid CategoryId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class BrandDto
{
    public Guid BrandId { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class ModelDto
{
    public Guid ModelId { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class ItemDto
{
    public Guid ItemId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal BaseCost { get; set; }
    public bool ManagesInventory { get; set; }
    public bool UsesLots { get; set; }
    public bool UsesSerials { get; set; }
    public bool IsSaleItem { get; set; }
    public bool IsPurchaseItem { get; set; }
    public bool IsActive { get; set; }
}

public sealed class PriceListDto
{
    public Guid PriceListId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BarcodeDto
{
    public Guid BarcodeId { get; set; }
    public Guid ItemId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CustomerRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? PriceListId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SupplierRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ContactRequest
{
    public Guid? CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid? ThirdPartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AddressRequest
{
    public Guid? CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid? ThirdPartyId { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ExteriorNumber { get; set; } = string.Empty;
    public string InteriorNumber { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public Guid? CityId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BankAccountRequest
{
    public Guid? CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid? ThirdPartyId { get; set; }
    public Guid? BankId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CategoryRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class BrandRequest
{
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ModelRequest
{
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ItemRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal BaseCost { get; set; }
    public bool ManagesInventory { get; set; }
    public bool UsesLots { get; set; }
    public bool UsesSerials { get; set; }
    public bool IsSaleItem { get; set; }
    public bool IsPurchaseItem { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PriceListRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BarcodeRequest
{
    public Guid? ItemId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}
