using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class MasterCatalogEndpoints
{
    public static IEndpointRouteBuilder MapMasterCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var currencies = app.MapGroup("/api/catalogs/currencies").WithTags("Currencies");
        currencies.MapGet("/", GetCurrenciesAsync);
        currencies.MapPost("/", CreateCurrencyAsync);
        currencies.MapPut("/{id:guid}", UpdateCurrencyAsync);
        currencies.MapDelete("/{id:guid}", DeleteCurrencyAsync);

        var exchangeRates = app.MapGroup("/api/catalogs/exchange-rates").WithTags("ExchangeRates");
        exchangeRates.MapGet("/", GetExchangeRatesAsync);
        exchangeRates.MapPost("/", CreateExchangeRateAsync);
        exchangeRates.MapPut("/{id:guid}", UpdateExchangeRateAsync);
        exchangeRates.MapDelete("/{id:guid}", DeleteExchangeRateAsync);

        var taxes = app.MapGroup("/api/catalogs/taxes").WithTags("Taxes");
        taxes.MapGet("/", GetTaxesAsync);
        taxes.MapPost("/", CreateTaxAsync);
        taxes.MapPut("/{id:guid}", UpdateTaxAsync);
        taxes.MapDelete("/{id:guid}", DeleteTaxAsync);

        var units = app.MapGroup("/api/catalogs/units").WithTags("Units");
        units.MapGet("/", GetUnitsAsync);
        units.MapPost("/", CreateUnitAsync);
        units.MapPut("/{id:guid}", UpdateUnitAsync);
        units.MapDelete("/{id:guid}", DeleteUnitAsync);

        var banks = app.MapGroup("/api/catalogs/banks").WithTags("Banks");
        banks.MapGet("/", GetBanksAsync);
        banks.MapPost("/", CreateBankAsync);
        banks.MapPut("/{id:guid}", UpdateBankAsync);
        banks.MapDelete("/{id:guid}", DeleteBankAsync);

        var countries = app.MapGroup("/api/catalogs/countries").WithTags("Countries");
        countries.MapGet("/", GetCountriesAsync);
        countries.MapPost("/", CreateCountryAsync);
        countries.MapPut("/{id:guid}", UpdateCountryAsync);
        countries.MapDelete("/{id:guid}", DeleteCountryAsync);

        var states = app.MapGroup("/api/catalogs/states").WithTags("States");
        states.MapGet("/", GetStatesAsync);
        states.MapPost("/", CreateStateAsync);
        states.MapPut("/{id:guid}", UpdateStateAsync);
        states.MapDelete("/{id:guid}", DeleteStateAsync);

        var cities = app.MapGroup("/api/catalogs/cities").WithTags("Cities");
        cities.MapGet("/", GetCitiesAsync);
        cities.MapPost("/", CreateCityAsync);
        cities.MapPut("/{id:guid}", UpdateCityAsync);
        cities.MapDelete("/{id:guid}", DeleteCityAsync);

        var documentSeries = app.MapGroup("/api/administration/document-series").WithTags("DocumentSeries");
        documentSeries.MapGet("/", GetDocumentSeriesAsync);
        documentSeries.MapPost("/", CreateDocumentSeriesAsync);
        documentSeries.MapPut("/{id:guid}", UpdateDocumentSeriesAsync);
        documentSeries.MapDelete("/{id:guid}", DeleteDocumentSeriesAsync);

        var documentFolios = app.MapGroup("/api/administration/document-folios").WithTags("DocumentFolios");
        documentFolios.MapGet("/", GetDocumentFoliosAsync);
        documentFolios.MapPost("/", CreateDocumentFolioAsync);
        documentFolios.MapPut("/{id:guid}", UpdateDocumentFolioAsync);
        documentFolios.MapDelete("/{id:guid}", DeleteDocumentFolioAsync);

        var companySettings = app.MapGroup("/api/administration/company-settings").WithTags("CompanySettings");
        companySettings.MapGet("/", GetCompanySettingsAsync);
        companySettings.MapPost("/", CreateCompanySettingAsync);
        companySettings.MapPut("/{id:guid}", UpdateCompanySettingAsync);
        companySettings.MapDelete("/{id:guid}", DeleteCompanySettingAsync);

        return app;
    }

    private static async Task<Guid?> ResolveDefaultTenantIdAsync(NanchesoftDbContext db)
    {
        return await db.Tenants
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    private static async Task<IResult> GetCurrenciesAsync(NanchesoftDbContext db)
    {
        var rows = await db.Currencies
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .Select(x => new CurrencyDto
            {
                CurrencyId = x.Id,
                TenantId = x.TenantId,
                Code = x.Code,
                Name = x.Name,
                Symbol = x.Symbol,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCurrencyAsync(CurrencyRequest request, NanchesoftDbContext db)
    {
        var tenantId = request.TenantId ?? await ResolveDefaultTenantIdAsync(db);
        if (!tenantId.HasValue)
            return Results.BadRequest(new { message = "No existe un tenant disponible para la moneda." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        var symbol = NormalizeText(request.Symbol);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(symbol))
            return Results.BadRequest(new { message = "Código, nombre y símbolo son obligatorios." });

        if (await db.Currencies.AnyAsync(x => x.TenantId == tenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe una moneda con ese código." });

        if (request.IsDefault)
        {
            var defaults = await db.Currencies.Where(x => x.TenantId == tenantId && x.IsDefault).ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        var entity = new Currency
        {
            TenantId = tenantId.Value,
            Code = code,
            Name = name,
            Symbol = symbol,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Currencies.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCurrencyAsync(Guid id, CurrencyRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Currencies.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la moneda." });

        var code = NormalizeUpper(request.Code, entity.Code);
        var name = NormalizeText(request.Name, entity.Name);
        var symbol = NormalizeText(request.Symbol, entity.Symbol);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(symbol))
            return Results.BadRequest(new { message = "Código, nombre y símbolo son obligatorios." });

        if (await db.Currencies.AnyAsync(x => x.Id != id && x.TenantId == entity.TenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otra moneda con ese código." });

        if (request.IsDefault)
        {
            var defaults = await db.Currencies.Where(x => x.TenantId == entity.TenantId && x.Id != id && x.IsDefault).ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        entity.Code = code;
        entity.Name = name;
        entity.Symbol = symbol;
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCurrencyAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Currencies.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la moneda." });

        if (await db.ExchangeRates.AnyAsync(x => x.CurrencyId == id) || await db.CompanySettings.AnyAsync(x => x.CurrencyId == id))
            return Results.BadRequest(new { message = "No puedes eliminar una moneda que ya está siendo utilizada." });

        db.Currencies.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetExchangeRatesAsync(NanchesoftDbContext db)
    {
        var rows = await db.ExchangeRates
            .AsNoTracking()
            .Include(x => x.Currency)
            .OrderByDescending(x => x.RateDate)
            .ThenBy(x => x.Currency!.Code)
            .Select(x => new ExchangeRateDto
            {
                ExchangeRateId = x.Id,
                TenantId = x.TenantId,
                CurrencyId = x.CurrencyId,
                CurrencyName = x.Currency != null ? $"{x.Currency.Code} · {x.Currency.Name}" : string.Empty,
                RateDate = x.RateDate,
                BuyRate = x.BuyRate,
                SellRate = x.SellRate,
                ReferenceRate = x.ReferenceRate,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateExchangeRateAsync(ExchangeRateRequest request, NanchesoftDbContext db)
    {
        var tenantId = request.TenantId ?? await ResolveDefaultTenantIdAsync(db);
        if (!tenantId.HasValue)
            return Results.BadRequest(new { message = "No existe un tenant disponible para el tipo de cambio." });

        if (!request.CurrencyId.HasValue || request.CurrencyId == Guid.Empty)
            return Results.BadRequest(new { message = "La moneda es obligatoria." });

        var currency = await db.Currencies.FirstOrDefaultAsync(x => x.Id == request.CurrencyId.Value);
        if (currency is null)
            return Results.BadRequest(new { message = "No se encontró la moneda enviada." });

        var rateDate = request.RateDate?.Date ?? DateTime.UtcNow.Date;

        if (request.BuyRate <= 0 || request.SellRate <= 0 || request.ReferenceRate <= 0)
            return Results.BadRequest(new { message = "Los valores del tipo de cambio deben ser mayores a cero." });

        if (await db.ExchangeRates.AnyAsync(x => x.CurrencyId == currency.Id && x.RateDate == rateDate))
            return Results.BadRequest(new { message = "Ya existe un tipo de cambio para esa moneda y fecha." });

        var entity = new ExchangeRate
        {
            TenantId = tenantId.Value,
            CurrencyId = currency.Id,
            RateDate = rateDate,
            BuyRate = request.BuyRate,
            SellRate = request.SellRate,
            ReferenceRate = request.ReferenceRate,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ExchangeRates.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateExchangeRateAsync(Guid id, ExchangeRateRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ExchangeRates.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el tipo de cambio." });

        var currencyId = request.CurrencyId ?? entity.CurrencyId;
        var rateDate = request.RateDate?.Date ?? entity.RateDate;

        if (request.BuyRate <= 0 || request.SellRate <= 0 || request.ReferenceRate <= 0)
            return Results.BadRequest(new { message = "Los valores del tipo de cambio deben ser mayores a cero." });

        if (await db.ExchangeRates.AnyAsync(x => x.Id != id && x.CurrencyId == currencyId && x.RateDate == rateDate))
            return Results.BadRequest(new { message = "Ya existe otro tipo de cambio para esa moneda y fecha." });

        entity.CurrencyId = currencyId;
        entity.RateDate = rateDate;
        entity.BuyRate = request.BuyRate;
        entity.SellRate = request.SellRate;
        entity.ReferenceRate = request.ReferenceRate;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteExchangeRateAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ExchangeRates.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el tipo de cambio." });

        db.ExchangeRates.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetTaxesAsync(NanchesoftDbContext db)
    {
        var rows = await db.Taxes
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .Select(x => new TaxDto
            {
                TaxId = x.Id,
                TenantId = x.TenantId,
                Code = x.Code,
                Name = x.Name,
                Rate = x.Rate,
                TaxType = x.TaxType,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateTaxAsync(TaxRequest request, NanchesoftDbContext db)
    {
        var tenantId = request.TenantId ?? await ResolveDefaultTenantIdAsync(db);
        if (!tenantId.HasValue)
            return Results.BadRequest(new { message = "No existe un tenant disponible para el impuesto." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        var taxType = NormalizeText(request.TaxType, "Traslado");

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (request.Rate < 0)
            return Results.BadRequest(new { message = "La tasa del impuesto no puede ser negativa." });

        if (await db.Taxes.AnyAsync(x => x.TenantId == tenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un impuesto con ese código." });

        if (request.IsDefault)
        {
            var defaults = await db.Taxes.Where(x => x.TenantId == tenantId && x.IsDefault).ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        var entity = new Tax
        {
            TenantId = tenantId.Value,
            Code = code,
            Name = name,
            Rate = request.Rate,
            TaxType = taxType,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Taxes.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateTaxAsync(Guid id, TaxRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Taxes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el impuesto." });

        var code = NormalizeUpper(request.Code, entity.Code);
        var name = NormalizeText(request.Name, entity.Name);
        var taxType = NormalizeText(request.TaxType, entity.TaxType);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (request.Rate < 0)
            return Results.BadRequest(new { message = "La tasa del impuesto no puede ser negativa." });

        if (await db.Taxes.AnyAsync(x => x.Id != id && x.TenantId == entity.TenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro impuesto con ese código." });

        if (request.IsDefault)
        {
            var defaults = await db.Taxes.Where(x => x.TenantId == entity.TenantId && x.Id != id && x.IsDefault).ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        entity.Code = code;
        entity.Name = name;
        entity.Rate = request.Rate;
        entity.TaxType = taxType;
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteTaxAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Taxes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el impuesto." });

        db.Taxes.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetUnitsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var currentTenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var query = db.Units.AsNoTracking();
        if (!isPlatformOwner && currentTenantId.HasValue)
            query = query.Where(x => x.TenantId == currentTenantId.Value);

        var rows = await query
            .OrderBy(x => x.Code)
            .Select(x => new UnitDto
            {
                UnitId = x.Id,
                TenantId = x.TenantId,
                Code = x.Code,
                Name = x.Name,
                Abbreviation = x.Abbreviation,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateUnitAsync(UnitRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var currentTenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);
        var tenantId = request.TenantId ?? currentTenantId ?? await ResolveDefaultTenantIdAsync(db);

        if (!tenantId.HasValue)
            return Results.BadRequest(new { message = "No existe un tenant disponible para la unidad." });

        if (!isPlatformOwner && currentTenantId.HasValue && tenantId.Value != currentTenantId.Value)
            return Results.BadRequest(new { message = "La unidad no pertenece al tenant activo." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        var abbreviation = NormalizeUpper(request.Abbreviation);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(abbreviation))
            return Results.BadRequest(new { message = "Código, nombre y abreviatura son obligatorios." });

        if (await db.Units.AnyAsync(x => x.TenantId == tenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe una unidad con ese código." });

        var entity = new Unit
        {
            TenantId = tenantId.Value,
            Code = code,
            Name = name,
            Abbreviation = abbreviation,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Units.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateUnitAsync(Guid id, UnitRequest request, HttpContext httpContext, NanchesoftDbContext db)
    {
        var currentTenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var entity = await db.Units.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null || (!isPlatformOwner && currentTenantId.HasValue && entity.TenantId != currentTenantId.Value))
            return Results.NotFound(new { message = "No se encontró la unidad." });

        var code = NormalizeUpper(request.Code, entity.Code);
        var name = NormalizeText(request.Name, entity.Name);
        var abbreviation = NormalizeUpper(request.Abbreviation, entity.Abbreviation);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(abbreviation))
            return Results.BadRequest(new { message = "Código, nombre y abreviatura son obligatorios." });

        if (await db.Units.AnyAsync(x => x.Id != id && x.TenantId == entity.TenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otra unidad con ese código." });

        entity.Code = code;
        entity.Name = name;
        entity.Abbreviation = abbreviation;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteUnitAsync(Guid id, HttpContext httpContext, NanchesoftDbContext db)
    {
        var currentTenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);

        var entity = await db.Units.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null || (!isPlatformOwner && currentTenantId.HasValue && entity.TenantId != currentTenantId.Value))
            return Results.NotFound(new { message = "No se encontró la unidad." });

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetBanksAsync(NanchesoftDbContext db)
    {
        var rows = await db.Banks
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new BankDto
            {
                BankId = x.Id,
                TenantId = x.TenantId,
                Code = x.Code,
                Name = x.Name,
                ShortName = x.ShortName,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateBankAsync(BankRequest request, NanchesoftDbContext db)
    {
        var tenantId = request.TenantId ?? await ResolveDefaultTenantIdAsync(db);
        if (!tenantId.HasValue)
            return Results.BadRequest(new { message = "No existe un tenant disponible para el banco." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        var shortName = NormalizeText(request.ShortName);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName))
            return Results.BadRequest(new { message = "Código, nombre y nombre corto son obligatorios." });

        if (await db.Banks.AnyAsync(x => x.TenantId == tenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un banco con ese código." });

        var entity = new Bank
        {
            TenantId = tenantId.Value,
            Code = code,
            Name = name,
            ShortName = shortName,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Banks.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateBankAsync(Guid id, BankRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Banks.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el banco." });

        var code = NormalizeUpper(request.Code, entity.Code);
        var name = NormalizeText(request.Name, entity.Name);
        var shortName = NormalizeText(request.ShortName, entity.ShortName);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName))
            return Results.BadRequest(new { message = "Código, nombre y nombre corto son obligatorios." });

        if (await db.Banks.AnyAsync(x => x.Id != id && x.TenantId == entity.TenantId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro banco con ese código." });

        entity.Code = code;
        entity.Name = name;
        entity.ShortName = shortName;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteBankAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Banks.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el banco." });

        db.Banks.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetCountriesAsync(NanchesoftDbContext db)
    {
        var rows = await db.Countries
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CountryDto
            {
                CountryId = x.Id,
                Code = x.Code,
                Name = x.Name,
                Iso2 = x.Iso2,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCountryAsync(CountryRequest request, NanchesoftDbContext db)
    {
        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        var iso2 = NormalizeUpper(request.Iso2);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(iso2))
            return Results.BadRequest(new { message = "Código, nombre e ISO2 son obligatorios." });

        if (await db.Countries.AnyAsync(x => x.Code == code || x.Iso2 == iso2))
            return Results.BadRequest(new { message = "Ya existe un país con ese código o ISO2." });

        var entity = new Country
        {
            Code = code,
            Name = name,
            Iso2 = iso2,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Countries.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCountryAsync(Guid id, CountryRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Countries.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el país." });

        var code = NormalizeUpper(request.Code, entity.Code);
        var name = NormalizeText(request.Name, entity.Name);
        var iso2 = NormalizeUpper(request.Iso2, entity.Iso2);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(iso2))
            return Results.BadRequest(new { message = "Código, nombre e ISO2 son obligatorios." });

        if (await db.Countries.AnyAsync(x => x.Id != id && (x.Code == code || x.Iso2 == iso2)))
            return Results.BadRequest(new { message = "Ya existe otro país con ese código o ISO2." });

        entity.Code = code;
        entity.Name = name;
        entity.Iso2 = iso2;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCountryAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Countries.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el país." });

        if (await db.States.AnyAsync(x => x.CountryId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un país que ya tiene estados." });

        db.Countries.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetStatesAsync(NanchesoftDbContext db)
    {
        var rows = await db.States
            .AsNoTracking()
            .Include(x => x.Country)
            .OrderBy(x => x.Country!.Name)
            .ThenBy(x => x.Name)
            .Select(x => new StateDto
            {
                StateId = x.Id,
                CountryId = x.CountryId,
                CountryName = x.Country != null ? x.Country.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateStateAsync(StateRequest request, NanchesoftDbContext db)
    {
        if (!request.CountryId.HasValue || request.CountryId == Guid.Empty)
            return Results.BadRequest(new { message = "El país es obligatorio." });

        var country = await db.Countries.FirstOrDefaultAsync(x => x.Id == request.CountryId.Value);
        if (country is null)
            return Results.BadRequest(new { message = "No se encontró el país enviado." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.States.AnyAsync(x => x.CountryId == country.Id && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un estado con ese código dentro del país." });

        var entity = new State
        {
            CountryId = country.Id,
            Code = code,
            Name = name,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.States.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateStateAsync(Guid id, StateRequest request, NanchesoftDbContext db)
    {
        var entity = await db.States.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el estado." });

        var countryId = request.CountryId ?? entity.CountryId;
        var code = NormalizeUpper(request.Code, entity.Code);
        var name = NormalizeText(request.Name, entity.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.States.AnyAsync(x => x.Id != id && x.CountryId == countryId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro estado con ese código dentro del país." });

        entity.CountryId = countryId;
        entity.Code = code;
        entity.Name = name;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteStateAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.States.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el estado." });

        if (await db.Cities.AnyAsync(x => x.StateId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un estado que ya tiene ciudades." });

        db.States.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetCitiesAsync(NanchesoftDbContext db)
    {
        var rows = await db.Cities
            .AsNoTracking()
            .Include(x => x.State)
                .ThenInclude(x => x.Country)
            .OrderBy(x => x.State!.Country!.Name)
            .ThenBy(x => x.State!.Name)
            .ThenBy(x => x.Name)
            .Select(x => new CityDto
            {
                CityId = x.Id,
                StateId = x.StateId,
                StateName = x.State != null ? x.State.Name : string.Empty,
                CountryId = x.State != null ? x.State.CountryId : Guid.Empty,
                CountryName = x.State != null && x.State.Country != null ? x.State.Country.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCityAsync(CityRequest request, NanchesoftDbContext db)
    {
        if (!request.StateId.HasValue || request.StateId == Guid.Empty)
            return Results.BadRequest(new { message = "El estado es obligatorio." });

        var state = await db.States.FirstOrDefaultAsync(x => x.Id == request.StateId.Value);
        if (state is null)
            return Results.BadRequest(new { message = "No se encontró el estado enviado." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.Cities.AnyAsync(x => x.StateId == state.Id && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe una ciudad con ese código dentro del estado." });

        var entity = new City
        {
            StateId = state.Id,
            Code = code,
            Name = name,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Cities.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCityAsync(Guid id, CityRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Cities.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la ciudad." });

        var stateId = request.StateId ?? entity.StateId;
        var code = NormalizeUpper(request.Code, entity.Code);
        var name = NormalizeText(request.Name, entity.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Código y nombre son obligatorios." });

        if (await db.Cities.AnyAsync(x => x.Id != id && x.StateId == stateId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otra ciudad con ese código dentro del estado." });

        entity.StateId = stateId;
        entity.Code = code;
        entity.Name = name;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCityAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Cities.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la ciudad." });

        db.Cities.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetDocumentSeriesAsync(NanchesoftDbContext db)
    {
        var rows = await db.DocumentSeries
            .AsNoTracking()
            .Include(x => x.Company)
            .OrderBy(x => x.DocumentType)
            .ThenBy(x => x.Code)
            .Select(x => new DocumentSeriesDto
            {
                DocumentSeriesId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                DocumentType = x.DocumentType,
                Code = x.Code,
                Name = x.Name,
                Prefix = x.Prefix,
                CurrentNumber = x.CurrentNumber,
                NumberLength = x.NumberLength,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateDocumentSeriesAsync(DocumentSeriesRequest request, NanchesoftDbContext db)
    {
        if (!request.CompanyId.HasValue || request.CompanyId == Guid.Empty)
            return Results.BadRequest(new { message = "La empresa es obligatoria." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId.Value);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa enviada." });

        var documentType = NormalizeUpper(request.DocumentType);
        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        var prefix = NormalizeUpper(request.Prefix);

        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Tipo de documento, código y nombre son obligatorios." });

        if (request.NumberLength <= 0)
            return Results.BadRequest(new { message = "La longitud del folio debe ser mayor a cero." });

        if (await db.DocumentSeries.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == documentType && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe una serie con ese código para el documento enviado." });

        if (request.IsDefault)
        {
            var defaults = await db.DocumentSeries
                .Where(x => x.CompanyId == company.Id && x.DocumentType == documentType && x.IsDefault)
                .ToListAsync();

            foreach (var item in defaults)
                item.IsDefault = false;
        }

        var entity = new DocumentSeries
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            DocumentType = documentType,
            Code = code,
            Name = name,
            Prefix = prefix,
            CurrentNumber = request.CurrentNumber,
            NumberLength = request.NumberLength,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.DocumentSeries.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateDocumentSeriesAsync(Guid id, DocumentSeriesRequest request, NanchesoftDbContext db)
    {
        var entity = await db.DocumentSeries.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la serie documental." });

        var companyId = request.CompanyId ?? entity.CompanyId;
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == companyId);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa enviada." });

        var documentType = NormalizeUpper(request.DocumentType, entity.DocumentType);
        var code = NormalizeUpper(request.Code, entity.Code);
        var name = NormalizeText(request.Name, entity.Name);
        var prefix = NormalizeUpper(request.Prefix, entity.Prefix);

        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "Tipo de documento, código y nombre son obligatorios." });

        if (request.NumberLength <= 0)
            return Results.BadRequest(new { message = "La longitud del folio debe ser mayor a cero." });

        if (await db.DocumentSeries.AnyAsync(x => x.Id != id && x.CompanyId == company.Id && x.DocumentType == documentType && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otra serie con ese código para el documento enviado." });

        if (request.IsDefault)
        {
            var defaults = await db.DocumentSeries
                .Where(x => x.CompanyId == company.Id && x.DocumentType == documentType && x.Id != id && x.IsDefault)
                .ToListAsync();

            foreach (var item in defaults)
                item.IsDefault = false;
        }

        entity.TenantId = company.TenantId;
        entity.CompanyId = company.Id;
        entity.DocumentType = documentType;
        entity.Code = code;
        entity.Name = name;
        entity.Prefix = prefix;
        entity.CurrentNumber = request.CurrentNumber;
        entity.NumberLength = request.NumberLength;
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteDocumentSeriesAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.DocumentSeries.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la serie documental." });

        if (await db.DocumentFolios.AnyAsync(x => x.SeriesId == id) ||
            await db.CompanySettings.AnyAsync(x => x.DefaultPurchaseSeriesId == id || x.DefaultSalesSeriesId == id))
        {
            return Results.BadRequest(new { message = "No puedes eliminar una serie que ya está relacionada." });
        }

        db.DocumentSeries.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetDocumentFoliosAsync(NanchesoftDbContext db)
    {
        var rows = await db.DocumentFolios
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Series)
            .OrderBy(x => x.DocumentType)
            .ThenBy(x => x.Company!.Name)
            .Select(x => new DocumentFolioDto
            {
                DocumentFolioId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                DocumentType = x.DocumentType,
                SeriesId = x.SeriesId,
                SeriesName = x.Series != null ? $"{x.Series.Code} · {x.Series.Name}" : string.Empty,
                CurrentNumber = x.CurrentNumber,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateDocumentFolioAsync(DocumentFolioRequest request, NanchesoftDbContext db)
    {
        if (!request.CompanyId.HasValue || request.CompanyId == Guid.Empty || !request.SeriesId.HasValue || request.SeriesId == Guid.Empty)
            return Results.BadRequest(new { message = "Empresa y serie son obligatorias." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId.Value);
        var series = await db.DocumentSeries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.SeriesId.Value);

        if (company is null || series is null)
            return Results.BadRequest(new { message = "No se encontró la empresa o la serie enviada." });

        var documentType = NormalizeUpper(request.DocumentType, series.DocumentType);
        if (string.IsNullOrWhiteSpace(documentType))
            return Results.BadRequest(new { message = "El tipo de documento es obligatorio." });

        if (await db.DocumentFolios.AnyAsync(x => x.CompanyId == company.Id && x.DocumentType == documentType && x.SeriesId == series.Id))
            return Results.BadRequest(new { message = "Ya existe un folio para esa empresa, documento y serie." });

        var entity = new DocumentFolio
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            DocumentType = documentType,
            SeriesId = series.Id,
            CurrentNumber = request.CurrentNumber,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.DocumentFolios.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateDocumentFolioAsync(Guid id, DocumentFolioRequest request, NanchesoftDbContext db)
    {
        var entity = await db.DocumentFolios.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el folio documental." });

        var companyId = request.CompanyId ?? entity.CompanyId;
        var seriesId = request.SeriesId ?? entity.SeriesId;
        var documentType = NormalizeUpper(request.DocumentType, entity.DocumentType);

        if (companyId == Guid.Empty || seriesId == Guid.Empty || string.IsNullOrWhiteSpace(documentType))
            return Results.BadRequest(new { message = "Empresa, serie y tipo de documento son obligatorios." });

        if (await db.DocumentFolios.AnyAsync(x => x.Id != id && x.CompanyId == companyId && x.DocumentType == documentType && x.SeriesId == seriesId))
            return Results.BadRequest(new { message = "Ya existe otro folio con esa combinación de empresa, documento y serie." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == companyId);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa enviada." });

        entity.TenantId = company.TenantId;
        entity.CompanyId = companyId;
        entity.DocumentType = documentType;
        entity.SeriesId = seriesId;
        entity.CurrentNumber = request.CurrentNumber;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteDocumentFolioAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.DocumentFolios.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el folio documental." });

        db.DocumentFolios.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetCompanySettingsAsync(NanchesoftDbContext db)
    {
        var rows = await db.CompanySettings
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Currency)
            .Include(x => x.DefaultPurchaseSeries)
            .Include(x => x.DefaultSalesSeries)
            .OrderBy(x => x.Company!.Name)
            .Select(x => new CompanySettingDto
            {
                CompanySettingId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                CurrencyId = x.CurrencyId,
                CurrencyName = x.Currency != null ? $"{x.Currency.Code} · {x.Currency.Name}" : string.Empty,
                Timezone = x.Timezone,
                MonetaryDecimals = x.MonetaryDecimals,
                QuantityDecimals = x.QuantityDecimals,
                DefaultPurchaseSeriesId = x.DefaultPurchaseSeriesId,
                DefaultPurchaseSeriesName = x.DefaultPurchaseSeries != null ? $"{x.DefaultPurchaseSeries.Code} · {x.DefaultPurchaseSeries.Name}" : string.Empty,
                DefaultSalesSeriesId = x.DefaultSalesSeriesId,
                DefaultSalesSeriesName = x.DefaultSalesSeries != null ? $"{x.DefaultSalesSeries.Code} · {x.DefaultSalesSeries.Name}" : string.Empty,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCompanySettingAsync(CompanySettingRequest request, NanchesoftDbContext db)
    {
        if (!request.CompanyId.HasValue || request.CompanyId == Guid.Empty || !request.CurrencyId.HasValue || request.CurrencyId == Guid.Empty)
            return Results.BadRequest(new { message = "Empresa y moneda base son obligatorias." });

        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CompanyId.Value);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa enviada." });

        if (await db.CompanySettings.AnyAsync(x => x.CompanyId == company.Id))
            return Results.BadRequest(new { message = "Esa empresa ya tiene configuración operativa." });

        var entity = new CompanySetting
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            CurrencyId = request.CurrencyId.Value,
            Timezone = NormalizeText(request.Timezone, "America/Mexico_City"),
            MonetaryDecimals = request.MonetaryDecimals,
            QuantityDecimals = request.QuantityDecimals,
            DefaultPurchaseSeriesId = request.DefaultPurchaseSeriesId,
            DefaultSalesSeriesId = request.DefaultSalesSeriesId,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.CompanySettings.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCompanySettingAsync(Guid id, CompanySettingRequest request, NanchesoftDbContext db)
    {
        var entity = await db.CompanySettings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la configuración de empresa." });

        var companyId = request.CompanyId ?? entity.CompanyId;
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == companyId);
        if (company is null)
            return Results.BadRequest(new { message = "No se encontró la empresa enviada." });

        var currencyId = request.CurrencyId ?? entity.CurrencyId;

        if (await db.CompanySettings.AnyAsync(x => x.Id != id && x.CompanyId == companyId))
            return Results.BadRequest(new { message = "La empresa ya tiene otra configuración operativa." });

        entity.TenantId = company.TenantId;
        entity.CompanyId = companyId;
        entity.CurrencyId = currencyId;
        entity.Timezone = NormalizeText(request.Timezone, entity.Timezone);
        entity.MonetaryDecimals = request.MonetaryDecimals;
        entity.QuantityDecimals = request.QuantityDecimals;
        entity.DefaultPurchaseSeriesId = request.DefaultPurchaseSeriesId;
        entity.DefaultSalesSeriesId = request.DefaultSalesSeriesId;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCompanySettingAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.CompanySettings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la configuración de empresa." });

        db.CompanySettings.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static string NormalizeUpper(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string NormalizeText(string? value, string fallback = "")
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
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

public sealed class CurrencyRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
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

public sealed class UnitDto
{
    public Guid UnitId { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class UnitRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
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

public sealed class BankRequest
{
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class CountryDto
{
    public Guid CountryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Iso2 { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CountryRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Iso2 { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
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

public sealed class StateRequest
{
    public Guid? CountryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
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

public sealed class CityRequest
{
    public Guid? StateId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
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

public sealed class DocumentFolioRequest
{
    public Guid? CompanyId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public Guid? SeriesId { get; set; }
    public int CurrentNumber { get; set; }
    public bool IsActive { get; set; } = true;
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
