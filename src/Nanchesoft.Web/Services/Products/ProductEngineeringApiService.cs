using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.Products;

public sealed class ProductEngineeringApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductEngineeringApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey.ToLowerInvariant() switch
        {
            "colors" => GetSimpleOrangeCatalogAsync("colors", "/api/products/colors", "Colores", "Catálogo operativo Orange/Silvasoft para colores predominantes del producto."),
            "leather-types" => GetSimpleOrangeCatalogAsync("leather-types", "/api/products/leather-types", "Pieles", "Tipos de piel o material exterior usados en el calzado."),
            "soles" => GetSimpleOrangeCatalogAsync("soles", "/api/products/soles", "Suelas", "Catálogo de suelas utilizadas en la fabricación del calzado."),
            "manufacturing-types" => GetSimpleOrangeCatalogAsync("manufacturing-types", "/api/products/manufacturing-types", "Manufacturas", "Tipos de manufactura del producto: a mano, máquina u otros procesos propios."),
            "toe-caps" => GetSimpleOrangeCatalogAsync("toe-caps", "/api/products/toe-caps", "Cascos", "Cascos o punteras usados en el producto, con material o referencia."),
            "sole-colors" => GetSimpleOrangeCatalogAsync("sole-colors", "/api/products/sole-colors", "Color suela", "Catálogo de colores de suela operativo para calzado."),
            "dies" => GetSimpleOrangeCatalogAsync("dies", "/api/products/dies", "Troqueles", "Troqueles relacionados al producto y tarjeta de producción."),
            "quality-control-dies" => GetSimpleOrangeCatalogAsync("quality-control-dies", "/api/products/quality-control-dies", "Troquel control calidad", "Troqueles o referencias de revisión usados por control de calidad."),
            "folio-patterns" => GetSimpleOrangeCatalogAsync("folio-patterns", "/api/products/folio-patterns", "Foliados", "Formas de foliado solicitadas por cliente o producción."),
            "unit-conversions" => GetUnitConversionsAsync(),
            "size-runs" => GetSizeRunsAsync(),
            "families" => GetFamiliesAsync(),
            "lasts" => GetLastsAsync(),
            "lines" => GetLinesAsync(),
            "styles" => GetStylesAsync(),
            "embroidery-patterns" => GetEmbroideryPatternsAsync(),
            "item-engineering-profiles" => GetEngineeringProfilesAsync(),
            "material-families" => GetMaterialFamiliesAsync(),
            "material-subfamilies" => GetMaterialSubfamiliesAsync(),
            "material-items" => GetMaterialItemsAsync(),
            "material-suppliers" => GetMaterialSuppliersAsync(),
            "material-supplier-cost-history" => GetMaterialSupplierCostHistoryAsync(),
            "finished-products" => GetFinishedProductsAsync(),
            "product-components" => GetProductComponentsAsync(),
            "finished-product-materials" => GetFinishedProductMaterialsAsync(),
            "product-consumption-profiles" => GetProductConsumptionProfilesAsync(),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = catalogKey.ToLowerInvariant() switch
        {
            "colors" => await client.PostAsJsonAsync("/api/products/colors", MapSimpleOrangeCatalogRequest(payload)),
            "leather-types" => await client.PostAsJsonAsync("/api/products/leather-types", MapSimpleOrangeCatalogRequest(payload)),
            "soles" => await client.PostAsJsonAsync("/api/products/soles", MapSimpleOrangeCatalogRequest(payload)),
            "manufacturing-types" => await client.PostAsJsonAsync("/api/products/manufacturing-types", MapSimpleOrangeCatalogRequest(payload)),
            "toe-caps" => await client.PostAsJsonAsync("/api/products/toe-caps", MapSimpleOrangeCatalogRequest(payload)),
            "sole-colors" => await client.PostAsJsonAsync("/api/products/sole-colors", MapSimpleOrangeCatalogRequest(payload)),
            "dies" => await client.PostAsJsonAsync("/api/products/dies", MapSimpleOrangeCatalogRequest(payload)),
            "quality-control-dies" => await client.PostAsJsonAsync("/api/products/quality-control-dies", MapSimpleOrangeCatalogRequest(payload)),
            "folio-patterns" => await client.PostAsJsonAsync("/api/products/folio-patterns", MapSimpleOrangeCatalogRequest(payload)),
            "unit-conversions" => await client.PostAsJsonAsync("/api/products/unit-conversions", MapUnitConversionRequest(payload)),
            "size-runs" => await client.PostAsJsonAsync("/api/products/size-runs", MapSizeRunRequest(payload)),
            "families" => await client.PostAsJsonAsync("/api/products/families", MapFamilyRequest(payload)),
            "lasts" => await client.PostAsJsonAsync("/api/products/lasts", MapLastRequest(payload)),
            "lines" => await client.PostAsJsonAsync("/api/products/lines", MapLineRequest(payload)),
            "styles" => await client.PostAsJsonAsync("/api/products/styles", MapStyleRequest(payload)),
            "embroidery-patterns" => await client.PostAsJsonAsync("/api/products/embroidery-patterns", MapEmbroideryPatternRequest(payload)),
            "item-engineering-profiles" => await client.PostAsJsonAsync("/api/products/item-engineering-profiles", MapEngineeringProfileRequest(payload)),
            "material-families" => await client.PostAsJsonAsync("/api/products/material-families", MapMaterialFamilyRequest(payload)),
            "material-subfamilies" => await client.PostAsJsonAsync("/api/products/material-subfamilies", MapMaterialSubfamilyRequest(payload)),
            "material-items" => await client.PostAsJsonAsync("/api/products/material-items", MapMaterialItemRequest(payload)),
            "material-suppliers" => await client.PostAsJsonAsync("/api/products/material-suppliers", MapMaterialSupplierRequest(payload)),
            "material-supplier-cost-history" => await client.PostAsJsonAsync("/api/products/material-supplier-cost-history", MapMaterialSupplierCostHistoryRequest(payload)),
            "finished-products" => await client.PostAsJsonAsync("/api/products/finished-products", MapFinishedProductRequest(payload)),
            "product-components" => await client.PostAsJsonAsync("/api/products/product-components", MapProductComponentRequest(payload)),
            "finished-product-materials" => await client.PostAsJsonAsync("/api/products/finished-product-materials", MapFinishedProductMaterialRequest(payload)),
            "product-consumption-profiles" => await client.PostAsJsonAsync("/api/products/product-consumption-profiles", MapProductConsumptionProfileRequest(payload)),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = catalogKey.ToLowerInvariant() switch
        {
            "colors" => await client.PutAsJsonAsync($"/api/products/colors/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "leather-types" => await client.PutAsJsonAsync($"/api/products/leather-types/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "soles" => await client.PutAsJsonAsync($"/api/products/soles/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "manufacturing-types" => await client.PutAsJsonAsync($"/api/products/manufacturing-types/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "toe-caps" => await client.PutAsJsonAsync($"/api/products/toe-caps/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "sole-colors" => await client.PutAsJsonAsync($"/api/products/sole-colors/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "dies" => await client.PutAsJsonAsync($"/api/products/dies/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "quality-control-dies" => await client.PutAsJsonAsync($"/api/products/quality-control-dies/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "folio-patterns" => await client.PutAsJsonAsync($"/api/products/folio-patterns/{key}", MapSimpleOrangeCatalogRequest(payload)),
            "unit-conversions" => await client.PutAsJsonAsync($"/api/products/unit-conversions/{key}", MapUnitConversionRequest(payload)),
            "size-runs" => await client.PutAsJsonAsync($"/api/products/size-runs/{key}", MapSizeRunRequest(payload)),
            "families" => await client.PutAsJsonAsync($"/api/products/families/{key}", MapFamilyRequest(payload)),
            "lasts" => await client.PutAsJsonAsync($"/api/products/lasts/{key}", MapLastRequest(payload)),
            "lines" => await client.PutAsJsonAsync($"/api/products/lines/{key}", MapLineRequest(payload)),
            "styles" => await client.PutAsJsonAsync($"/api/products/styles/{key}", MapStyleRequest(payload)),
            "embroidery-patterns" => await client.PutAsJsonAsync($"/api/products/embroidery-patterns/{key}", MapEmbroideryPatternRequest(payload)),
            "item-engineering-profiles" => await client.PutAsJsonAsync($"/api/products/item-engineering-profiles/{key}", MapEngineeringProfileRequest(payload)),
            "material-families" => await client.PutAsJsonAsync($"/api/products/material-families/{key}", MapMaterialFamilyRequest(payload)),
            "material-subfamilies" => await client.PutAsJsonAsync($"/api/products/material-subfamilies/{key}", MapMaterialSubfamilyRequest(payload)),
            "material-items" => await client.PutAsJsonAsync($"/api/products/material-items/{key}", MapMaterialItemRequest(payload)),
            "material-suppliers" => await client.PutAsJsonAsync($"/api/products/material-suppliers/{key}", MapMaterialSupplierRequest(payload)),
            "material-supplier-cost-history" => await client.PutAsJsonAsync($"/api/products/material-supplier-cost-history/{key}", MapMaterialSupplierCostHistoryRequest(payload)),
            "finished-products" => await client.PutAsJsonAsync($"/api/products/finished-products/{key}", MapFinishedProductRequest(payload)),
            "product-components" => await client.PutAsJsonAsync($"/api/products/product-components/{key}", MapProductComponentRequest(payload)),
            "finished-product-materials" => await client.PutAsJsonAsync($"/api/products/finished-product-materials/{key}", MapFinishedProductMaterialRequest(payload)),
            "product-consumption-profiles" => await client.PutAsJsonAsync($"/api/products/product-consumption-profiles/{key}", MapProductConsumptionProfileRequest(payload)),
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
            "colors" => $"/api/products/colors/{key}",
            "leather-types" => $"/api/products/leather-types/{key}",
            "soles" => $"/api/products/soles/{key}",
            "manufacturing-types" => $"/api/products/manufacturing-types/{key}",
            "toe-caps" => $"/api/products/toe-caps/{key}",
            "sole-colors" => $"/api/products/sole-colors/{key}",
            "dies" => $"/api/products/dies/{key}",
            "quality-control-dies" => $"/api/products/quality-control-dies/{key}",
            "folio-patterns" => $"/api/products/folio-patterns/{key}",
            "unit-conversions" => $"/api/products/unit-conversions/{key}",
            "size-runs" => $"/api/products/size-runs/{key}",
            "families" => $"/api/products/families/{key}",
            "lasts" => $"/api/products/lasts/{key}",
            "lines" => $"/api/products/lines/{key}",
            "styles" => $"/api/products/styles/{key}",
            "embroidery-patterns" => $"/api/products/embroidery-patterns/{key}",
            "item-engineering-profiles" => $"/api/products/item-engineering-profiles/{key}",
            "material-families" => $"/api/products/material-families/{key}",
            "material-subfamilies" => $"/api/products/material-subfamilies/{key}",
            "material-items" => $"/api/products/material-items/{key}",
            "material-suppliers" => $"/api/products/material-suppliers/{key}",
            "material-supplier-cost-history" => $"/api/products/material-supplier-cost-history/{key}",
            "finished-products" => $"/api/products/finished-products/{key}",
            "product-components" => $"/api/products/product-components/{key}",
            "finished-product-materials" => $"/api/products/finished-product-materials/{key}",
            "product-consumption-profiles" => $"/api/products/product-consumption-profiles/{key}",
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };
        var response = await client.DeleteAsync(endpoint);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    private async Task<CatalogViewDefinition> GetUnitConversionsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<UnitConversionDto>>("/api/products/unit-conversions") ?? [];
        var units = await GetUnitLookupsAsync();
        return BuildView("unit-conversions", "Unit Conversions", "Legacy Orange uses explicit unit conversions for purchasing, inventory and explosion.", "UnitConversionId", [
            TextColumn("UnitConversionId", "ID", allowEditing: false, width: 220),
            LookupColumn("FromUnitId", "From Unit", units, required: true, width: 180),
            LookupColumn("ToUnitId", "To Unit", units, required: true, width: 180),
            NumberColumn("ConversionFactor", "Factor", width: 120),
            BoolColumn("IsBidirectional", "Bidirectional", width: 110),
            TextColumn("Notes", "Notes", width: 220),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("UnitConversionId", x.UnitConversionId.ToString("D")), ("FromUnitId", x.FromUnitId.ToString("D")), ("ToUnitId", x.ToUnitId.ToString("D")), ("ConversionFactor", x.ConversionFactor), ("IsBidirectional", x.IsBidirectional), ("Notes", x.Notes), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetSizeRunsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ProductSizeRunDto>>("/api/products/size-runs") ?? [];
        return BuildView("size-runs", "Size Runs", "Standardized version of legacy Corrida, including unique size run U.", "ProductSizeRunId", [
            TextColumn("ProductSizeRunId", "ID", allowEditing: false, width: 220),
            TextColumn("Code", "Code", required: true, width: 100),
            TextColumn("Name", "Name", required: true, width: 180),
            TextColumn("DisplayName", "Display Name", width: 160),
            BoolColumn("IsUniqueSizeRun", "Unique", width: 90),
            NumberColumn("SizeCount", "Sizes", width: 80),
            TextColumn("SizeDefinitions", "Sizes CSV", width: 180),
            TextColumn("SizesPreview", "Preview", allowEditing: false, width: 180),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("ProductSizeRunId", x.ProductSizeRunId.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("DisplayName", x.DisplayName), ("IsUniqueSizeRun", x.IsUniqueSizeRun), ("SizeCount", x.SizeCount), ("SizeDefinitions", x.SizeDefinitions), ("SizesPreview", x.SizesPreview), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetFamiliesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ProductFamilyDto>>("/api/products/families") ?? [];
        return BuildView("families", "Product Families", "Legacy Orange family classification translated to standardized English product families.", "ProductFamilyId", [
            TextColumn("ProductFamilyId", "ID", allowEditing: false, width: 220),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 200),
            TextColumn("StatisticsGroup", "Statistics Group", width: 180),
            BoolColumn("IsFinishedProductFamily", "Finished Goods", width: 120),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("ProductFamilyId", x.ProductFamilyId.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("StatisticsGroup", x.StatisticsGroup), ("IsFinishedProductFamily", x.IsFinishedProductFamily), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetLastsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ProductLastDto>>("/api/products/lasts") ?? [];
        return BuildView("lasts", "Product Lasts", "Standardized catalog for legacy Horma.", "ProductLastId", [
            TextColumn("ProductLastId", "ID", allowEditing: false, width: 220),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 220),
            TextColumn("WidthReference", "Width Reference", width: 140),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("ProductLastId", x.ProductLastId.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("WidthReference", x.WidthReference), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetLinesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ProductLineDto>>("/api/products/lines") ?? [];
        var families = await GetProductFamilyLookupsAsync();
        var lasts = await GetProductLastLookupsAsync();
        return BuildView("lines", "Product Lines", "Legacy Linea aligned to family and last.", "ProductLineId", [
            TextColumn("ProductLineId", "ID", allowEditing: false, width: 220),
            LookupColumn("ProductFamilyId", "Family", families, width: 180),
            LookupColumn("ProductLastId", "Last", lasts, width: 180),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 220),
            TextColumn("ShortName", "Short Name", width: 120),
            BoolColumn("AllowsDiscount", "Allows Discount", width: 120),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("ProductLineId", x.ProductLineId.ToString("D")), ("ProductFamilyId", x.ProductFamilyId?.ToString("D")), ("ProductLastId", x.ProductLastId?.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("ShortName", x.ShortName), ("AllowsDiscount", x.AllowsDiscount), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetStylesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ProductStyleDto>>("/api/products/styles") ?? [];
        var lines = await GetProductLineLookupsAsync();
        var lasts = await GetProductLastLookupsAsync();
        return BuildView("styles", "Product Styles", "Legacy Estilo translated to standardized product style catalog.", "ProductStyleId", [
            TextColumn("ProductStyleId", "ID", allowEditing: false, width: 220),
            LookupColumn("ProductLineId", "Line", lines, width: 180),
            LookupColumn("ProductLastId", "Last", lasts, width: 180),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 220),
            TextColumn("CustomerLabel1", "Customer Label 1", width: 160),
            TextColumn("CustomerLabel2", "Customer Label 2", width: 160),
            TextColumn("ColorLabel", "Color Label", width: 140),
            TextColumn("DieCutReference", "Die Cut", width: 120),
            NumberColumn("MaxLotSize", "Max Lot", width: 100),
            BoolColumn("HasAuthorizedConsumption", "Authorized Consumption", width: 150),
            BoolColumn("HandlesFractionsByStyle", "Fractions by Style", width: 130),
            TextColumn("OutsourcedProcessName", "Outsourced Process", width: 180),
            TextColumn("PhotoUrl", "Photo Url", width: 220),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("ProductStyleId", x.ProductStyleId.ToString("D")), ("ProductLineId", x.ProductLineId?.ToString("D")), ("ProductLastId", x.ProductLastId?.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("CustomerLabel1", x.CustomerLabel1), ("CustomerLabel2", x.CustomerLabel2), ("ColorLabel", x.ColorLabel), ("DieCutReference", x.DieCutReference), ("MaxLotSize", x.MaxLotSize), ("HasAuthorizedConsumption", x.HasAuthorizedConsumption), ("HandlesFractionsByStyle", x.HandlesFractionsByStyle), ("OutsourcedProcessName", x.OutsourcedProcessName), ("PhotoUrl", x.PhotoUrl), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetEmbroideryPatternsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<EmbroideryPatternDto>>("/api/products/embroidery-patterns") ?? [];
        return BuildView("embroidery-patterns", "Embroidery Patterns", "Standardized catalog for legacy Bordado.", "EmbroideryPatternId", [
            TextColumn("EmbroideryPatternId", "ID", allowEditing: false, width: 220),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 220),
            NumberColumn("Sequence", "Sequence", width: 90),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("EmbroideryPatternId", x.EmbroideryPatternId.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("Sequence", x.Sequence), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetEngineeringProfilesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ItemEngineeringProfileDto>>("/api/products/item-engineering-profiles") ?? [];
        var items = await GetItemLookupsAsync();
        var styles = await GetProductStyleLookupsAsync();
        var runs = await GetProductSizeRunLookupsAsync();
        var embroideries = await GetEmbroideryPatternLookupsAsync();
        return BuildView("item-engineering-profiles", "Item Engineering Profiles", "Bridge between item master and Orange engineering attributes.", "ItemEngineeringProfileId", [
            TextColumn("ItemEngineeringProfileId", "ID", allowEditing: false, width: 220),
            LookupColumn("ItemId", "Item", items, required: true, width: 220),
            LookupColumn("ProductStyleId", "Style", styles, width: 180),
            LookupColumn("ProductSizeRunId", "Size Run", runs, width: 180),
            LookupColumn("EmbroideryPatternId", "Embroidery", embroideries, width: 180),
            LookupColumn("PrimaryMaterialItemId", "Primary Material", items, width: 220),
            TextColumn("FolioPattern", "Folio Pattern", width: 140),
            TextColumn("TechnicalSheetMode", "Technical Sheet Mode", width: 140),
            TextColumn("ProcessVoucherProfile", "Voucher Profile", width: 160),
            BoolColumn("HasPhoto", "Has Photo", width: 90),
            BoolColumn("HasConsumptionDefinition", "Has Consumption", width: 120),
            BoolColumn("HasMaterialAssignments", "Has Material Assignments", width: 140),
            BoolColumn("IsAuthorizedForExplosion", "Authorized", width: 100),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("ItemEngineeringProfileId", x.ItemEngineeringProfileId.ToString("D")), ("ItemId", x.ItemId.ToString("D")), ("ProductStyleId", x.ProductStyleId?.ToString("D")), ("ProductSizeRunId", x.ProductSizeRunId?.ToString("D")), ("EmbroideryPatternId", x.EmbroideryPatternId?.ToString("D")), ("PrimaryMaterialItemId", x.PrimaryMaterialItemId?.ToString("D")), ("FolioPattern", x.FolioPattern), ("TechnicalSheetMode", x.TechnicalSheetMode), ("ProcessVoucherProfile", x.ProcessVoucherProfile), ("HasPhoto", x.HasPhoto), ("HasConsumptionDefinition", x.HasConsumptionDefinition), ("HasMaterialAssignments", x.HasMaterialAssignments), ("IsAuthorizedForExplosion", x.IsAuthorizedForExplosion), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetMaterialFamiliesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<MaterialFamilyDto>>("/api/products/material-families") ?? [];
        return BuildView("material-families", "Material Families", "Orange material families translated to standardized English catalogs.", "MaterialFamilyId", [
            TextColumn("MaterialFamilyId", "ID", allowEditing: false, width: 220),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 220),
            TextColumn("InventoryGroup", "Inventory Group", width: 160),
            TextColumn("Notes", "Notes", width: 240),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("MaterialFamilyId", x.MaterialFamilyId.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("InventoryGroup", x.InventoryGroup), ("Notes", x.Notes), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetMaterialSubfamiliesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<MaterialSubfamilyDto>>("/api/products/material-subfamilies") ?? [];
        var families = await GetMaterialFamilyLookupsAsync();
        return BuildView("material-subfamilies", "Material Subfamilies", "Legacy subfamilies used for direct and indirect material grouping.", "MaterialSubfamilyId", [
            TextColumn("MaterialSubfamilyId", "ID", allowEditing: false, width: 220),
            LookupColumn("MaterialFamilyId", "Material Family", families, required: true, width: 200),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 220),
            TextColumn("MaterialType", "Type", width: 120),
            BoolColumn("IsDirectMaterial", "Direct", width: 90),
            TextColumn("Notes", "Notes", width: 220),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("MaterialSubfamilyId", x.MaterialSubfamilyId.ToString("D")), ("MaterialFamilyId", x.MaterialFamilyId.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("MaterialType", x.MaterialType), ("IsDirectMaterial", x.IsDirectMaterial), ("Notes", x.Notes), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetMaterialItemsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<MaterialItemDto>>("/api/products/material-items") ?? [];
        var subfamilies = await GetMaterialSubfamilyLookupsAsync();
        var units = await GetUnitLookupsAsync();
        var suppliers = await GetSupplierLookupsAsync();
        return BuildView("material-items", "Material Items", "Orange materials with authorized cost, supplier and purchase/issue units.", "MaterialItemId", [
            TextColumn("MaterialItemId", "ID", allowEditing: false, width: 220),
            LookupColumn("MaterialSubfamilyId", "Subfamily", subfamilies, required: true, width: 180),
            LookupColumn("PurchaseUnitId", "Purchase Unit", units, width: 160),
            LookupColumn("IssueUnitId", "Issue Unit", units, width: 160),
            LookupColumn("SupplierId", "Supplier", suppliers, width: 220),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 220),
            TextColumn("LegacyMaterialName", "Legacy Name", width: 180),
            NumberColumn("AuthorizedCost", "Authorized Cost", width: 120),
            NumberColumn("LastPurchaseCost", "Last Cost", width: 120),
            NumberColumn("StandardCost", "Standard Cost", width: 120),
            TextColumn("CostStatus", "Cost Status", width: 120),
            BoolColumn("IsServiceItem", "Service", width: 90),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("MaterialItemId", x.MaterialItemId.ToString("D")), ("MaterialSubfamilyId", x.MaterialSubfamilyId.ToString("D")), ("PurchaseUnitId", x.PurchaseUnitId?.ToString("D")), ("IssueUnitId", x.IssueUnitId?.ToString("D")), ("SupplierId", x.SupplierId?.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("LegacyMaterialName", x.LegacyMaterialName), ("AuthorizedCost", x.AuthorizedCost), ("LastPurchaseCost", x.LastPurchaseCost), ("StandardCost", x.StandardCost), ("CostStatus", x.CostStatus), ("IsServiceItem", x.IsServiceItem), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetMaterialSuppliersAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<MaterialSupplierAssignmentDto>>("/api/products/material-suppliers") ?? [];
        var materials = await GetMaterialItemLookupsAsync();
        var suppliers = await GetSupplierLookupsAsync();
        var units = await GetUnitLookupsAsync();
        var currencies = await GetCurrencyLookupsAsync();
        return BuildView("material-suppliers", "Matriz material-proveedor", "Relación múltiple de proveedores por material con costo autorizado, último costo y proveedor principal.", "MaterialSupplierAssignmentId", [
            TextColumn("MaterialSupplierAssignmentId", "ID", allowEditing: false, width: 220),
            LookupColumn("MaterialItemId", "Material", materials, required: true, width: 220),
            LookupColumn("SupplierId", "Proveedor", suppliers, required: true, width: 220),
            LookupColumn("PurchaseUnitId", "Unidad compra", units, width: 150),
            LookupColumn("CurrencyId", "Moneda", currencies, width: 140),
            TextColumn("SupplierItemCode", "Código proveedor", width: 140),
            TextColumn("SupplierItemName", "Nombre proveedor", width: 220),
            NumberColumn("ConversionFactor", "Factor conv.", width: 110),
            NumberColumn("AuthorizedCost", "Costo autorizado", width: 130),
            NumberColumn("LastCost", "Último costo", width: 120),
            NumberColumn("LeadTimeDays", "Lead time", width: 100),
            NumberColumn("MinimumOrderQuantity", "Mínimo", width: 110),
            BoolColumn("IsPreferred", "Principal", width: 90),
            DateColumn("ValidFrom", "Vigencia desde", width: 120),
            DateColumn("ValidTo", "Vigencia hasta", width: 120),
            TextColumn("Notes", "Notas", width: 220),
            BoolColumn("IsActive", "Activo", width: 90)
        ], rows.Select(x => Row(("MaterialSupplierAssignmentId", x.MaterialSupplierAssignmentId.ToString("D")), ("MaterialItemId", x.MaterialItemId.ToString("D")), ("SupplierId", x.SupplierId.ToString("D")), ("PurchaseUnitId", x.PurchaseUnitId?.ToString("D")), ("CurrencyId", x.CurrencyId?.ToString("D")), ("SupplierItemCode", x.SupplierItemCode), ("SupplierItemName", x.SupplierItemName), ("ConversionFactor", x.ConversionFactor), ("AuthorizedCost", x.AuthorizedCost), ("LastCost", x.LastCost), ("LeadTimeDays", x.LeadTimeDays), ("MinimumOrderQuantity", x.MinimumOrderQuantity), ("IsPreferred", x.IsPreferred), ("ValidFrom", x.ValidFrom), ("ValidTo", x.ValidTo), ("Notes", x.Notes), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetMaterialSupplierCostHistoryAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<MaterialSupplierCostHistoryDto>>("/api/products/material-supplier-cost-history") ?? [];
        var assignments = await GetMaterialSupplierAssignmentLookupsAsync();
        var currencies = await GetCurrencyLookupsAsync();
        return BuildView("material-supplier-cost-history", "Histórico de costos material-proveedor", "Histórico manual o documental del costo por proveedor para cada material.", "MaterialSupplierCostHistoryId", [
            TextColumn("MaterialSupplierCostHistoryId", "ID", allowEditing: false, width: 220),
            LookupColumn("MaterialSupplierAssignmentId", "Matriz material/proveedor", assignments, required: true, width: 260),
            LookupColumn("CurrencyId", "Moneda", currencies, width: 140),
            DateColumn("CostDate", "Fecha costo", required: true, width: 120),
            NumberColumn("Cost", "Costo", required: true, width: 120),
            NumberColumn("ExchangeRate", "Tipo cambio", width: 120),
            TextColumn("SourceDocumentType", "Tipo doc", width: 120),
            TextColumn("SourceDocumentNumber", "Folio doc", width: 120),
            TextColumn("Notes", "Notas", width: 220),
            BoolColumn("IsActive", "Activo", width: 90)
        ], rows.Select(x => Row(("MaterialSupplierCostHistoryId", x.MaterialSupplierCostHistoryId.ToString("D")), ("MaterialSupplierAssignmentId", x.MaterialSupplierAssignmentId.ToString("D")), ("CurrencyId", x.CurrencyId?.ToString("D")), ("CostDate", x.CostDate), ("Cost", x.Cost), ("ExchangeRate", x.ExchangeRate), ("SourceDocumentType", x.SourceDocumentType), ("SourceDocumentNumber", x.SourceDocumentNumber), ("Notes", x.Notes), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetFinishedProductsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<FinishedProductDto>>("/api/products/finished-products") ?? [];
        var styles = await GetProductStyleLookupsAsync();
        var models = await GetFinishedProductModelLookupsAsync();
        var brands = await GetFinishedProductBrandLookupsAsync();
        var colors = await GetOrangeSimpleLookupsAsync("/api/products/colors");
        var toeCaps = await GetOrangeSimpleLookupsAsync("/api/products/toe-caps");
        var soles = await GetOrangeSimpleLookupsAsync("/api/products/soles");
        var soleColors = await GetOrangeSimpleLookupsAsync("/api/products/sole-colors");
        var folioPatterns = await GetOrangeSimpleLookupsAsync("/api/products/folio-patterns");
        var runs = await GetProductSizeRunLookupsAsync();
        var lines = await GetProductLineLookupsAsync();
        var lasts = await GetProductLastLookupsAsync();
        var materials = await GetMaterialItemLookupsAsync();
        return BuildView("finished-products", "Productos terminados", "Catálogo base de productos terminados para ingeniería, ficha técnica y costeo.", "FinishedProductId", [
            TextColumn("FinishedProductId", "ID", allowEditing: false, width: 220),
            LookupColumn("ProductStyleId", "Estilo", styles, width: 180),
            LookupColumn("ItemModelId", "Modelo", models, width: 180),
            LookupColumn("ItemBrandId", "Marca", brands, width: 160),
            LookupColumn("ProductColorId", "Color", colors, width: 160),
            LookupColumn("ProductToeCapId", "Casco", toeCaps, width: 160),
            LookupColumn("ProductSoleId", "Suela", soles, width: 160),
            LookupColumn("ProductSoleColorId", "Color suela", soleColors, width: 160),
            LookupColumn("ProductFolioPatternId", "Foliado", folioPatterns, width: 160),
            LookupColumn("ProductSizeRunId", "Corrida", runs, width: 180),
            LookupColumn("ProductLineId", "Línea", lines, width: 160),
            LookupColumn("ProductLastId", "Horma", lasts, width: 160),
            LookupColumn("MainMaterialItemId", "Mat. principal", materials, width: 200),
            TextColumn("Code", "Clave", required: true, width: 120),
            TextColumn("Name", "Nombre", width: 260),
            TextColumn("BillingName", "Nombre facturación", width: 220),
            BoolColumn("HasPhoto", "Foto", width: 80),
            BoolColumn("HasConsumptionDefinition", "Consumos", width: 100),
            BoolColumn("HasMaterialAssignments", "Materiales", width: 100),
            BoolColumn("IsAuthorizedForExplosion", "Autorizado", width: 100),
            BoolColumn("IsActive", "Activo", width: 80)
        ], rows.Select(x => Row(
            ("FinishedProductId", x.FinishedProductId.ToString("D")),
            ("ProductStyleId", x.ProductStyleId?.ToString("D")),
            ("ItemModelId", x.ItemModelId?.ToString("D")),
            ("ItemBrandId", x.ItemBrandId?.ToString("D")),
            ("ProductColorId", x.ProductColorId?.ToString("D")),
            ("ProductToeCapId", x.ProductToeCapId?.ToString("D")),
            ("ProductSoleId", x.ProductSoleId?.ToString("D")),
            ("ProductSoleColorId", x.ProductSoleColorId?.ToString("D")),
            ("ProductFolioPatternId", x.ProductFolioPatternId?.ToString("D")),
            ("ProductSizeRunId", x.ProductSizeRunId?.ToString("D")),
            ("ProductLineId", x.ProductLineId?.ToString("D")),
            ("ProductLastId", x.ProductLastId?.ToString("D")),
            ("MainMaterialItemId", x.MainMaterialItemId?.ToString("D")),
            ("Code", x.Code),
            ("Name", x.Name),
            ("BillingName", x.BillingName),
            ("HasPhoto", x.HasPhoto),
            ("HasConsumptionDefinition", x.HasConsumptionDefinition),
            ("HasMaterialAssignments", x.HasMaterialAssignments),
            ("IsAuthorizedForExplosion", x.IsAuthorizedForExplosion),
            ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetProductComponentsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ProductComponentDto>>("/api/products/product-components") ?? [];
        var units = await GetUnitLookupsAsync();
        return BuildView("product-components", "Componentes del producto", "Catálogo universal de componentes por fase de producción y almacén entregará.", "ProductComponentId", [
            TextColumn("ProductComponentId", "ID", allowEditing: false, width: 220),
            LookupColumn("ConsumptionUnitId", "Consumption Unit", units, width: 160),
            TextColumn("Code", "Code", required: true, width: 120),
            TextColumn("Name", "Name", required: true, width: 200),
            TextColumn("ProductionPhase", "Phase", width: 120),
            TextColumn("WarehouseDeliveryRole", "Warehouse Delivery", width: 180),
            NumberColumn("DefaultConsumption", "Default Consumption", width: 130),
            BoolColumn("ActivateForAllProducts", "Activate for All", width: 120),
            BoolColumn("ShowOnProductionCard", "Show on Card", width: 110),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("ProductComponentId", x.ProductComponentId.ToString("D")), ("ConsumptionUnitId", x.ConsumptionUnitId?.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("ProductionPhase", x.ProductionPhase), ("WarehouseDeliveryRole", x.WarehouseDeliveryRole), ("DefaultConsumption", x.DefaultConsumption), ("ActivateForAllProducts", x.ActivateForAllProducts), ("ShowOnProductionCard", x.ShowOnProductionCard), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetFinishedProductMaterialsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<FinishedProductMaterialDto>>("/api/products/finished-product-materials") ?? [];
        var products = await GetFinishedProductLookupsAsync();
        var components = await GetProductComponentLookupsAsync();
        var materials = await GetMaterialItemLookupsAsync();
        return BuildView("finished-product-materials", "Materiales por producto", "Asignación de materiales por producto terminado, componente y talla.", "FinishedProductMaterialId", [
            TextColumn("FinishedProductMaterialId", "ID", allowEditing: false, width: 220),
            LookupColumn("FinishedProductId", "Finished Product", products, required: true, width: 220),
            LookupColumn("ProductComponentId", "Component", components, required: true, width: 180),
            LookupColumn("MaterialItemId", "Material", materials, required: true, width: 220),
            TextColumn("SizeCode", "Size Code", width: 100),
            NumberColumn("Quantity", "Quantity", width: 100),
            BoolColumn("IsRequired", "Required", width: 90),
            TextColumn("Notes", "Notes", width: 220),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("FinishedProductMaterialId", x.FinishedProductMaterialId.ToString("D")), ("FinishedProductId", x.FinishedProductId.ToString("D")), ("ProductComponentId", x.ProductComponentId.ToString("D")), ("MaterialItemId", x.MaterialItemId.ToString("D")), ("SizeCode", x.SizeCode), ("Quantity", x.Quantity), ("IsRequired", x.IsRequired), ("Notes", x.Notes), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<CatalogViewDefinition> GetProductConsumptionProfilesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<ProductConsumptionProfileDto>>("/api/products/product-consumption-profiles") ?? [];
        var products = await GetFinishedProductLookupsAsync();
        var components = await GetProductComponentLookupsAsync();
        return BuildView("product-consumption-profiles", "Consumos por producto", "Consumo por producto, componente y talla como base de ficha técnica y hoja de costo.", "ProductConsumptionProfileId", [
            TextColumn("ProductConsumptionProfileId", "ID", allowEditing: false, width: 220),
            LookupColumn("FinishedProductId", "Finished Product", products, required: true, width: 220),
            LookupColumn("ProductComponentId", "Component", components, required: true, width: 180),
            TextColumn("SizeCode", "Size Code", width: 100),
            NumberColumn("Pieces", "Pieces", width: 90),
            NumberColumn("Consumption", "Consumption", width: 110),
            TextColumn("Status", "Status", width: 120),
            TextColumn("Notes", "Notes", width: 220),
            BoolColumn("IsActive", "Active", width: 90)
        ], rows.Select(x => Row(("ProductConsumptionProfileId", x.ProductConsumptionProfileId.ToString("D")), ("FinishedProductId", x.FinishedProductId.ToString("D")), ("ProductComponentId", x.ProductComponentId.ToString("D")), ("SizeCode", x.SizeCode), ("Pieces", x.Pieces), ("Consumption", x.Consumption), ("Status", x.Status), ("Notes", x.Notes), ("IsActive", x.IsActive))).ToList());
    }

    private async Task<List<CatalogLookupItem>> GetMaterialFamilyLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<MaterialFamilyOptionDto>>("/api/products/material-families/options"))?.Select(x => new CatalogLookupItem { Id = x.MaterialFamilyId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetMaterialSubfamilyLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<MaterialSubfamilyOptionDto>>("/api/products/material-subfamilies/options"))?.Select(x => new CatalogLookupItem { Id = x.MaterialSubfamilyId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetMaterialItemLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<MaterialItemOptionDto>>("/api/products/material-items/options"))?.Select(x => new CatalogLookupItem { Id = x.MaterialItemId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetMaterialSupplierAssignmentLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<MaterialSupplierAssignmentOptionDto>>("/api/products/material-suppliers/options"))?.Select(x => new CatalogLookupItem { Id = x.MaterialSupplierAssignmentId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetFinishedProductLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<FinishedProductOptionDto>>("/api/products/finished-products/options"))?.Select(x => new CatalogLookupItem { Id = x.FinishedProductId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetProductComponentLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<ProductComponentOptionDto>>("/api/products/product-components/options"))?.Select(x => new CatalogLookupItem { Id = x.ProductComponentId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetSupplierLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<SupplierOptionDto>>("/api/third-parties/suppliers"))?.Select(x => new CatalogLookupItem { Id = x.SupplierId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];

    private async Task<List<CatalogLookupItem>> GetUnitLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<UnitOptionDto>>("/api/catalogs/units"))?.Select(x => new CatalogLookupItem { Id = x.UnitId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetCurrencyLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<CurrencyOptionDto>>("/api/catalogs/currencies"))?.Select(x => new CatalogLookupItem { Id = x.CurrencyId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetProductFamilyLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<ProductFamilyDto>>("/api/products/families"))?.Select(x => new CatalogLookupItem { Id = x.ProductFamilyId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetProductLastLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<ProductLastDto>>("/api/products/lasts"))?.Select(x => new CatalogLookupItem { Id = x.ProductLastId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetProductLineLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<ProductLineDto>>("/api/products/lines"))?.Select(x => new CatalogLookupItem { Id = x.ProductLineId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetProductStyleLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<ProductStyleDto>>("/api/products/styles"))?.Select(x => new CatalogLookupItem { Id = x.ProductStyleId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetProductSizeRunLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<ProductSizeRunDto>>("/api/products/size-runs"))?.Select(x => new CatalogLookupItem { Id = x.ProductSizeRunId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetEmbroideryPatternLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<EmbroideryPatternDto>>("/api/products/embroidery-patterns"))?.Select(x => new CatalogLookupItem { Id = x.EmbroideryPatternId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];
    private async Task<List<CatalogLookupItem>> GetItemLookupsAsync() => (await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<ItemOptionDto>>("/api/products/items/options"))?.Select(x => new CatalogLookupItem { Id = x.ItemId.ToString("D"), Name = $"{x.Code} - {x.Name}" }).ToList() ?? [];

    private async Task<List<CatalogLookupItem>> GetOrangeSimpleLookupsAsync(string apiRoute)
    {
        var rows = await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<OrangeSimpleCatalogDto>>(apiRoute) ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new CatalogLookupItem { Id = x.Id.ToString("D"), Name = $"{x.Code} - {x.Name}" })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetFinishedProductBrandLookupsAsync()
    {
        var rows = await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<FinishedProductBrandDto>>("/api/products/brands") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new CatalogLookupItem { Id = x.BrandId.ToString("D"), Name = $"{x.Code} - {x.Name}" })
            .ToList();
    }

    private async Task<List<CatalogLookupItem>> GetFinishedProductModelLookupsAsync()
    {
        var rows = await _httpClientFactory.CreateClient("Nanchesoft.Api").GetFromJsonAsync<List<FinishedProductModelDto>>("/api/products/models") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new CatalogLookupItem { Id = x.ModelId.ToString("D"), Name = $"{x.Code} - {x.Name}" })
            .ToList();
    }

    private static MaterialFamilyRequest MapMaterialFamilyRequest(JsonElement payload) => new() { Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), InventoryGroup = ReadString(payload, "InventoryGroup"), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static MaterialSubfamilyRequest MapMaterialSubfamilyRequest(JsonElement payload) => new() { MaterialFamilyId = ReadGuid(payload, "MaterialFamilyId"), Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), MaterialType = ReadString(payload, "MaterialType"), IsDirectMaterial = ReadBool(payload, "IsDirectMaterial", true), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static MaterialItemRequest MapMaterialItemRequest(JsonElement payload) => new() { MaterialSubfamilyId = ReadGuid(payload, "MaterialSubfamilyId"), PurchaseUnitId = ReadNullableGuid(payload, "PurchaseUnitId"), IssueUnitId = ReadNullableGuid(payload, "IssueUnitId"), SupplierId = ReadNullableGuid(payload, "SupplierId"), Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), Description = ReadString(payload, "Description"), LegacyMaterialName = ReadString(payload, "LegacyMaterialName"), AuthorizedCost = ReadDecimal(payload, "AuthorizedCost"), LastPurchaseCost = ReadDecimal(payload, "LastPurchaseCost"), StandardCost = ReadDecimal(payload, "StandardCost"), CostStatus = ReadString(payload, "CostStatus"), IsServiceItem = ReadBool(payload, "IsServiceItem"), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static MaterialSupplierAssignmentRequest MapMaterialSupplierRequest(JsonElement payload) => new() { MaterialItemId = ReadGuid(payload, "MaterialItemId"), SupplierId = ReadGuid(payload, "SupplierId"), PurchaseUnitId = ReadNullableGuid(payload, "PurchaseUnitId"), CurrencyId = ReadNullableGuid(payload, "CurrencyId"), SupplierItemCode = ReadString(payload, "SupplierItemCode"), SupplierItemName = ReadString(payload, "SupplierItemName"), ConversionFactor = ReadDecimal(payload, "ConversionFactor", 1m), AuthorizedCost = ReadDecimal(payload, "AuthorizedCost"), LastCost = ReadDecimal(payload, "LastCost"), LeadTimeDays = ReadInt(payload, "LeadTimeDays"), MinimumOrderQuantity = ReadDecimal(payload, "MinimumOrderQuantity"), IsPreferred = ReadBool(payload, "IsPreferred"), ValidFrom = ReadNullableDate(payload, "ValidFrom"), ValidTo = ReadNullableDate(payload, "ValidTo"), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static MaterialSupplierCostHistoryRequest MapMaterialSupplierCostHistoryRequest(JsonElement payload) => new() { MaterialSupplierAssignmentId = ReadGuid(payload, "MaterialSupplierAssignmentId"), CurrencyId = ReadNullableGuid(payload, "CurrencyId"), CostDate = ReadDate(payload, "CostDate"), Cost = ReadDecimal(payload, "Cost"), ExchangeRate = ReadDecimal(payload, "ExchangeRate", 1m), SourceDocumentType = ReadString(payload, "SourceDocumentType"), SourceDocumentId = ReadNullableGuid(payload, "SourceDocumentId"), SourceDocumentNumber = ReadString(payload, "SourceDocumentNumber"), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static FinishedProductRequest MapFinishedProductRequest(JsonElement payload) => new() { ProductStyleId = ReadNullableGuid(payload, "ProductStyleId"), ItemModelId = ReadNullableGuid(payload, "ItemModelId"), ItemBrandId = ReadNullableGuid(payload, "ItemBrandId"), ProductColorId = ReadNullableGuid(payload, "ProductColorId"), ProductToeCapId = ReadNullableGuid(payload, "ProductToeCapId"), ProductSoleId = ReadNullableGuid(payload, "ProductSoleId"), ProductSoleColorId = ReadNullableGuid(payload, "ProductSoleColorId"), ProductFolioPatternId = ReadNullableGuid(payload, "ProductFolioPatternId"), ProductSizeRunId = ReadNullableGuid(payload, "ProductSizeRunId"), ProductLineId = ReadNullableGuid(payload, "ProductLineId"), ProductLastId = ReadNullableGuid(payload, "ProductLastId"), MainMaterialItemId = ReadNullableGuid(payload, "MainMaterialItemId"), Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), BillingName = ReadString(payload, "BillingName"), HasPhoto = ReadBool(payload, "HasPhoto"), HasConsumptionDefinition = ReadBool(payload, "HasConsumptionDefinition"), HasMaterialAssignments = ReadBool(payload, "HasMaterialAssignments"), IsAuthorizedForExplosion = ReadBool(payload, "IsAuthorizedForExplosion"), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static ProductComponentRequest MapProductComponentRequest(JsonElement payload) => new() { ConsumptionUnitId = ReadNullableGuid(payload, "ConsumptionUnitId"), Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), ProductionPhase = ReadString(payload, "ProductionPhase"), WarehouseDeliveryRole = ReadString(payload, "WarehouseDeliveryRole"), DefaultConsumption = ReadDecimal(payload, "DefaultConsumption"), ActivateForAllProducts = ReadBool(payload, "ActivateForAllProducts"), ShowOnProductionCard = ReadBool(payload, "ShowOnProductionCard"), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static FinishedProductMaterialRequest MapFinishedProductMaterialRequest(JsonElement payload) => new() { FinishedProductId = ReadGuid(payload, "FinishedProductId"), ProductComponentId = ReadGuid(payload, "ProductComponentId"), MaterialItemId = ReadGuid(payload, "MaterialItemId"), SizeCode = ReadString(payload, "SizeCode"), Quantity = ReadDecimal(payload, "Quantity"), IsRequired = ReadBool(payload, "IsRequired", true), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static ProductConsumptionProfileRequest MapProductConsumptionProfileRequest(JsonElement payload) => new() { FinishedProductId = ReadGuid(payload, "FinishedProductId"), ProductComponentId = ReadGuid(payload, "ProductComponentId"), SizeCode = ReadString(payload, "SizeCode"), Pieces = ReadInt(payload, "Pieces"), Consumption = ReadDecimal(payload, "Consumption"), Status = ReadString(payload, "Status"), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };

    private static UnitConversionRequest MapUnitConversionRequest(JsonElement payload) => new() { FromUnitId = ReadGuid(payload, "FromUnitId"), ToUnitId = ReadGuid(payload, "ToUnitId"), ConversionFactor = ReadDecimal(payload, "ConversionFactor"), IsBidirectional = ReadBool(payload, "IsBidirectional"), Notes = ReadString(payload, "Notes"), IsActive = ReadBool(payload, "IsActive", true) };
    private static ProductSizeRunRequest MapSizeRunRequest(JsonElement payload) => new() { Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), DisplayName = ReadString(payload, "DisplayName"), IsUniqueSizeRun = ReadBool(payload, "IsUniqueSizeRun"), SizeCount = ReadInt(payload, "SizeCount"), SizeDefinitions = ReadString(payload, "SizeDefinitions"), IsActive = ReadBool(payload, "IsActive", true) };
    private static ProductFamilyRequest MapFamilyRequest(JsonElement payload) => new() { Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), StatisticsGroup = ReadString(payload, "StatisticsGroup"), IsFinishedProductFamily = ReadBool(payload, "IsFinishedProductFamily", true), IsActive = ReadBool(payload, "IsActive", true) };
    private static ProductLastRequest MapLastRequest(JsonElement payload) => new() { Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), WidthReference = ReadString(payload, "WidthReference"), IsActive = ReadBool(payload, "IsActive", true) };
    private static ProductLineRequest MapLineRequest(JsonElement payload) => new() { ProductFamilyId = ReadNullableGuid(payload, "ProductFamilyId"), ProductLastId = ReadNullableGuid(payload, "ProductLastId"), Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), ShortName = ReadString(payload, "ShortName"), AllowsDiscount = ReadBool(payload, "AllowsDiscount"), IsActive = ReadBool(payload, "IsActive", true) };
    private static ProductStyleRequest MapStyleRequest(JsonElement payload) => new() { ProductLineId = ReadNullableGuid(payload, "ProductLineId"), ProductLastId = ReadNullableGuid(payload, "ProductLastId"), Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), CustomerLabel1 = ReadString(payload, "CustomerLabel1"), CustomerLabel2 = ReadString(payload, "CustomerLabel2"), ColorLabel = ReadString(payload, "ColorLabel"), DieCutReference = ReadString(payload, "DieCutReference"), MaxLotSize = ReadDecimal(payload, "MaxLotSize"), HasAuthorizedConsumption = ReadBool(payload, "HasAuthorizedConsumption"), HandlesFractionsByStyle = ReadBool(payload, "HandlesFractionsByStyle"), TechnicalNotes = ReadString(payload, "TechnicalNotes"), ProductionCardNotes = ReadString(payload, "ProductionCardNotes"), OutsourcedProcessName = ReadString(payload, "OutsourcedProcessName"), PhotoUrl = ReadString(payload, "PhotoUrl"), IsActive = ReadBool(payload, "IsActive", true) };
    private static EmbroideryPatternRequest MapEmbroideryPatternRequest(JsonElement payload) => new() { Code = ReadString(payload, "Code"), Name = ReadString(payload, "Name"), Sequence = ReadInt(payload, "Sequence"), IsActive = ReadBool(payload, "IsActive", true) };
    private static ItemEngineeringProfileRequest MapEngineeringProfileRequest(JsonElement payload) => new() { ItemId = ReadGuid(payload, "ItemId"), ProductStyleId = ReadNullableGuid(payload, "ProductStyleId"), ProductSizeRunId = ReadNullableGuid(payload, "ProductSizeRunId"), EmbroideryPatternId = ReadNullableGuid(payload, "EmbroideryPatternId"), PrimaryMaterialItemId = ReadNullableGuid(payload, "PrimaryMaterialItemId"), FolioPattern = ReadString(payload, "FolioPattern"), TechnicalSheetMode = ReadString(payload, "TechnicalSheetMode", "style"), ProcessVoucherProfile = ReadString(payload, "ProcessVoucherProfile"), TechnicalSheetNotes = ReadString(payload, "TechnicalSheetNotes"), ProductionCardNotes = ReadString(payload, "ProductionCardNotes"), HasPhoto = ReadBool(payload, "HasPhoto"), HasConsumptionDefinition = ReadBool(payload, "HasConsumptionDefinition"), HasMaterialAssignments = ReadBool(payload, "HasMaterialAssignments"), IsAuthorizedForExplosion = ReadBool(payload, "IsAuthorizedForExplosion"), IsActive = ReadBool(payload, "IsActive", true) };

    private static CatalogViewDefinition BuildView(string catalogKey, string title, string subtitle, string keyExpr, List<CatalogColumnDefinition> columns, List<Dictionary<string, object?>> rows)
    {
        foreach (var column in columns)
        {
            column.Caption = TranslateUiText(column.Caption);
        }

        return new()
        {
            CatalogKey = catalogKey,
            Title = TranslateUiText(title),
            Subtitle = TranslateUiText(subtitle),
            KeyExpr = keyExpr,
            AllowCreate = true,
            AllowUpdate = true,
            AllowDelete = true,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => x.TryGetValue("IsActive", out var value) && (value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false)),
            InactiveCount = rows.Count(x => x.TryGetValue("IsActive", out var value) && !(value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false)),
            Columns = columns,
            Rows = rows
        };
    }


    private async Task<CatalogViewDefinition> GetSimpleOrangeCatalogAsync(string catalogKey, string apiRoute, string title, string subtitle)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<OrangeSimpleCatalogDto>>(apiRoute) ?? [];
        return BuildView(catalogKey, title, subtitle, "Id", [
            TextColumn("Id", "ID", allowEditing: false, width: 220),
            TextColumn("Code", "Clave", required: true, width: 120),
            TextColumn("Name", "Nombre", required: true, width: 220),
            TextColumn("Description", "Descripción", width: 260),
            NumberColumn("Sequence", "Orden", width: 90),
            BoolColumn("IsActive", "Activo", width: 90)
        ], rows.Select(x => Row(("Id", x.Id.ToString("D")), ("Code", x.Code), ("Name", x.Name), ("Description", x.Description), ("Sequence", x.Sequence), ("IsActive", x.IsActive))).ToList());
    }

    private static CatalogColumnDefinition TextColumn(string dataField, string caption, bool required = false, bool allowEditing = true, int width = 150) => new() { DataField = dataField, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Width = width };
    private static CatalogColumnDefinition NumberColumn(string dataField, string caption, bool required = false, int width = 110) => new() { DataField = dataField, Caption = caption, DataType = "number", Required = required, Width = width };
    private static CatalogColumnDefinition BoolColumn(string dataField, string caption, int width = 90) => new() { DataField = dataField, Caption = caption, DataType = "boolean", Width = width };
    private static CatalogColumnDefinition DateColumn(string dataField, string caption, bool required = false, int width = 120) => new() { DataField = dataField, Caption = caption, DataType = "date", Required = required, Width = width };
    private static CatalogColumnDefinition LookupColumn(string dataField, string caption, List<CatalogLookupItem> lookup, bool required = false, int width = 160) => new() { DataField = dataField, Caption = caption, DataType = "string", UseLookup = true, LookupItems = lookup, Required = required, Width = width };

    private static string TranslateUiText(string value)
        => value switch
        {
            "Unit Conversions" => "Conversiones de unidad",
            "Legacy Orange uses explicit unit conversions for purchasing, inventory and explosion." => "Orange usa conversiones explícitas de unidad para compras, inventario y explosión de materiales.",
            "Size Runs" => "Corridas",
            "Standardized version of legacy Corrida, including unique size run U." => "Versión estandarizada de la corrida de Orange, incluyendo la corrida única U.",
            "Product Families" => "Familias de productos",
            "Legacy Orange family classification translated to standardized English product families." => "Clasificación de familias de Orange adaptada al catálogo estándar del ERP.",
            "Product Lasts" => "Hormas",
            "Standardized catalog for legacy Horma." => "Catálogo estandarizado para la horma del sistema Orange.",
            "Product Lines" => "Líneas de producto",
            "Legacy Linea aligned to family and last." => "Catálogo de líneas ligado a familia y horma.",
            "Product Styles" => "Estilos",
            "Legacy Estilo translated to standardized product style catalog." => "Catálogo de estilos migrado desde Orange al esquema estándar del ERP.",
            "Embroidery Patterns" => "Bordados",
            "Standardized catalog for legacy Bordado." => "Catálogo estandarizado para los bordados del sistema Orange.",
            "Item Engineering Profiles" => "Perfiles de ingeniería",
            "Bridge between item master and Orange engineering attributes." => "Puente entre el maestro de artículos y los atributos de ingeniería provenientes de Orange.",
            "Material Families" => "Familias de materiales",
            "Orange material families translated to standardized English catalogs." => "Familias de materiales de Orange adaptadas al catálogo estándar del ERP.",
            "Material Subfamilies" => "Subfamilias de materiales",
            "Legacy subfamilies used for direct and indirect material grouping." => "Subfamilias heredadas para agrupar materiales directos e indirectos.",
            "Material Items" => "Catálogo de materiales",
            "Orange materials with authorized cost, supplier and purchase/issue units." => "Materiales de Orange con costo autorizado, proveedor y unidades de compra y salida.",
            "Code" => "Código",
            "Name" => "Nombre",
            "Display Name" => "Nombre visible",
            "Unique" => "Única",
            "Sizes" => "Tallas",
            "Sizes CSV" => "Tallas CSV",
            "Preview" => "Vista previa",
            "Statistics Group" => "Grupo estadístico",
            "Finished Goods" => "Producto terminado",
            "Width Reference" => "Referencia ancho",
            "Family" => "Familia",
            "Last" => "Horma",
            "Short Name" => "Nombre corto",
            "Allows Discount" => "Permite descuento",
            "Line" => "Línea",
            "Customer Label 1" => "Etiqueta cliente 1",
            "Customer Label 2" => "Etiqueta cliente 2",
            "Color Label" => "Etiqueta color",
            "Die Cut" => "Troquel",
            "Max Lot" => "Lote máx.",
            "Authorized Consumption" => "Consumo autorizado",
            "Fractions by Style" => "Fracciones por estilo",
            "Outsourced Process" => "Proceso maquila",
            "Photo Url" => "URL foto",
            "Sequence" => "Secuencia",
            "Item" => "Artículo",
            "Style" => "Estilo",
            "Size Run" => "Corrida",
            "Embroidery" => "Bordado",
            "Primary Material" => "Material principal",
            "Folio Pattern" => "Foliado",
            "Technical Sheet Mode" => "Modo ficha técnica",
            "Voucher Profile" => "Perfil de vale",
            "Has Photo" => "Tiene foto",
            "Has Consumption" => "Tiene consumo",
            "Has Material Assignments" => "Tiene materiales",
            "Authorized" => "Autorizado",
            "Inventory Group" => "Grupo inventario",
            "Material Family" => "Familia material",
            "Type" => "Tipo",
            "Direct" => "Directo",
            "Subfamily" => "Subfamilia",
            "Purchase Unit" => "Unidad compra",
            "Issue Unit" => "Unidad salida",
            "Supplier" => "Proveedor",
            "Legacy Name" => "Nombre legado",
            "Authorized Cost" => "Costo autorizado",
            "Last Cost" => "Último costo",
            "Standard Cost" => "Costo estándar",
            "Cost Status" => "Estatus costo",
            "Service" => "Servicio",
            "Finished Product" => "Producto terminado",
            "Billing Name" => "Nombre facturación",
            "Consumption Unit" => "Unidad consumo",
            "Phase" => "Fase",
            "Warehouse Delivery" => "Almacén entregará",
            "Default Consumption" => "Consumo base",
            "Activate for All" => "Activar para todos",
            "Show on Card" => "Ver en tarjeta",
            "Component" => "Componente",
            "Material" => "Material",
            "Size Code" => "Código talla",
            "Quantity" => "Cantidad",
            "Required" => "Obligatorio",
            "Pieces" => "Piezas",
            "Consumption" => "Consumo",
            "Status" => "Estatus",
            "Notes" => "Notas",
            "Active" => "Activo",
            "Lead time" => "Días entrega",
            _ => value
        };

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values) => values.ToDictionary(x => x.Key, x => x.Value);
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? $"Error HTTP {(int)response.StatusCode}" : body);
    }

    private static OrangeSimpleCatalogRequest MapSimpleOrangeCatalogRequest(JsonElement payload) => new()
    {
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Description = ReadString(payload, "Description"),
        Sequence = ReadInt(payload, "Sequence"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static string ReadString(JsonElement payload, string name, string fallback = "") => payload.TryGetProperty(name, out var value) ? value.ValueKind switch { JsonValueKind.String => value.GetString() ?? fallback, JsonValueKind.Number => value.ToString(), JsonValueKind.True => "true", JsonValueKind.False => "false", _ => fallback } : fallback;
    private static Guid ReadGuid(JsonElement payload, string name) => ReadNullableGuid(payload, name) ?? Guid.Empty;
    private static Guid? ReadNullableGuid(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;
        if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var result)) return result;
        return null;
    }
    private static int ReadInt(JsonElement payload, string name, int fallback = 0) => payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) ? result : fallback;
    private static decimal ReadDecimal(JsonElement payload, string name, decimal fallback = 0m) => payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var result) ? result : fallback;
    private static DateTime ReadDate(JsonElement payload, string name, DateTime? fallback = null)
    {
        if (payload.TryGetProperty(name, out var value))
        {
            if (value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var parsed)) return parsed;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unix)) return DateTimeOffset.FromUnixTimeMilliseconds(unix).UtcDateTime;
        }
        return fallback ?? DateTime.UtcNow.Date;
    }
    private static DateTime? ReadNullableDate(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
        if (value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var parsed)) return parsed;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unix)) return DateTimeOffset.FromUnixTimeMilliseconds(unix).UtcDateTime;
        return null;
    }
    private static bool ReadBool(JsonElement payload, string name, bool fallback = false) => payload.TryGetProperty(name, out var value) ? value.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.String when bool.TryParse(value.GetString(), out var result) => result, _ => fallback } : fallback;
}

public sealed class UnitOptionDto { public Guid UnitId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class CurrencyOptionDto { public Guid CurrencyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class ItemOptionDto { public Guid ItemId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public class UnitConversionRequest { public Guid FromUnitId { get; set; } public Guid ToUnitId { get; set; } public decimal ConversionFactor { get; set; } public bool IsBidirectional { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class UnitConversionDto : UnitConversionRequest { public Guid UnitConversionId { get; set; } public Guid CompanyId { get; set; } public string FromUnitCode { get; set; } = string.Empty; public string ToUnitCode { get; set; } = string.Empty; }
public class ProductSizeRunRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public bool IsUniqueSizeRun { get; set; } public int SizeCount { get; set; } public string SizeDefinitions { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductSizeRunDto : ProductSizeRunRequest { public Guid ProductSizeRunId { get; set; } public Guid CompanyId { get; set; } public string SizesPreview { get; set; } = string.Empty; }
public class ProductFamilyRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string StatisticsGroup { get; set; } = string.Empty; public bool IsFinishedProductFamily { get; set; } = true; public bool IsActive { get; set; } = true; }
public sealed class ProductFamilyDto : ProductFamilyRequest { public Guid ProductFamilyId { get; set; } public Guid CompanyId { get; set; } }
public class ProductLastRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string WidthReference { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductLastDto : ProductLastRequest { public Guid ProductLastId { get; set; } public Guid CompanyId { get; set; } }
public class ProductLineRequest { public Guid? ProductFamilyId { get; set; } public Guid? ProductLastId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string ShortName { get; set; } = string.Empty; public bool AllowsDiscount { get; set; } public bool IsActive { get; set; } = true; }
public sealed class ProductLineDto : ProductLineRequest { public Guid ProductLineId { get; set; } public Guid CompanyId { get; set; } public string ProductFamilyName { get; set; } = string.Empty; public string ProductLastName { get; set; } = string.Empty; }
public class ProductStyleRequest { public Guid? ProductLineId { get; set; } public Guid? ProductLastId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string CustomerLabel1 { get; set; } = string.Empty; public string CustomerLabel2 { get; set; } = string.Empty; public string ColorLabel { get; set; } = string.Empty; public string DieCutReference { get; set; } = string.Empty; public decimal MaxLotSize { get; set; } public bool HasAuthorizedConsumption { get; set; } public bool HandlesFractionsByStyle { get; set; } public string TechnicalNotes { get; set; } = string.Empty; public string ProductionCardNotes { get; set; } = string.Empty; public string OutsourcedProcessName { get; set; } = string.Empty; public string PhotoUrl { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductStyleDto : ProductStyleRequest { public Guid ProductStyleId { get; set; } public Guid CompanyId { get; set; } public string ProductLineName { get; set; } = string.Empty; public string ProductLastName { get; set; } = string.Empty; }
public class EmbroideryPatternRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public int Sequence { get; set; } public bool IsActive { get; set; } = true; }
public sealed class EmbroideryPatternDto : EmbroideryPatternRequest { public Guid EmbroideryPatternId { get; set; } public Guid CompanyId { get; set; } }
public class ItemEngineeringProfileRequest { public Guid ItemId { get; set; } public Guid? ProductStyleId { get; set; } public Guid? ProductSizeRunId { get; set; } public Guid? EmbroideryPatternId { get; set; } public Guid? PrimaryMaterialItemId { get; set; } public string FolioPattern { get; set; } = string.Empty; public string TechnicalSheetMode { get; set; } = "style"; public string ProcessVoucherProfile { get; set; } = string.Empty; public string TechnicalSheetNotes { get; set; } = string.Empty; public string ProductionCardNotes { get; set; } = string.Empty; public bool HasPhoto { get; set; } public bool HasConsumptionDefinition { get; set; } public bool HasMaterialAssignments { get; set; } public bool IsAuthorizedForExplosion { get; set; } public bool IsActive { get; set; } = true; }
public sealed class ItemEngineeringProfileDto : ItemEngineeringProfileRequest { public Guid ItemEngineeringProfileId { get; set; } public Guid CompanyId { get; set; } public string ItemCode { get; set; } = string.Empty; public string ItemName { get; set; } = string.Empty; public string ProductStyleName { get; set; } = string.Empty; public string ProductSizeRunName { get; set; } = string.Empty; public string EmbroideryPatternName { get; set; } = string.Empty; public string PrimaryMaterialItemName { get; set; } = string.Empty; }

public sealed class MaterialSupplierAssignmentOptionDto { public Guid MaterialSupplierAssignmentId { get; set; } public Guid MaterialItemId { get; set; } public Guid SupplierId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public class MaterialSupplierAssignmentRequest { public Guid MaterialItemId { get; set; } public Guid SupplierId { get; set; } public Guid? PurchaseUnitId { get; set; } public Guid? CurrencyId { get; set; } public string SupplierItemCode { get; set; } = string.Empty; public string SupplierItemName { get; set; } = string.Empty; public decimal ConversionFactor { get; set; } = 1m; public decimal AuthorizedCost { get; set; } public decimal LastCost { get; set; } public int LeadTimeDays { get; set; } public decimal MinimumOrderQuantity { get; set; } public bool IsPreferred { get; set; } public DateTime? ValidFrom { get; set; } public DateTime? ValidTo { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialSupplierAssignmentDto : MaterialSupplierAssignmentRequest { public Guid MaterialSupplierAssignmentId { get; set; } public Guid CompanyId { get; set; } public string MaterialItemCode { get; set; } = string.Empty; public string MaterialItemName { get; set; } = string.Empty; public string SupplierCode { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; public string PurchaseUnitName { get; set; } = string.Empty; public string CurrencyCode { get; set; } = string.Empty; }
public class MaterialSupplierCostHistoryRequest { public Guid MaterialSupplierAssignmentId { get; set; } public Guid? CurrencyId { get; set; } public DateTime CostDate { get; set; } = DateTime.UtcNow.Date; public decimal Cost { get; set; } public decimal ExchangeRate { get; set; } = 1m; public string SourceDocumentType { get; set; } = string.Empty; public Guid? SourceDocumentId { get; set; } public string SourceDocumentNumber { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialSupplierCostHistoryDto : MaterialSupplierCostHistoryRequest { public Guid MaterialSupplierCostHistoryId { get; set; } public Guid CompanyId { get; set; } public Guid MaterialItemId { get; set; } public string MaterialItemCode { get; set; } = string.Empty; public string MaterialItemName { get; set; } = string.Empty; public Guid SupplierId { get; set; } public string SupplierCode { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; public string CurrencyCode { get; set; } = string.Empty; }

public sealed class FinishedProductBrandDto { public Guid BrandId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
public sealed class FinishedProductModelDto { public Guid ModelId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
public sealed class SupplierOptionDto { public Guid SupplierId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class MaterialFamilyOptionDto { public Guid MaterialFamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class MaterialSubfamilyOptionDto { public Guid MaterialSubfamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class MaterialItemOptionDto { public Guid MaterialItemId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class FinishedProductOptionDto { public Guid FinishedProductId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class ProductComponentOptionDto { public Guid ProductComponentId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public class MaterialFamilyRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string InventoryGroup { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialFamilyDto : MaterialFamilyRequest { public Guid MaterialFamilyId { get; set; } public Guid CompanyId { get; set; } }
public class MaterialSubfamilyRequest { public Guid MaterialFamilyId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string MaterialType { get; set; } = string.Empty; public bool IsDirectMaterial { get; set; } = true; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialSubfamilyDto : MaterialSubfamilyRequest { public Guid MaterialSubfamilyId { get; set; } public Guid CompanyId { get; set; } public string MaterialFamilyName { get; set; } = string.Empty; }
public class MaterialItemRequest { public Guid MaterialSubfamilyId { get; set; } public Guid? PurchaseUnitId { get; set; } public Guid? IssueUnitId { get; set; } public Guid? SupplierId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string LegacyMaterialName { get; set; } = string.Empty; public decimal AuthorizedCost { get; set; } public decimal LastPurchaseCost { get; set; } public decimal StandardCost { get; set; } public string CostStatus { get; set; } = string.Empty; public bool IsServiceItem { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class MaterialItemDto : MaterialItemRequest { public Guid MaterialItemId { get; set; } public Guid CompanyId { get; set; } public string MaterialSubfamilyName { get; set; } = string.Empty; public string PurchaseUnitName { get; set; } = string.Empty; public string IssueUnitName { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; }
public class FinishedProductRequest { public Guid? ProductStyleId { get; set; } public Guid? ItemModelId { get; set; } public Guid? ItemBrandId { get; set; } public Guid? ProductLeatherTypeId { get; set; } public Guid? ProductColorId { get; set; } public Guid? ProductToeCapId { get; set; } public Guid? ProductSoleId { get; set; } public Guid? ProductSoleColorId { get; set; } public Guid? ProductFolioPatternId { get; set; } public Guid? ProductSizeRunId { get; set; } public Guid? ProductLineId { get; set; } public Guid? ProductLastId { get; set; } public Guid? MainMaterialItemId { get; set; } public string Code { get; set; } = string.Empty; public string? Name { get; set; } public string BillingName { get; set; } = string.Empty; public bool HasPhoto { get; set; } public bool HasConsumptionDefinition { get; set; } public bool HasMaterialAssignments { get; set; } public bool IsAuthorizedForExplosion { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class FinishedProductDto : FinishedProductRequest { public Guid FinishedProductId { get; set; } public Guid CompanyId { get; set; } public string ProductStyleName { get; set; } = string.Empty; public string ItemModelName { get; set; } = string.Empty; public string ItemBrandName { get; set; } = string.Empty; public string ProductLeatherTypeName { get; set; } = string.Empty; public string ProductColorName { get; set; } = string.Empty; public string ProductToeCapName { get; set; } = string.Empty; public string ProductSoleName { get; set; } = string.Empty; public string ProductSoleColorName { get; set; } = string.Empty; public string ProductFolioPatternName { get; set; } = string.Empty; public string ProductSizeRunName { get; set; } = string.Empty; public string MainMaterialItemName { get; set; } = string.Empty; }
public class ProductComponentRequest { public Guid? ConsumptionUnitId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string ProductionPhase { get; set; } = string.Empty; public string WarehouseDeliveryRole { get; set; } = string.Empty; public decimal DefaultConsumption { get; set; } public bool ActivateForAllProducts { get; set; } public bool ShowOnProductionCard { get; set; } public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductComponentDto : ProductComponentRequest { public Guid ProductComponentId { get; set; } public Guid CompanyId { get; set; } public string ConsumptionUnitName { get; set; } = string.Empty; }
public class FinishedProductMaterialRequest { public Guid FinishedProductId { get; set; } public Guid ProductComponentId { get; set; } public Guid MaterialItemId { get; set; } public string SizeCode { get; set; } = string.Empty; public decimal Quantity { get; set; } public bool IsRequired { get; set; } = true; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class FinishedProductMaterialDto : FinishedProductMaterialRequest { public Guid FinishedProductMaterialId { get; set; } public Guid CompanyId { get; set; } public string FinishedProductName { get; set; } = string.Empty; public string ProductComponentName { get; set; } = string.Empty; public string MaterialItemName { get; set; } = string.Empty; }
public class ProductConsumptionProfileRequest { public Guid FinishedProductId { get; set; } public Guid ProductComponentId { get; set; } public string SizeCode { get; set; } = string.Empty; public int Pieces { get; set; } public decimal Consumption { get; set; } public string Status { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public sealed class ProductConsumptionProfileDto : ProductConsumptionProfileRequest { public Guid ProductConsumptionProfileId { get; set; } public Guid CompanyId { get; set; } public string FinishedProductName { get; set; } = string.Empty; public string ProductComponentName { get; set; } = string.Empty; }

public sealed class OrangeSimpleCatalogDto : OrangeSimpleCatalogRequest { public Guid Id { get; set; } public Guid TenantId { get; set; } public Guid CompanyId { get; set; } public DateTime CreatedAt { get; set; } public DateTime? UpdatedAt { get; set; } }
public class OrangeSimpleCatalogRequest { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public int Sequence { get; set; } public bool IsActive { get; set; } = true; }
