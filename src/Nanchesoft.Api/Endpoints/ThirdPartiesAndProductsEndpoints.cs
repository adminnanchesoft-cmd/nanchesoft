using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ThirdPartiesAndProductsEndpoints
{
    public static IEndpointRouteBuilder MapThirdPartiesAndProductsEndpoints(this IEndpointRouteBuilder app)
    {
        var customers = app.MapGroup("/api/third-parties/customers").WithTags("Customers");
        customers.MapGet("/", GetCustomersAsync);
        customers.MapPost("/", CreateCustomerAsync);
        customers.MapPut("/{id:guid}", UpdateCustomerAsync);
        customers.MapDelete("/{id:guid}", DeleteCustomerAsync);

        var suppliers = app.MapGroup("/api/third-parties/suppliers").WithTags("Suppliers");
        suppliers.MapGet("/", GetSuppliersAsync);
        suppliers.MapPost("/", CreateSupplierAsync);
        suppliers.MapPut("/{id:guid}", UpdateSupplierAsync);
        suppliers.MapDelete("/{id:guid}", DeleteSupplierAsync);

        var contacts = app.MapGroup("/api/third-parties/contacts").WithTags("Contacts");
        contacts.MapGet("/", GetContactsAsync);
        contacts.MapPost("/", CreateContactAsync);
        contacts.MapPut("/{id:guid}", UpdateContactAsync);
        contacts.MapDelete("/{id:guid}", DeleteContactAsync);

        var addresses = app.MapGroup("/api/third-parties/addresses").WithTags("Addresses");
        addresses.MapGet("/", GetAddressesAsync);
        addresses.MapPost("/", CreateAddressAsync);
        addresses.MapPut("/{id:guid}", UpdateAddressAsync);
        addresses.MapDelete("/{id:guid}", DeleteAddressAsync);

        var bankAccounts = app.MapGroup("/api/third-parties/bank-accounts").WithTags("ThirdPartyBankAccounts");
        bankAccounts.MapGet("/", GetBankAccountsAsync);
        bankAccounts.MapPost("/", CreateBankAccountAsync);
        bankAccounts.MapPut("/{id:guid}", UpdateBankAccountAsync);
        bankAccounts.MapDelete("/{id:guid}", DeleteBankAccountAsync);

        var categories = app.MapGroup("/api/products/categories").WithTags("ItemCategories");
        categories.MapGet("/", GetCategoriesAsync);
        categories.MapPost("/", CreateCategoryAsync);
        categories.MapPut("/{id:guid}", UpdateCategoryAsync);
        categories.MapDelete("/{id:guid}", DeleteCategoryAsync);

        var brands = app.MapGroup("/api/products/brands").WithTags("ItemBrands");
        brands.MapGet("/", GetBrandsAsync);
        brands.MapPost("/", CreateBrandAsync);
        brands.MapPut("/{id:guid}", UpdateBrandAsync);
        brands.MapDelete("/{id:guid}", DeleteBrandAsync);

        var models = app.MapGroup("/api/products/models").WithTags("ItemModels");
        models.MapGet("/", GetModelsAsync);
        models.MapPost("/", CreateModelAsync);
        models.MapPut("/{id:guid}", UpdateModelAsync);
        models.MapDelete("/{id:guid}", DeleteModelAsync);

        var items = app.MapGroup("/api/products/items").WithTags("Items");
        items.MapGet("/", GetItemsAsync);
        items.MapPost("/", CreateItemAsync);
        items.MapPut("/{id:guid}", UpdateItemAsync);
        items.MapDelete("/{id:guid}", DeleteItemAsync);

        var priceLists = app.MapGroup("/api/products/price-lists").WithTags("ItemPriceLists");
        priceLists.MapGet("/", GetPriceListsAsync);
        priceLists.MapPost("/", CreatePriceListAsync);
        priceLists.MapPut("/{id:guid}", UpdatePriceListAsync);
        priceLists.MapDelete("/{id:guid}", DeletePriceListAsync);

        var barcodes = app.MapGroup("/api/products/barcodes").WithTags("ItemBarcodes");
        barcodes.MapGet("/", GetBarcodesAsync);
        barcodes.MapPost("/", CreateBarcodeAsync);
        barcodes.MapPut("/{id:guid}", UpdateBarcodeAsync);
        barcodes.MapDelete("/{id:guid}", DeleteBarcodeAsync);

        return app;
    }

    private static async Task<Guid?> ResolveDefaultTenantIdAsync(NanchesoftDbContext db)
        => await db.Tenants.OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveDefaultContextAsync(NanchesoftDbContext db)
    {
        var company = await db.Companies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TenantId }).FirstOrDefaultAsync();
        return company is null ? (null, null) : (company.TenantId, company.Id);
    }

    private static async Task<(Guid? TenantId, Guid? CompanyId)> ResolveScopedContextAsync(HttpContext httpContext, NanchesoftDbContext db, Guid? requestedCompanyId = null)
    {
        var isPlatformOwner = ApiTenantScope.IsPlatformOwner(httpContext);
        var currentTenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var currentCompanyId = requestedCompanyId ?? ApiTenantScope.ResolveCompanyId(httpContext);

        if (currentCompanyId.HasValue)
        {
            var company = await db.Companies.AsNoTracking()
                .Where(x => x.Id == currentCompanyId.Value)
                .Select(x => new { x.Id, x.TenantId })
                .FirstOrDefaultAsync();

            if (company is null)
                return (null, null);

            if (!isPlatformOwner && currentTenantId.HasValue && company.TenantId != currentTenantId.Value)
                return (null, null);

            return (company.TenantId, company.Id);
        }

        if (!isPlatformOwner && currentTenantId.HasValue)
        {
            var company = await db.Companies.AsNoTracking()
                .Where(x => x.TenantId == currentTenantId.Value)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new { x.Id, x.TenantId })
                .FirstOrDefaultAsync();

            return company is null ? (currentTenantId, null) : (company.TenantId, company.Id);
        }

        return await ResolveDefaultContextAsync(db);
    }

    private static bool IsOutOfScope(HttpContext httpContext, Guid entityTenantId)
    {
        if (ApiTenantScope.IsPlatformOwner(httpContext))
            return false;

        var currentTenantId = ApiTenantScope.ResolveTenantId(httpContext);
        return currentTenantId.HasValue && currentTenantId.Value != entityTenantId;
    }

    private static string NormalizeUpper(string? value, string fallback = "") => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();
    private static string NormalizeText(string? value, string fallback = "") => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static string NormalizePartyType(string? value, string fallback = "")
    {
        var normalized = NormalizeText(value, fallback).ToLowerInvariant();
        return normalized switch
        {
            "customer" or "cliente" => "customer",
            "supplier" or "proveedor" => "supplier",
            _ => normalized
        };
    }

    private static async Task<bool> ThirdPartyExistsAsync(NanchesoftDbContext db, string thirdPartyType, Guid thirdPartyId)
        => thirdPartyType switch
        {
            "customer" => await db.Customers.AnyAsync(x => x.Id == thirdPartyId),
            "supplier" => await db.Suppliers.AnyAsync(x => x.Id == thirdPartyId),
            _ => false
        };

    private static async Task<IResult> GetCustomersAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.Customers.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Company)
            .Include(x => x.Currency)
            .Include(x => x.PriceList)
            .OrderBy(x => x.Code)
            .Select(x => new CustomerDto
            {
                CustomerId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                CurrencyId = x.CurrencyId,
                CurrencyName = x.Currency != null ? x.Currency.Code : string.Empty,
                PriceListId = x.PriceListId,
                PriceListName = x.PriceList != null ? x.PriceList.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                LegalName = x.LegalName,
                TaxId = x.TaxId,
                Email = x.Email,
                Phone = x.Phone,
                CreditLimit = x.CreditLimit,
                PaymentTermDays = x.PaymentTermDays,
                IsActive = x.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCustomerAsync(HttpContext httpContext, CustomerRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el cliente." });

        var code = NormalizeUpper(request.Code);
        var name = NormalizeText(request.Name);
        var legalName = NormalizeText(request.LegalName);
        var taxId = NormalizeUpper(request.TaxId);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(legalName) || string.IsNullOrWhiteSpace(taxId))
            return Results.BadRequest(new { message = "Código, nombre, razón social y RFC/Tax ID son obligatorios." });

        if (await db.Customers.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un cliente con ese código." });

        var entity = new Customer
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            CurrencyId = request.CurrencyId,
            PriceListId = request.PriceListId,
            Code = code,
            Name = name,
            LegalName = legalName,
            TaxId = taxId,
            Email = NormalizeText(request.Email),
            Phone = NormalizeText(request.Phone),
            CreditLimit = request.CreditLimit,
            PaymentTermDays = request.PaymentTermDays,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Customers.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCustomerAsync(HttpContext httpContext, Guid id, CustomerRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Customers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el cliente." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.Customers.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro cliente con ese código." });

        entity.CurrencyId = request.CurrencyId;
        entity.PriceListId = request.PriceListId;
        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.LegalName = NormalizeText(request.LegalName, entity.LegalName);
        entity.TaxId = NormalizeUpper(request.TaxId, entity.TaxId);
        entity.Email = NormalizeText(request.Email, entity.Email);
        entity.Phone = NormalizeText(request.Phone, entity.Phone);
        entity.CreditLimit = request.CreditLimit;
        entity.PaymentTermDays = request.PaymentTermDays;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCustomerAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Customers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el cliente." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        if (await db.ThirdPartyContacts.AnyAsync(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == id) ||
            await db.ThirdPartyAddresses.AnyAsync(x => x.ThirdPartyType == "customer" && x.ThirdPartyId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un cliente con contactos o direcciones relacionadas." });

        db.Customers.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetSuppliersAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.Suppliers.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Company)
            .Include(x => x.Currency)
            .OrderBy(x => x.Code)
            .Select(x => new SupplierDto
            {
                SupplierId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                CurrencyId = x.CurrencyId,
                CurrencyName = x.Currency != null ? x.Currency.Code : string.Empty,
                Code = x.Code,
                Name = x.Name,
                LegalName = x.LegalName,
                TaxId = x.TaxId,
                Email = x.Email,
                Phone = x.Phone,
                PaymentTermDays = x.PaymentTermDays,
                IsActive = x.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateSupplierAsync(HttpContext httpContext, SupplierRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el proveedor." });

        var code = NormalizeUpper(request.Code);
        if (await db.Suppliers.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un proveedor con ese código." });

        var entity = new Supplier
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            CurrencyId = request.CurrencyId,
            Code = code,
            Name = NormalizeText(request.Name),
            LegalName = NormalizeText(request.LegalName),
            TaxId = NormalizeUpper(request.TaxId),
            Email = NormalizeText(request.Email),
            Phone = NormalizeText(request.Phone),
            PaymentTermDays = request.PaymentTermDays,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Suppliers.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateSupplierAsync(HttpContext httpContext, Guid id, SupplierRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el proveedor." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.Suppliers.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro proveedor con ese código." });

        entity.CurrencyId = request.CurrencyId;
        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.LegalName = NormalizeText(request.LegalName, entity.LegalName);
        entity.TaxId = NormalizeUpper(request.TaxId, entity.TaxId);
        entity.Email = NormalizeText(request.Email, entity.Email);
        entity.Phone = NormalizeText(request.Phone, entity.Phone);
        entity.PaymentTermDays = request.PaymentTermDays;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteSupplierAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el proveedor." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        if (await db.ThirdPartyContacts.AnyAsync(x => x.ThirdPartyType == "supplier" && x.ThirdPartyId == id) ||
            await db.ThirdPartyBankAccounts.AnyAsync(x => x.ThirdPartyType == "supplier" && x.ThirdPartyId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un proveedor con relaciones activas." });

        db.Suppliers.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetContactsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.ThirdPartyContacts.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .OrderBy(x => x.ThirdPartyType)
            .ThenBy(x => x.Name)
            .Select(x => new ContactDto
            {
                ContactId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                ThirdPartyType = x.ThirdPartyType,
                ThirdPartyId = x.ThirdPartyId,
                ThirdPartyName = string.Empty,
                Name = x.Name,
                Position = x.Position,
                Email = x.Email,
                Phone = x.Phone,
                Mobile = x.Mobile,
                IsPrimary = x.IsPrimary,
                IsActive = x.IsActive
            }).ToListAsync();

        var customerNames = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        var supplierNames = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

        foreach (var row in rows)
        {
            row.ThirdPartyName = row.ThirdPartyType == "customer"
                ? customerNames.GetValueOrDefault(row.ThirdPartyId, string.Empty)
                : supplierNames.GetValueOrDefault(row.ThirdPartyId, string.Empty);
        }

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateContactAsync(HttpContext httpContext, ContactRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;
        var partyType = NormalizePartyType(request.ThirdPartyType);

        if (!tenantId.HasValue || !companyId.HasValue || !request.ThirdPartyId.HasValue || !await ThirdPartyExistsAsync(db, partyType, request.ThirdPartyId.Value))
            return Results.BadRequest(new { message = "No se encontró el tercero relacionado para el contacto." });

        if (request.IsPrimary)
        {
            var primaryContacts = await db.ThirdPartyContacts
                .Where(x => x.ThirdPartyType == partyType && x.ThirdPartyId == request.ThirdPartyId.Value && x.IsPrimary)
                .ToListAsync();

            foreach (var item in primaryContacts)
                item.IsPrimary = false;
        }

        var entity = new ThirdPartyContact
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            ThirdPartyType = partyType,
            ThirdPartyId = request.ThirdPartyId.Value,
            Name = NormalizeText(request.Name),
            Position = NormalizeText(request.Position),
            Email = NormalizeText(request.Email),
            Phone = NormalizeText(request.Phone),
            Mobile = NormalizeText(request.Mobile),
            IsPrimary = request.IsPrimary,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ThirdPartyContacts.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateContactAsync(HttpContext httpContext, Guid id, ContactRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ThirdPartyContacts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el contacto." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var partyType = NormalizePartyType(request.ThirdPartyType, entity.ThirdPartyType);
        var thirdPartyId = request.ThirdPartyId ?? entity.ThirdPartyId;

        if (!await ThirdPartyExistsAsync(db, partyType, thirdPartyId))
            return Results.BadRequest(new { message = "No se encontró el tercero relacionado para el contacto." });

        if (request.IsPrimary)
        {
            var primaryContacts = await db.ThirdPartyContacts
                .Where(x => x.Id != id && x.ThirdPartyType == partyType && x.ThirdPartyId == thirdPartyId && x.IsPrimary)
                .ToListAsync();

            foreach (var item in primaryContacts)
                item.IsPrimary = false;
        }

        entity.ThirdPartyType = partyType;
        entity.ThirdPartyId = thirdPartyId;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.Position = NormalizeText(request.Position, entity.Position);
        entity.Email = NormalizeText(request.Email, entity.Email);
        entity.Phone = NormalizeText(request.Phone, entity.Phone);
        entity.Mobile = NormalizeText(request.Mobile, entity.Mobile);
        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteContactAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ThirdPartyContacts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el contacto." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        db.ThirdPartyContacts.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetAddressesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.ThirdPartyAddresses.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Country)
            .Include(x => x.State)
            .Include(x => x.City)
            .OrderBy(x => x.ThirdPartyType)
            .ThenBy(x => x.Street)
            .Select(x => new AddressDto
            {
                AddressId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                ThirdPartyType = x.ThirdPartyType,
                ThirdPartyId = x.ThirdPartyId,
                ThirdPartyName = string.Empty,
                AddressType = x.AddressType,
                Street = x.Street,
                ExteriorNumber = x.ExteriorNumber,
                InteriorNumber = x.InteriorNumber,
                Neighborhood = x.Neighborhood,
                ZipCode = x.ZipCode,
                CountryId = x.CountryId,
                CountryName = x.Country != null ? x.Country.Name : string.Empty,
                StateId = x.StateId,
                StateName = x.State != null ? x.State.Name : string.Empty,
                CityId = x.CityId,
                CityName = x.City != null ? x.City.Name : string.Empty,
                Reference = x.Reference,
                IsPrimary = x.IsPrimary,
                IsActive = x.IsActive
            }).ToListAsync();

        var customerNames = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        var supplierNames = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        foreach (var row in rows)
        {
            row.ThirdPartyName = row.ThirdPartyType == "customer"
                ? customerNames.GetValueOrDefault(row.ThirdPartyId, string.Empty)
                : supplierNames.GetValueOrDefault(row.ThirdPartyId, string.Empty);
        }

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateAddressAsync(HttpContext httpContext, AddressRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;
        var partyType = NormalizePartyType(request.ThirdPartyType);

        if (!tenantId.HasValue || !companyId.HasValue || !request.ThirdPartyId.HasValue || !await ThirdPartyExistsAsync(db, partyType, request.ThirdPartyId.Value))
            return Results.BadRequest(new { message = "No se encontró el tercero relacionado para la dirección." });

        if (request.IsPrimary)
        {
            var primaryRows = await db.ThirdPartyAddresses
                .Where(x => x.ThirdPartyType == partyType && x.ThirdPartyId == request.ThirdPartyId.Value && x.IsPrimary)
                .ToListAsync();

            foreach (var item in primaryRows)
                item.IsPrimary = false;
        }

        var entity = new ThirdPartyAddress
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            ThirdPartyType = partyType,
            ThirdPartyId = request.ThirdPartyId.Value,
            AddressType = NormalizeText(request.AddressType, "Principal"),
            Street = NormalizeText(request.Street),
            ExteriorNumber = NormalizeText(request.ExteriorNumber),
            InteriorNumber = NormalizeText(request.InteriorNumber),
            Neighborhood = NormalizeText(request.Neighborhood),
            ZipCode = NormalizeText(request.ZipCode),
            CountryId = request.CountryId,
            StateId = request.StateId,
            CityId = request.CityId,
            Reference = NormalizeText(request.Reference),
            IsPrimary = request.IsPrimary,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ThirdPartyAddresses.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateAddressAsync(HttpContext httpContext, Guid id, AddressRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ThirdPartyAddresses.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la dirección." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var partyType = NormalizePartyType(request.ThirdPartyType, entity.ThirdPartyType);
        var thirdPartyId = request.ThirdPartyId ?? entity.ThirdPartyId;

        if (!await ThirdPartyExistsAsync(db, partyType, thirdPartyId))
            return Results.BadRequest(new { message = "No se encontró el tercero relacionado para la dirección." });

        if (request.IsPrimary)
        {
            var primaryRows = await db.ThirdPartyAddresses
                .Where(x => x.Id != id && x.ThirdPartyType == partyType && x.ThirdPartyId == thirdPartyId && x.IsPrimary)
                .ToListAsync();

            foreach (var item in primaryRows)
                item.IsPrimary = false;
        }

        entity.ThirdPartyType = partyType;
        entity.ThirdPartyId = thirdPartyId;
        entity.AddressType = NormalizeText(request.AddressType, entity.AddressType);
        entity.Street = NormalizeText(request.Street, entity.Street);
        entity.ExteriorNumber = NormalizeText(request.ExteriorNumber, entity.ExteriorNumber);
        entity.InteriorNumber = NormalizeText(request.InteriorNumber, entity.InteriorNumber);
        entity.Neighborhood = NormalizeText(request.Neighborhood, entity.Neighborhood);
        entity.ZipCode = NormalizeText(request.ZipCode, entity.ZipCode);
        entity.CountryId = request.CountryId;
        entity.StateId = request.StateId;
        entity.CityId = request.CityId;
        entity.Reference = NormalizeText(request.Reference, entity.Reference);
        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteAddressAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ThirdPartyAddresses.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la dirección." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        db.ThirdPartyAddresses.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetBankAccountsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.ThirdPartyBankAccounts.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Bank)
            .Include(x => x.Currency)
            .OrderBy(x => x.ThirdPartyType)
            .ThenBy(x => x.AccountHolder)
            .Select(x => new BankAccountDto
            {
                BankAccountId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                ThirdPartyType = x.ThirdPartyType,
                ThirdPartyId = x.ThirdPartyId,
                ThirdPartyName = string.Empty,
                BankId = x.BankId,
                BankName = x.Bank != null ? x.Bank.Name : string.Empty,
                CurrencyId = x.CurrencyId,
                CurrencyName = x.Currency != null ? x.Currency.Code : string.Empty,
                AccountHolder = x.AccountHolder,
                AccountNumber = x.AccountNumber,
                Clabe = x.Clabe,
                IsPrimary = x.IsPrimary,
                IsActive = x.IsActive
            }).ToListAsync();

        var customerNames = await db.Customers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        var supplierNames = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        foreach (var row in rows)
        {
            row.ThirdPartyName = row.ThirdPartyType == "customer"
                ? customerNames.GetValueOrDefault(row.ThirdPartyId, string.Empty)
                : supplierNames.GetValueOrDefault(row.ThirdPartyId, string.Empty);
        }

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateBankAccountAsync(HttpContext httpContext, BankAccountRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;
        var partyType = NormalizePartyType(request.ThirdPartyType);

        if (!tenantId.HasValue || !companyId.HasValue || !request.ThirdPartyId.HasValue || !request.BankId.HasValue)
            return Results.BadRequest(new { message = "Faltan datos obligatorios para la cuenta bancaria del tercero." });

        if (!await ThirdPartyExistsAsync(db, partyType, request.ThirdPartyId.Value))
            return Results.BadRequest(new { message = "No se encontró el tercero relacionado para la cuenta bancaria." });

        if (request.IsPrimary)
        {
            var primaryRows = await db.ThirdPartyBankAccounts
                .Where(x => x.ThirdPartyType == partyType && x.ThirdPartyId == request.ThirdPartyId.Value && x.IsPrimary)
                .ToListAsync();

            foreach (var item in primaryRows)
                item.IsPrimary = false;
        }

        var entity = new ThirdPartyBankAccount
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            ThirdPartyType = partyType,
            ThirdPartyId = request.ThirdPartyId.Value,
            BankId = request.BankId.Value,
            CurrencyId = request.CurrencyId,
            AccountHolder = NormalizeText(request.AccountHolder),
            AccountNumber = NormalizeText(request.AccountNumber),
            Clabe = NormalizeText(request.Clabe),
            IsPrimary = request.IsPrimary,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ThirdPartyBankAccounts.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateBankAccountAsync(HttpContext httpContext, Guid id, BankAccountRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ThirdPartyBankAccounts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la cuenta bancaria del tercero." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var partyType = NormalizePartyType(request.ThirdPartyType, entity.ThirdPartyType);
        var thirdPartyId = request.ThirdPartyId ?? entity.ThirdPartyId;

        if (!await ThirdPartyExistsAsync(db, partyType, thirdPartyId))
            return Results.BadRequest(new { message = "No se encontró el tercero relacionado para la cuenta bancaria." });

        if (request.IsPrimary)
        {
            var primaryRows = await db.ThirdPartyBankAccounts
                .Where(x => x.Id != id && x.ThirdPartyType == partyType && x.ThirdPartyId == thirdPartyId && x.IsPrimary)
                .ToListAsync();

            foreach (var item in primaryRows)
                item.IsPrimary = false;
        }

        entity.ThirdPartyType = partyType;
        entity.ThirdPartyId = thirdPartyId;
        entity.BankId = request.BankId ?? entity.BankId;
        entity.CurrencyId = request.CurrencyId;
        entity.AccountHolder = NormalizeText(request.AccountHolder, entity.AccountHolder);
        entity.AccountNumber = NormalizeText(request.AccountNumber, entity.AccountNumber);
        entity.Clabe = NormalizeText(request.Clabe, entity.Clabe);
        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteBankAccountAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ThirdPartyBankAccounts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la cuenta bancaria del tercero." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        db.ThirdPartyBankAccounts.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetCategoriesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.ItemCategories.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Company)
            .Include(x => x.Parent)
            .OrderBy(x => x.Code)
            .Select(x => new CategoryDto
            {
                CategoryId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                ParentId = x.ParentId,
                ParentName = x.Parent != null ? x.Parent.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCategoryAsync(HttpContext httpContext, CategoryRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para la categoría." });

        var code = NormalizeUpper(request.Code);
        if (await db.ItemCategories.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe una categoría con ese código." });

        var entity = new ItemCategory
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            ParentId = request.ParentId,
            Code = code,
            Name = NormalizeText(request.Name),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ItemCategories.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCategoryAsync(HttpContext httpContext, Guid id, CategoryRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ItemCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la categoría." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.ItemCategories.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otra categoría con ese código." });

        entity.ParentId = request.ParentId;
        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCategoryAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ItemCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la categoría." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        if (await db.Items.AnyAsync(x => x.CategoryId == id))
            return Results.BadRequest(new { message = "No puedes eliminar una categoría en uso por productos." });

        db.ItemCategories.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetBrandsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.ItemBrands.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Company)
            .OrderBy(x => x.Code)
            .Select(x => new BrandDto
            {
                BrandId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateBrandAsync(HttpContext httpContext, BrandRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para la marca." });

        var code = NormalizeUpper(request.Code);
        if (await db.ItemBrands.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe una marca con ese código." });

        var entity = new ItemBrand
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            Code = code,
            Name = NormalizeText(request.Name),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ItemBrands.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateBrandAsync(HttpContext httpContext, Guid id, BrandRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ItemBrands.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la marca." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.ItemBrands.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otra marca con ese código." });

        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteBrandAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ItemBrands.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la marca." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        if (await db.Items.AnyAsync(x => x.BrandId == id))
            return Results.BadRequest(new { message = "No puedes eliminar una marca en uso." });

        db.ItemBrands.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetModelsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.ItemModels.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Company)
            .OrderBy(x => x.Code)
            .Select(x => new ModelDto
            {
                ModelId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateModelAsync(HttpContext httpContext, ModelRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el modelo." });

        var code = NormalizeUpper(request.Code);
        if (await db.ItemModels.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un modelo con ese código." });

        var entity = new ItemModel
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            Code = code,
            Name = NormalizeText(request.Name),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ItemModels.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateModelAsync(HttpContext httpContext, Guid id, ModelRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ItemModels.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el modelo." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.ItemModels.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro modelo con ese código." });

        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteModelAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ItemModels.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el modelo." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        if (await db.Items.AnyAsync(x => x.ModelId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un modelo en uso por productos." });

        db.ItemModels.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetItemsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.Items.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Company)
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Model)
            .Include(x => x.Unit)
            .Include(x => x.Tax)
            .Include(x => x.Currency)
            .OrderBy(x => x.Code)
            .Select(x => new ItemDto
            {
                ItemId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : string.Empty,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : string.Empty,
                ModelId = x.ModelId,
                ModelName = x.Model != null ? x.Model.Name : string.Empty,
                UnitId = x.UnitId,
                UnitName = x.Unit != null ? x.Unit.Name : string.Empty,
                TaxId = x.TaxId,
                TaxName = x.Tax != null ? x.Tax.Name : string.Empty,
                CurrencyId = x.CurrencyId,
                CurrencyName = x.Currency != null ? x.Currency.Code : string.Empty,
                Code = x.Code,
                Barcode = x.Barcode,
                Name = x.Name,
                Description = x.Description,
                ItemType = x.ItemType,
                BasePrice = x.BasePrice,
                BaseCost = x.BaseCost,
                ManagesInventory = x.ManagesInventory,
                UsesLots = x.UsesLots,
                UsesSerials = x.UsesSerials,
                IsSaleItem = x.IsSaleItem,
                IsPurchaseItem = x.IsPurchaseItem,
                IsActive = x.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateItemAsync(HttpContext httpContext, ItemRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para el producto." });

        var code = NormalizeUpper(request.Code);
        if (await db.Items.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe un producto/servicio con ese código." });

        var entity = new Item
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            ModelId = request.ModelId,
            UnitId = request.UnitId,
            TaxId = request.TaxId,
            CurrencyId = request.CurrencyId,
            Code = code,
            Barcode = NormalizeText(request.Barcode),
            Name = NormalizeText(request.Name),
            Description = NormalizeText(request.Description),
            ItemType = NormalizeText(request.ItemType, "Producto"),
            BasePrice = request.BasePrice,
            BaseCost = request.BaseCost,
            ManagesInventory = request.ManagesInventory,
            UsesLots = request.UsesLots,
            UsesSerials = request.UsesSerials,
            IsSaleItem = request.IsSaleItem,
            IsPurchaseItem = request.IsPurchaseItem,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.Items.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateItemAsync(HttpContext httpContext, Guid id, ItemRequest request, NanchesoftDbContext db)
    {
        var entity = await db.Items.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el producto/servicio." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.Items.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otro producto/servicio con ese código." });

        entity.CategoryId = request.CategoryId;
        entity.BrandId = request.BrandId;
        entity.ModelId = request.ModelId;
        entity.UnitId = request.UnitId;
        entity.TaxId = request.TaxId;
        entity.CurrencyId = request.CurrencyId;
        entity.Code = code;
        entity.Barcode = NormalizeText(request.Barcode, entity.Barcode);
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.Description = NormalizeText(request.Description, entity.Description);
        entity.ItemType = NormalizeText(request.ItemType, entity.ItemType);
        entity.BasePrice = request.BasePrice;
        entity.BaseCost = request.BaseCost;
        entity.ManagesInventory = request.ManagesInventory;
        entity.UsesLots = request.UsesLots;
        entity.UsesSerials = request.UsesSerials;
        entity.IsSaleItem = request.IsSaleItem;
        entity.IsPurchaseItem = request.IsPurchaseItem;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteItemAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Items.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el producto/servicio." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        if (await db.ItemBarcodes.AnyAsync(x => x.ItemId == id) || await db.ItemPriceListDetails.AnyAsync(x => x.ItemId == id))
            return Results.BadRequest(new { message = "No puedes eliminar un producto con códigos de barras o precios asignados." });

        db.Items.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetPriceListsAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.ItemPriceLists.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Company)
            .Include(x => x.Currency)
            .OrderBy(x => x.Code)
            .Select(x => new PriceListDto
            {
                PriceListId = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                CurrencyId = x.CurrencyId,
                CurrencyName = x.Currency != null ? x.Currency.Code : string.Empty,
                Code = x.Code,
                Name = x.Name,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreatePriceListAsync(HttpContext httpContext, PriceListRequest request, NanchesoftDbContext db)
    {
        var context = await ResolveScopedContextAsync(httpContext, db, request.CompanyId);
        var tenantId = context.TenantId;
        var companyId = context.CompanyId;

        if (!tenantId.HasValue || !companyId.HasValue)
            return Results.BadRequest(new { message = "No existe contexto de tenant/empresa para la lista de precios." });

        var code = NormalizeUpper(request.Code);
        if (await db.ItemPriceLists.AnyAsync(x => x.CompanyId == companyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe una lista de precios con ese código." });

        if (request.IsDefault)
        {
            var defaultRows = await db.ItemPriceLists.Where(x => x.CompanyId == companyId && x.IsDefault).ToListAsync();
            foreach (var item in defaultRows)
                item.IsDefault = false;
        }

        var entity = new ItemPriceList
        {
            TenantId = tenantId.Value,
            CompanyId = companyId.Value,
            CurrencyId = request.CurrencyId,
            Code = code,
            Name = NormalizeText(request.Name),
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ItemPriceLists.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdatePriceListAsync(HttpContext httpContext, Guid id, PriceListRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ItemPriceLists.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la lista de precios." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        var code = NormalizeUpper(request.Code, entity.Code);
        if (await db.ItemPriceLists.AnyAsync(x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code))
            return Results.BadRequest(new { message = "Ya existe otra lista de precios con ese código." });

        if (request.IsDefault)
        {
            var defaultRows = await db.ItemPriceLists.Where(x => x.CompanyId == entity.CompanyId && x.Id != id && x.IsDefault).ToListAsync();
            foreach (var item in defaultRows)
                item.IsDefault = false;
        }

        entity.CurrencyId = request.CurrencyId;
        entity.Code = code;
        entity.Name = NormalizeText(request.Name, entity.Name);
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeletePriceListAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ItemPriceLists.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró la lista de precios." });
        if (IsOutOfScope(httpContext, entity.TenantId))
            return Results.Forbid();

        if (await db.Customers.AnyAsync(x => x.PriceListId == id) || await db.ItemPriceListDetails.AnyAsync(x => x.PriceListId == id))
            return Results.BadRequest(new { message = "No puedes eliminar una lista de precios en uso." });

        db.ItemPriceLists.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetBarcodesAsync(HttpContext httpContext, NanchesoftDbContext db)
    {
        var tenantId = ApiTenantScope.ResolveTenantId(httpContext);
        var query = db.ItemBarcodes.AsNoTracking();
        if (!ApiTenantScope.IsPlatformOwner(httpContext) && tenantId.HasValue)
            query = query.Where(x => x.Item != null && x.Item.TenantId == tenantId.Value);

        var rows = await query
            .Include(x => x.Item)
            .OrderBy(x => x.Barcode)
            .Select(x => new BarcodeDto
            {
                BarcodeId = x.Id,
                ItemId = x.ItemId,
                ItemName = x.Item != null ? x.Item.Name : string.Empty,
                Barcode = x.Barcode,
                IsPrimary = x.IsPrimary,
                IsActive = x.IsActive
            }).ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateBarcodeAsync(HttpContext httpContext, BarcodeRequest request, NanchesoftDbContext db)
    {
        if (!request.ItemId.HasValue)
            return Results.BadRequest(new { message = "El producto es obligatorio para asignar el código de barras." });

        var scopedItem = await db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ItemId.Value);
        if (scopedItem is null)
            return Results.BadRequest(new { message = "No se encontró el producto relacionado." });
        if (IsOutOfScope(httpContext, scopedItem.TenantId))
            return Results.Forbid();

        var barcode = NormalizeText(request.Barcode);
        if (await db.ItemBarcodes.AnyAsync(x => x.Barcode == barcode))
            return Results.BadRequest(new { message = "Ya existe ese código de barras." });

        if (request.IsPrimary)
        {
            var existingPrimary = await db.ItemBarcodes.Where(x => x.ItemId == request.ItemId.Value && x.IsPrimary).ToListAsync();
            foreach (var item in existingPrimary)
                item.IsPrimary = false;
        }

        var entity = new ItemBarcode
        {
            ItemId = request.ItemId.Value,
            Barcode = barcode,
            IsPrimary = request.IsPrimary,
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.ItemBarcodes.Add(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateBarcodeAsync(HttpContext httpContext, Guid id, BarcodeRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ItemBarcodes.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el código de barras." });
        if (entity.Item is not null && IsOutOfScope(httpContext, entity.Item.TenantId))
            return Results.Forbid();

        var itemId = request.ItemId ?? entity.ItemId;
        if (!await db.Items.AnyAsync(x => x.Id == itemId))
            return Results.BadRequest(new { message = "No se encontró el producto relacionado." });

        var barcode = NormalizeText(request.Barcode, entity.Barcode);
        if (await db.ItemBarcodes.AnyAsync(x => x.Id != id && x.Barcode == barcode))
            return Results.BadRequest(new { message = "Ya existe ese código de barras." });

        if (request.IsPrimary)
        {
            var existingPrimary = await db.ItemBarcodes.Where(x => x.Id != id && x.ItemId == itemId && x.IsPrimary).ToListAsync();
            foreach (var item in existingPrimary)
                item.IsPrimary = false;
        }

        entity.ItemId = itemId;
        entity.Barcode = barcode;
        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteBarcodeAsync(HttpContext httpContext, Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ItemBarcodes.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Results.NotFound(new { message = "No se encontró el código de barras." });
        if (entity.Item is not null && IsOutOfScope(httpContext, entity.Item.TenantId))
            return Results.Forbid();

        db.ItemBarcodes.Remove(entity);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}

public sealed class CustomerDto
{
    public Guid CustomerId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
    public Guid? PriceListId { get; set; }
    public string PriceListName { get; set; } = string.Empty;
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
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
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
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }
    public string ThirdPartyName { get; set; } = string.Empty;
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
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }
    public string ThirdPartyName { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ExteriorNumber { get; set; } = string.Empty;
    public string InteriorNumber { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public Guid? CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public Guid? StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public Guid? CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BankAccountDto
{
    public Guid BankAccountId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid ThirdPartyId { get; set; }
    public string ThirdPartyName { get; set; } = string.Empty;
    public Guid BankId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CategoryDto
{
    public Guid CategoryId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class BrandDto
{
    public Guid BrandId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class ModelDto
{
    public Guid ModelId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class ItemDto
{
    public Guid ItemId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid? BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public Guid? ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public Guid? TaxId { get; set; }
    public string TaxName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
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
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class BarcodeDto
{
    public Guid BarcodeId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CustomerRequest
{
    public Guid? TenantId { get; set; }
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
    public Guid? TenantId { get; set; }
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
    public Guid? TenantId { get; set; }
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
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string ThirdPartyType { get; set; } = string.Empty;
    public Guid? ThirdPartyId { get; set; }
    public string AddressType { get; set; } = "Principal";
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
    public Guid? TenantId { get; set; }
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
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class BrandRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ModelRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ItemRequest
{
    public Guid? TenantId { get; set; }
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
    public string ItemType { get; set; } = "Producto";
    public decimal BasePrice { get; set; }
    public decimal BaseCost { get; set; }
    public bool ManagesInventory { get; set; } = true;
    public bool UsesLots { get; set; }
    public bool UsesSerials { get; set; }
    public bool IsSaleItem { get; set; } = true;
    public bool IsPurchaseItem { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class PriceListRequest
{
    public Guid? TenantId { get; set; }
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
