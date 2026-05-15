using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Endpoints;

public static class ServiceBillingEndpoints
{
    private const string ServiceNoteDocumentType = "SERVICE_NOTE";
    private const string ServiceNoteReceiptReferencePrefix = "SN:";

    public static IEndpointRouteBuilder MapServiceBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("/api/services/service-notes").WithTags("ServiceNotes");
        notes.MapGet("/", GetServiceNotesAsync);
        notes.MapGet("/lookups", GetServiceModuleLookupsAsync);
        notes.MapPost("/", CreateServiceNoteAsync);
        notes.MapPut("/{id:guid}", UpdateServiceNoteAsync);
        notes.MapDelete("/{id:guid}", DeleteServiceNoteAsync);

        var catalog = app.MapGroup("/api/services/catalog").WithTags("ServiceCatalog");
        catalog.MapGet("/", GetServiceCatalogAsync);
        catalog.MapGet("/lookups", GetServiceModuleLookupsAsync);
        catalog.MapPost("/", CreateServiceCatalogItemAsync);
        catalog.MapPut("/{id:guid}", UpdateServiceCatalogItemAsync);
        catalog.MapDelete("/{id:guid}", DeleteServiceCatalogItemAsync);

        var rates = app.MapGroup("/api/services/customer-rates").WithTags("CustomerServiceRates");
        rates.MapGet("/", GetCustomerServiceRatesAsync);
        rates.MapGet("/lookups", GetServiceModuleLookupsAsync);
        rates.MapPost("/", CreateCustomerServiceRateAsync);
        rates.MapPut("/{id:guid}", UpdateCustomerServiceRateAsync);
        rates.MapDelete("/{id:guid}", DeleteCustomerServiceRateAsync);

        return app;
    }

    private static async Task<IResult> GetServiceNotesAsync(NanchesoftDbContext db)
    {
        var notes = await db.ServiceNotes
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Company)
            .Include(x => x.Customer)
            .Include(x => x.ServiceCatalogItem)
            .OrderByDescending(x => x.NoteDate)
            .ThenByDescending(x => x.Folio)
            .Select(x => new ServiceNoteListItemDto
            {
                ServiceNoteId = x.Id,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.Name : x.CustomerNameSnapshot,
                ServiceCatalogItemId = x.ServiceCatalogItemId,
                ServiceCode = x.ServiceCatalogItem != null ? x.ServiceCatalogItem.Code : x.ServiceCodeSnapshot,
                ServiceName = x.ServiceCatalogItem != null ? x.ServiceCatalogItem.Name : x.ServiceNameSnapshot,
                Folio = x.Folio,
                NoteDate = x.NoteDate,
                Description = x.Description,
                StartTimeText = x.StartTimeText ?? string.Empty,
                EndTimeText = x.EndTimeText ?? string.Empty,
                BreakMinutes = x.BreakMinutes,
                HoursWorked = x.HoursWorked,
                HourlyRate = x.HourlyRate,
                Subtotal = x.Subtotal,
                Total = x.Total,
                PaymentStatus = x.PaymentStatus,
                PaymentMethod = x.PaymentMethod,
                PaymentDate = x.PaymentDate,
                PaymentDestination = x.PaymentDestination ?? string.Empty,
                PaymentReference = x.PaymentReference ?? string.Empty,
                Notes = x.Notes ?? string.Empty,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(notes);
    }

    private static async Task<IResult> GetServiceCatalogAsync(NanchesoftDbContext db)
    {
        var rows = await db.ServiceCatalogItems
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Company)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Select(x => new ServiceCatalogItemListItemDto
            {
                ServiceCatalogItemId = x.Id,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                BillingUnit = x.BillingUnit,
                DefaultRate = x.DefaultRate,
                Notes = x.Notes ?? string.Empty,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetCustomerServiceRatesAsync(NanchesoftDbContext db)
    {
        var rows = await db.CustomerServiceRates
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Company)
            .Include(x => x.Customer)
            .Include(x => x.ServiceCatalogItem)
            .Include(x => x.Currency)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.Customer != null ? x.Customer.Name : string.Empty)
            .ThenBy(x => x.ServiceCatalogItem != null ? x.ServiceCatalogItem.Name : string.Empty)
            .Select(x => new CustomerServiceRateListItemDto
            {
                CustomerServiceRateId = x.Id,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                ServiceCatalogItemId = x.ServiceCatalogItemId,
                ServiceCode = x.ServiceCatalogItem != null ? x.ServiceCatalogItem.Code : string.Empty,
                ServiceName = x.ServiceCatalogItem != null ? x.ServiceCatalogItem.Name : string.Empty,
                CurrencyId = x.CurrencyId,
                CurrencyCode = x.Currency != null ? x.Currency.Code : string.Empty,
                Rate = x.Rate,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                Notes = x.Notes ?? string.Empty,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetServiceModuleLookupsAsync(NanchesoftDbContext db)
    {
        var tenants = await db.Tenants
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ServiceTenantLookupDto
            {
                TenantId = x.Id,
                TenantName = x.Name
            })
            .ToListAsync();

        var companies = await db.Companies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ServiceCompanyLookupDto
            {
                CompanyId = x.Id,
                CompanyName = x.Name,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty
            })
            .ToListAsync();

        var customers = await db.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ServiceCustomerLookupDto
            {
                CustomerId = x.Id,
                CustomerName = x.Name,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty
            })
            .ToListAsync();

        var services = await db.ServiceCatalogItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Select(x => new ServiceCatalogLookupDto
            {
                ServiceCatalogItemId = x.Id,
                ServiceCode = x.Code,
                ServiceName = x.Name,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                TenantId = x.TenantId,
                TenantName = x.Tenant != null ? x.Tenant.Name : string.Empty,
                BillingUnit = x.BillingUnit,
                DefaultRate = x.DefaultRate
            })
            .ToListAsync();

        var currencies = await db.Currencies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new ServiceCurrencyLookupDto
            {
                CurrencyId = x.Id,
                CurrencyCode = x.Code,
                CurrencyName = x.Name
            })
            .ToListAsync();

        var paymentDestinations = new List<ServicePaymentDestinationLookupDto>();

        paymentDestinations.AddRange(await db.CashAccounts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ServicePaymentDestinationLookupDto
            {
                DestinationValue = $"CAJA | {x.Code} | {x.Name}",
                DestinationLabel = $"Caja · {x.Code} · {x.Name}"
            })
            .ToListAsync());

        paymentDestinations.AddRange(await db.BankAccounts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ServicePaymentDestinationLookupDto
            {
                DestinationValue = $"BANCO | {x.Code} | {x.Name} | {x.AccountNumber}",
                DestinationLabel = $"Banco · {x.Code} · {x.Name} · {x.AccountNumber}"
            })
            .ToListAsync());

        return Results.Ok(new ServiceModuleLookupBundleDto
        {
            Tenants = tenants,
            Companies = companies,
            Customers = customers,
            Services = services,
            Currencies = currencies,
            PaymentDestinations = paymentDestinations
        });
    }

    private static async Task<IResult> CreateServiceCatalogItemAsync(CreateOrUpdateServiceCatalogItemRequest request, NanchesoftDbContext db)
    {
        var company = await ResolveCompanyAsync(db, request.CompanyId, request.TenantId);
        if (company is null)
        {
            return Results.BadRequest(new { message = "No se encontró la empresa seleccionada." });
        }

        var code = NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(code))
        {
            return Results.BadRequest(new { message = "El código del servicio es obligatorio." });
        }

        if (await db.ServiceCatalogItems.AnyAsync(x => x.CompanyId == company.Id && x.Code == code))
        {
            return Results.BadRequest(new { message = "Ya existe un servicio con ese código dentro de la misma empresa." });
        }

        var entity = new ServiceCatalogItem
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            Code = code,
            Name = NormalizeLabel(request.Name),
            Description = NormalizeText(request.Description),
            BillingUnit = NormalizeBillingUnit(request.BillingUnit),
            DefaultRate = request.DefaultRate < 0 ? 0 : request.DefaultRate,
            Notes = NormalizeNullableText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            return Results.BadRequest(new { message = "El nombre del servicio es obligatorio." });
        }

        db.ServiceCatalogItems.Add(entity);
        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateServiceCatalogItemAsync(Guid id, CreateOrUpdateServiceCatalogItemRequest request, NanchesoftDbContext db)
    {
        var entity = await db.ServiceCatalogItems.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return Results.NotFound(new { message = "No se encontró el servicio." });
        }

        var company = await ResolveCompanyAsync(db, request.CompanyId ?? entity.CompanyId, request.TenantId ?? entity.TenantId);
        if (company is null)
        {
            return Results.BadRequest(new { message = "No se encontró la empresa seleccionada." });
        }

        var code = string.IsNullOrWhiteSpace(request.Code) ? entity.Code : NormalizeCode(request.Code);
        if (await db.ServiceCatalogItems.AnyAsync(x => x.Id != id && x.CompanyId == company.Id && x.Code == code))
        {
            return Results.BadRequest(new { message = "Ya existe otro servicio con ese código dentro de la misma empresa." });
        }

        entity.TenantId = company.TenantId;
        entity.CompanyId = company.Id;
        entity.Code = code;
        entity.Name = string.IsNullOrWhiteSpace(request.Name) ? entity.Name : NormalizeLabel(request.Name);
        entity.Description = request.Description is null ? entity.Description : NormalizeText(request.Description);
        entity.BillingUnit = string.IsNullOrWhiteSpace(request.BillingUnit) ? entity.BillingUnit : NormalizeBillingUnit(request.BillingUnit);
        entity.DefaultRate = request.DefaultRate < 0 ? entity.DefaultRate : request.DefaultRate;
        entity.Notes = request.Notes is null ? entity.Notes : NormalizeNullableText(request.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            return Results.BadRequest(new { message = "El nombre del servicio es obligatorio." });
        }

        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteServiceCatalogItemAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ServiceCatalogItems.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return Results.NotFound(new { message = "No se encontró el servicio." });
        }

        var inUse = await db.CustomerServiceRates.AnyAsync(x => x.ServiceCatalogItemId == id)
                    || await db.ServiceNotes.AnyAsync(x => x.ServiceCatalogItemId == id);
        if (inUse)
        {
            return Results.BadRequest(new { message = "No se puede eliminar el servicio porque ya tiene tarifas o notas asociadas." });
        }

        db.ServiceCatalogItems.Remove(entity);
        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> CreateCustomerServiceRateAsync(CreateOrUpdateCustomerServiceRateRequest request, NanchesoftDbContext db)
    {
        var resolution = await ResolveRateDependenciesAsync(db, request.CompanyId, request.TenantId, request.CustomerId, request.ServiceCatalogItemId, request.CurrencyId);
        if (!resolution.Success)
        {
            return resolution.Error!;
        }

        var effectiveFrom = EnsureUtcDate(request.EffectiveFrom) ?? UtcToday();
        var effectiveTo = EnsureUtcDate(request.EffectiveTo);
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            return Results.BadRequest(new { message = "La vigencia final no puede ser menor a la vigencia inicial." });
        }

        if (await db.CustomerServiceRates.AnyAsync(x => x.CompanyId == resolution.Company!.Id && x.CustomerId == resolution.Customer!.Id && x.ServiceCatalogItemId == resolution.Service!.Id && x.EffectiveFrom == effectiveFrom))
        {
            return Results.BadRequest(new { message = "Ya existe una tarifa con la misma vigencia inicial para ese cliente y servicio." });
        }

        var entity = new CustomerServiceRate
        {
            TenantId = resolution.Company!.TenantId,
            CompanyId = resolution.Company.Id,
            CustomerId = resolution.Customer!.Id,
            ServiceCatalogItemId = resolution.Service!.Id,
            CurrencyId = resolution.Currency?.Id,
            Rate = request.Rate < 0 ? 0 : request.Rate,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            Notes = NormalizeNullableText(request.Notes),
            IsActive = request.IsActive,
            CreatedBy = "web-api"
        };

        db.CustomerServiceRates.Add(entity);
        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = entity.Id });
    }

    private static async Task<IResult> UpdateCustomerServiceRateAsync(Guid id, CreateOrUpdateCustomerServiceRateRequest request, NanchesoftDbContext db)
    {
        var entity = await db.CustomerServiceRates.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return Results.NotFound(new { message = "No se encontró la tarifa por cliente." });
        }

        var resolution = await ResolveRateDependenciesAsync(
            db,
            request.CompanyId ?? entity.CompanyId,
            request.TenantId ?? entity.TenantId,
            request.CustomerId ?? entity.CustomerId,
            request.ServiceCatalogItemId ?? entity.ServiceCatalogItemId,
            request.CurrencyId ?? entity.CurrencyId);

        if (!resolution.Success)
        {
            return resolution.Error!;
        }

        var effectiveFrom = request.EffectiveFrom.HasValue ? EnsureUtcDate(request.EffectiveFrom)!.Value : entity.EffectiveFrom;
        var effectiveTo = request.EffectiveTo.HasValue ? EnsureUtcDate(request.EffectiveTo) : entity.EffectiveTo;
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            return Results.BadRequest(new { message = "La vigencia final no puede ser menor a la vigencia inicial." });
        }

        var duplicate = await db.CustomerServiceRates.AnyAsync(x => x.Id != id && x.CompanyId == resolution.Company!.Id && x.CustomerId == resolution.Customer!.Id && x.ServiceCatalogItemId == resolution.Service!.Id && x.EffectiveFrom == effectiveFrom);
        if (duplicate)
        {
            return Results.BadRequest(new { message = "Ya existe otra tarifa con la misma vigencia inicial para ese cliente y servicio." });
        }

        entity.TenantId = resolution.Company!.TenantId;
        entity.CompanyId = resolution.Company.Id;
        entity.CustomerId = resolution.Customer!.Id;
        entity.ServiceCatalogItemId = resolution.Service!.Id;
        entity.CurrencyId = resolution.Currency?.Id;
        entity.Rate = request.Rate < 0 ? entity.Rate : request.Rate;
        entity.EffectiveFrom = effectiveFrom;
        entity.EffectiveTo = effectiveTo;
        entity.Notes = request.Notes is null ? entity.Notes : NormalizeNullableText(request.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "web-api";

        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteCustomerServiceRateAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.CustomerServiceRates.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return Results.NotFound(new { message = "No se encontró la tarifa por cliente." });
        }

        db.CustomerServiceRates.Remove(entity);
        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> CreateServiceNoteAsync(CreateOrUpdateServiceNoteRequest request, NanchesoftDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var resolution = await ResolveNoteDependenciesAsync(db, request.CompanyId, request.TenantId, request.CustomerId, request.ServiceCatalogItemId);
            if (!resolution.Success)
            {
                return resolution.Error!;
            }

            var company = resolution.Company!;
            var customer = resolution.Customer;
            var service = resolution.Service;
            var noteDate = EnsureUtcDate(request.NoteDate) ?? UtcToday();
            var requestedFolio = NormalizeCode(request.Folio);

            if (!string.IsNullOrWhiteSpace(requestedFolio) && await db.ServiceNotes.AnyAsync(x => x.CompanyId == company.Id && x.Folio == requestedFolio))
            {
                return Results.BadRequest(new { message = "Ya existe una nota de servicio con ese folio dentro de la misma empresa." });
            }

            var entity = new ServiceNote
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                CustomerId = customer?.Id,
                ServiceCatalogItemId = service?.Id,
                CustomerNameSnapshot = customer?.Name ?? string.Empty,
                ServiceCodeSnapshot = service?.Code ?? string.Empty,
                ServiceNameSnapshot = service?.Name ?? string.Empty,
                Folio = await ResolveFolioAsync(db, company, request.Folio),
                NoteDate = noteDate,
                Description = ResolveDescription(request.Description, service),
                StartTimeText = NormalizeNullableText(request.StartTimeText),
                EndTimeText = NormalizeNullableText(request.EndTimeText),
                BreakMinutes = request.BreakMinutes < 0 ? 0 : request.BreakMinutes,
                HoursWorked = request.HoursWorked < 0 ? 0 : request.HoursWorked,
                HourlyRate = request.HourlyRate < 0 ? 0 : request.HourlyRate,
                PaymentStatus = NormalizePaymentStatus(request.PaymentStatus),
                PaymentMethod = NormalizePaymentMethod(request.PaymentMethod),
                PaymentDate = EnsureUtcDate(request.PaymentDate),
                PaymentDestination = NormalizeNullableText(request.PaymentDestination),
                PaymentReference = NormalizeNullableText(request.PaymentReference),
                Notes = NormalizeNullableText(request.Notes),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "web-api"
            };

            entity.NoteDate = NormalizeBusinessDateUtc(entity.NoteDate);
            entity.CreatedAt = NormalizeUtcDateTime(entity.CreatedAt);
            entity.PaymentDate = EnsureUtcDate(entity.PaymentDate);

            await ApplyCalculatedFieldsAsync(db, entity, entity.NoteDate, customer?.Id, service);
            db.ServiceNotes.Add(entity);
            NormalizeTrackedDateTimesToUtc(db);
            await db.SaveChangesAsync();
            await EnsureServiceNoteCollectionAsync(db, entity);
            await transaction.CommitAsync();
            return Results.Ok(new { success = true, id = entity.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BuildServiceNoteWriteError(ex, db);
        }
    }

    private static async Task<IResult> UpdateServiceNoteAsync(Guid id, CreateOrUpdateServiceNoteRequest request, NanchesoftDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var entity = await db.ServiceNotes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null)
            {
                return Results.NotFound(new { message = "No se encontró la nota de servicio." });
            }

            var resolution = await ResolveNoteDependenciesAsync(
                db,
                request.CompanyId ?? entity.CompanyId,
                request.TenantId ?? entity.TenantId,
                request.CustomerId ?? entity.CustomerId,
                request.ServiceCatalogItemId ?? entity.ServiceCatalogItemId);

            if (!resolution.Success)
            {
                return resolution.Error!;
            }

            var company = resolution.Company!;
            var customer = resolution.Customer;
            var service = resolution.Service;

            entity.CreatedAt = NormalizeUtcDateTime(entity.CreatedAt);
            entity.NoteDate = NormalizeBusinessDateUtc(entity.NoteDate);
            entity.PaymentDate = EnsureUtcDate(entity.PaymentDate);
            entity.UpdatedAt = entity.UpdatedAt.HasValue ? NormalizeUtcDateTime(entity.UpdatedAt.Value) : entity.UpdatedAt;

            var requestedFolio = NormalizeCode(request.Folio);

            if (!string.IsNullOrWhiteSpace(requestedFolio))
            {
                var duplicate = await db.ServiceNotes.AnyAsync(x => x.Id != id && x.CompanyId == company.Id && x.Folio == requestedFolio);
                if (duplicate)
                {
                    return Results.BadRequest(new { message = "Ya existe otra nota de servicio con ese folio dentro de la misma empresa." });
                }

                entity.Folio = requestedFolio;
            }

            entity.TenantId = company.TenantId;
            entity.CompanyId = company.Id;
            entity.CustomerId = customer?.Id;
            entity.ServiceCatalogItemId = service?.Id;
            entity.CustomerNameSnapshot = customer?.Name ?? entity.CustomerNameSnapshot;
            entity.ServiceCodeSnapshot = service?.Code ?? entity.ServiceCodeSnapshot;
            entity.ServiceNameSnapshot = service?.Name ?? entity.ServiceNameSnapshot;
            entity.NoteDate = request.NoteDate.HasValue ? EnsureUtcDate(request.NoteDate)!.Value : entity.NoteDate;
            entity.Description = request.Description is null ? ResolveDescription(entity.Description, service) : ResolveDescription(request.Description, service, entity.Description);
            entity.StartTimeText = request.StartTimeText is null ? entity.StartTimeText : NormalizeNullableText(request.StartTimeText);
            entity.EndTimeText = request.EndTimeText is null ? entity.EndTimeText : NormalizeNullableText(request.EndTimeText);
            entity.BreakMinutes = request.BreakMinutes < 0 ? entity.BreakMinutes : request.BreakMinutes;
            entity.HoursWorked = request.HoursWorked < 0 ? entity.HoursWorked : request.HoursWorked;
            entity.HourlyRate = request.HourlyRate < 0 ? entity.HourlyRate : request.HourlyRate;
            entity.PaymentStatus = NormalizePaymentStatus(request.PaymentStatus, entity.PaymentStatus);
            entity.PaymentMethod = NormalizePaymentMethod(request.PaymentMethod, entity.PaymentMethod);
            entity.PaymentDate = request.PaymentDate.HasValue ? EnsureUtcDate(request.PaymentDate) : entity.PaymentDate;
            entity.PaymentDestination = request.PaymentDestination is null ? entity.PaymentDestination : NormalizeNullableText(request.PaymentDestination);
            entity.PaymentReference = request.PaymentReference is null ? entity.PaymentReference : NormalizeNullableText(request.PaymentReference);
            entity.Notes = request.Notes is null ? entity.Notes : NormalizeNullableText(request.Notes);
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = NormalizeUtcDateTime(DateTime.UtcNow);
            entity.UpdatedBy = "web-api";

            entity.NoteDate = NormalizeBusinessDateUtc(entity.NoteDate);
            entity.CreatedAt = NormalizeUtcDateTime(entity.CreatedAt);
            entity.PaymentDate = EnsureUtcDate(entity.PaymentDate);
            entity.UpdatedAt = entity.UpdatedAt.HasValue ? NormalizeUtcDateTime(entity.UpdatedAt.Value) : entity.UpdatedAt;

            await ApplyCalculatedFieldsAsync(db, entity, entity.NoteDate, customer?.Id, service);
            NormalizeTrackedDateTimesToUtc(db);
            await db.SaveChangesAsync();
            await EnsureServiceNoteCollectionAsync(db, entity);
            await transaction.CommitAsync();
            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BuildServiceNoteWriteError(ex, db);
        }
    }

    private static IResult BuildServiceNoteWriteError(Exception ex, NanchesoftDbContext db)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        if (ex is DbUpdateException dbUpdate && dbUpdate.InnerException is not null)
        {
            message = dbUpdate.InnerException.Message;
        }

        message = string.IsNullOrWhiteSpace(message)
            ? "Ocurrió un error al guardar la nota de servicio."
            : message;

        if (message.Contains("Cannot write DateTime with Kind=Unspecified", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timestamp with time zone", StringComparison.OrdinalIgnoreCase))
        {
            var pendingDates = DescribeUnspecifiedDateTimes(db);
            message = string.IsNullOrWhiteSpace(pendingDates)
                ? "La nota de servicio todavía contiene una fecha sin zona horaria válida para PostgreSQL. Este hotfix fuerza todas las fechas del guardado a UTC antes de grabar. Vuelve a intentar guardar la nota."
                : $"La nota de servicio todavía contiene fechas sin zona horaria válida para PostgreSQL. Revisa estos campos: {pendingDates}.";
        }

        return Results.BadRequest(new { message });
    }

    private static async Task<IResult> DeleteServiceNoteAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.ServiceNotes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return Results.NotFound(new { message = "No se encontró la nota de servicio." });
        }

        db.ServiceNotes.Remove(entity);
        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task ApplyCalculatedFieldsAsync(NanchesoftDbContext db, ServiceNote entity, DateTime noteDate, Guid? customerId, ServiceCatalogItem? service)
    {
        if (TryCalculateHours(entity.StartTimeText, entity.EndTimeText, entity.BreakMinutes, out var calculatedHours))
        {
            entity.HoursWorked = calculatedHours;
        }

        if (entity.HourlyRate <= 0)
        {
            entity.HourlyRate = await ResolveHourlyRateAsync(db, entity.CompanyId, customerId, service?.Id, noteDate, service?.DefaultRate ?? 0);
        }

        entity.Subtotal = Math.Round(entity.HoursWorked * entity.HourlyRate, 2, MidpointRounding.AwayFromZero);
        entity.Total = entity.Subtotal;

        if (!IsPaidStatus(entity.PaymentStatus))
        {
            entity.PaymentDate = null;
        }
        else if (!entity.PaymentDate.HasValue)
        {
            entity.PaymentDate = noteDate;
        }
    }

    private static async Task EnsureServiceNoteCollectionAsync(NanchesoftDbContext db, ServiceNote entity)
    {
        if (!IsPaidStatus(entity.PaymentStatus))
        {
            return;
        }

        var paymentDate = EnsureUtcDate(entity.PaymentDate) ?? EnsureUtcDate(entity.NoteDate) ?? UtcToday();
        entity.PaymentDate = paymentDate;

        var company = await db.Companies.AsNoTracking().FirstAsync(x => x.Id == entity.CompanyId);
        var branch = await db.Branches.AsNoTracking()
            .Where(x => x.CompanyId == entity.CompanyId && x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (branch is null)
        {
            throw new InvalidOperationException("No hay sucursal activa disponible para registrar el cobro de la nota de servicio.");
        }

        var target = await ResolvePaymentTargetAsync(db, entity);
        var marker = BuildServiceNoteReceiptMarker(entity.Id);
        var receipt = await db.Receipts
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CompanyId == entity.CompanyId && x.Reference.StartsWith(marker));

        var receiptReference = BuildServiceNoteReceiptReference(entity);
        var receiptLineDescription = BuildServiceNoteReceiptLineDescription(entity);
        var amount = Math.Round(entity.Total, 2, MidpointRounding.AwayFromZero);

        if (receipt is null)
        {
            receipt = new Receipt
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                BranchId = branch.Id,
                CustomerId = entity.CustomerId,
                CurrencyId = target.CurrencyId ?? await GetDefaultCurrencyIdAsync(db),
                CashAccountId = target.TargetType == "cash" ? target.CashAccountId : null,
                BankAccountId = target.TargetType == "bank" ? target.BankAccountId : null,
                Folio = await ResolveServiceNoteReceiptFolioAsync(db, entity.CompanyId, entity.Folio),
                ReceiptDate = paymentDate,
                TargetType = target.TargetType,
                ExchangeRate = 1m,
                Status = "approved",
                Reference = receiptReference,
                Total = amount,
                ApprovedAt = DateTime.UtcNow,
                PostedAt = null,
                IsActive = true,
                CreatedBy = "service-note-payment",
                Lines =
                [
                    new ReceiptLine
                    {
                        LineNumber = 1,
                        Description = receiptLineDescription,
                        Amount = amount,
                        CreatedBy = "service-note-payment"
                    }
                ]
            };

            db.Receipts.Add(receipt);
            NormalizeTrackedDateTimesToUtc(db);
            await db.SaveChangesAsync();
            await PostReceiptAsync(receipt.Id, db);
            return;
        }

        var sameTarget = receipt.TargetType == target.TargetType
            && receipt.CashAccountId == (target.TargetType == "cash" ? target.CashAccountId : null)
            && receipt.BankAccountId == (target.TargetType == "bank" ? target.BankAccountId : null);
        var sameAmount = receipt.Total == amount;
        var sameDate = receipt.ReceiptDate.Date == paymentDate.Date;
        var sameCustomer = receipt.CustomerId == entity.CustomerId;

        if (receipt.Status == "posted" || receipt.PostedAt.HasValue)
        {
            if (sameTarget && sameAmount && sameDate && sameCustomer)
            {
                if (receipt.Reference != receiptReference)
                {
                    receipt.Reference = receiptReference;
                    receipt.UpdatedAt = DateTime.UtcNow;
                    receipt.UpdatedBy = "service-note-payment";
                    NormalizeTrackedDateTimesToUtc(db);
                    await db.SaveChangesAsync();
                }

                return;
            }

            throw new InvalidOperationException("La nota ya tiene un cobro registrado en Tesorería. Si necesitas cambiar importe, fecha o destino, edita primero el recibo generado en Tesorería > Recibos.");
        }

        receipt.CustomerId = entity.CustomerId;
        receipt.CurrencyId = target.CurrencyId ?? receipt.CurrencyId;
        receipt.CashAccountId = target.TargetType == "cash" ? target.CashAccountId : null;
        receipt.BankAccountId = target.TargetType == "bank" ? target.BankAccountId : null;
        receipt.ReceiptDate = paymentDate;
        receipt.TargetType = target.TargetType;
        receipt.Reference = receiptReference;
        receipt.Total = amount;
        receipt.Status = "approved";
        receipt.ApprovedAt ??= DateTime.UtcNow;
        receipt.PostedAt = null;
        receipt.UpdatedAt = DateTime.UtcNow;
        receipt.UpdatedBy = "service-note-payment";

        receipt.Lines.Clear();
        receipt.Lines.Add(new ReceiptLine
        {
            LineNumber = 1,
            Description = receiptLineDescription,
            Amount = amount,
            CreatedBy = "service-note-payment"
        });

        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
        await PostReceiptAsync(receipt.Id, db);
    }


    private static async Task PostReceiptAsync(Guid id, NanchesoftDbContext db)
    {
        var entity = await db.Receipts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            throw new InvalidOperationException("No se encontró el recibo generado para la nota de servicio.");
        }

        if (entity.PostedAt.HasValue || entity.Status == "posted")
        {
            return;
        }

        entity.Total = entity.Lines.Sum(x => x.Amount);
        entity.Status = "posted";
        entity.PostedAt = DateTime.UtcNow;
        entity.ApprovedAt ??= DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = "service-note-payment";

        if (entity.TargetType == "bank")
        {
            var account = await db.BankAccounts.FirstAsync(x => x.Id == entity.BankAccountId);
            account.CurrentBalance += entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "service-note-payment";

            db.BankMovements.Add(new BankMovement
            {
                TenantId = entity.TenantId,
                CompanyId = entity.CompanyId,
                BankAccountId = account.Id,
                MovementDate = entity.PostedAt.Value,
                MovementType = "receipt",
                DocumentType = "receipt",
                DocumentId = entity.Id,
                Reference = entity.Reference,
                AmountIn = entity.Total,
                AmountOut = 0m,
                BalanceAfter = account.CurrentBalance,
                CreatedBy = "service-note-payment"
            });
        }
        else
        {
            var account = await db.CashAccounts.FirstAsync(x => x.Id == entity.CashAccountId);
            account.CurrentBalance += entity.Total;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = "service-note-payment";

            db.CashMovements.Add(new CashMovement
            {
                TenantId = entity.TenantId,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                CashAccountId = account.Id,
                MovementDate = entity.PostedAt.Value,
                MovementType = "receipt",
                DocumentType = "receipt",
                DocumentId = entity.Id,
                Reference = entity.Reference,
                AmountIn = entity.Total,
                AmountOut = 0m,
                BalanceAfter = account.CurrentBalance,
                CreatedBy = "service-note-payment"
            });
        }

        NormalizeTrackedDateTimesToUtc(db);
        await db.SaveChangesAsync();
    }

    private static bool IsPaidStatus(string? value)
    {
        var normalized = NormalizeCode(value);
        return normalized is "PAGADA" or "PAGADO" or "COBRADA" or "COBRADO";
    }

    private static async Task<ServiceNotePaymentTarget> ResolvePaymentTargetAsync(NanchesoftDbContext db, ServiceNote entity)
    {
        var method = NormalizePaymentMethod(entity.PaymentMethod);
        var parsed = ParsePaymentDestination(entity.PaymentDestination);
        var targetType = parsed.TargetType;

        if (string.IsNullOrWhiteSpace(targetType))
        {
            targetType = method == "EFECTIVO" ? "cash" : "bank";
        }

        if (targetType == "cash")
        {
            var cashAccounts = await db.CashAccounts
                .AsNoTracking()
                .Where(x => x.CompanyId == entity.CompanyId && x.IsActive)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            CashAccount? cash = null;

            if (!string.IsNullOrWhiteSpace(parsed.Code))
            {
                var code = NormalizeCode(parsed.Code);
                cash = cashAccounts.FirstOrDefault(x => NormalizeCode(x.Code) == code);
            }

            cash ??= cashAccounts.FirstOrDefault();
            if (cash is null)
            {
                throw new InvalidOperationException("No hay una caja activa disponible para registrar el cobro de la nota de servicio.");
            }

            return new ServiceNotePaymentTarget("cash", cash.Id, null, cash.CurrencyId);
        }

        var bankAccounts = await db.BankAccounts
            .AsNoTracking()
            .Where(x => x.CompanyId == entity.CompanyId && x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        BankAccount? bank = null;

        if (!string.IsNullOrWhiteSpace(parsed.Code))
        {
            var code = NormalizeCode(parsed.Code);
            bank = bankAccounts.FirstOrDefault(x => NormalizeCode(x.Code) == code);
        }

        if (bank is null && !string.IsNullOrWhiteSpace(parsed.AccountNumber))
        {
            var acct = NormalizeCode(parsed.AccountNumber);
            bank = bankAccounts.FirstOrDefault(x => NormalizeCode(x.AccountNumber) == acct || NormalizeCode(x.Clabe) == acct);
        }

        bank ??= bankAccounts.FirstOrDefault();
        if (bank is null)
        {
            throw new InvalidOperationException("No hay una cuenta bancaria activa disponible para registrar el cobro de la nota de servicio.");
        }

        return new ServiceNotePaymentTarget("bank", null, bank.Id, bank.CurrencyId);
    }

    private static ServiceNotePaymentDestination ParsePaymentDestination(string? value)
    {
        var parts = (value ?? string.Empty)
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return new ServiceNotePaymentDestination(null, null, null);
        }

        var targetType = NormalizeCode(parts[0]) switch
        {
            "CAJA" => "cash",
            "BANCO" => "bank",
            _ => null
        };

        var code = parts.Length > 1 ? parts[1] : null;
        var accountNumber = parts.Length > 3 ? parts[3] : null;
        return new ServiceNotePaymentDestination(targetType, code, accountNumber);
    }

    private static string BuildServiceNoteReceiptMarker(Guid serviceNoteId)
        => $"{ServiceNoteReceiptReferencePrefix}{serviceNoteId:D}";

    private static string BuildServiceNoteReceiptReference(ServiceNote entity)
    {
        var marker = BuildServiceNoteReceiptMarker(entity.Id);
        var paymentReference = NormalizeText(entity.PaymentReference);
        return string.IsNullOrWhiteSpace(paymentReference)
            ? $"{marker} · Nota {entity.Folio}"
            : $"{marker} · Nota {entity.Folio} · {paymentReference}";
    }

    private static string BuildServiceNoteReceiptLineDescription(ServiceNote entity)
    {
        var customer = NormalizeText(entity.CustomerNameSnapshot);
        var description = NormalizeText(entity.Description);

        if (!string.IsNullOrWhiteSpace(description))
        {
            return $"Cobro nota de servicio {entity.Folio} · {description}";
        }

        return string.IsNullOrWhiteSpace(customer)
            ? $"Cobro nota de servicio {entity.Folio}"
            : $"Cobro nota de servicio {entity.Folio} · {customer}";
    }

    private static async Task<string> ResolveServiceNoteReceiptFolioAsync(NanchesoftDbContext db, Guid companyId, string serviceNoteFolio)
    {
        var baseFolio = NormalizeCode($"COB-{serviceNoteFolio}");
        var folio = baseFolio;
        var sequence = 2;

        while (await db.Receipts.AnyAsync(x => x.CompanyId == companyId && x.Folio == folio))
        {
            folio = $"{baseFolio}-{sequence}";
            sequence++;
        }

        return folio;
    }

    private static async Task<decimal> ResolveHourlyRateAsync(NanchesoftDbContext db, Guid companyId, Guid? customerId, Guid? serviceCatalogItemId, DateTime noteDate, decimal fallbackRate)
    {
        if (customerId.HasValue && serviceCatalogItemId.HasValue)
        {
            var rate = await db.CustomerServiceRates
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId
                         && x.CustomerId == customerId.Value
                         && x.ServiceCatalogItemId == serviceCatalogItemId.Value
                         && x.IsActive
                         && x.EffectiveFrom <= noteDate
                         && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= noteDate))
                .OrderByDescending(x => x.EffectiveFrom)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => x.Rate)
                .FirstOrDefaultAsync();

            if (rate > 0)
            {
                return rate;
            }
        }

        return fallbackRate < 0 ? 0 : fallbackRate;
    }

    private static async Task<(bool Success, Company? Company, Customer? Customer, ServiceCatalogItem? Service, Currency? Currency, IResult? Error)> ResolveRateDependenciesAsync(
        NanchesoftDbContext db,
        Guid? companyId,
        Guid? tenantId,
        Guid? customerId,
        Guid? serviceCatalogItemId,
        Guid? currencyId)
    {
        var company = await ResolveCompanyAsync(db, companyId, tenantId);
        if (company is null)
        {
            return (false, null, null, null, null, Results.BadRequest(new { message = "No se encontró la empresa seleccionada." }));
        }

        var customer = customerId.HasValue && customerId.Value != Guid.Empty
            ? await db.Customers.FirstOrDefaultAsync(x => x.Id == customerId.Value && x.CompanyId == company.Id && x.IsActive)
            : null;
        if (customer is null)
        {
            return (false, null, null, null, null, Results.BadRequest(new { message = "No se encontró el cliente seleccionado." }));
        }

        var service = serviceCatalogItemId.HasValue && serviceCatalogItemId.Value != Guid.Empty
            ? await db.ServiceCatalogItems.FirstOrDefaultAsync(x => x.Id == serviceCatalogItemId.Value && x.CompanyId == company.Id && x.IsActive)
            : null;
        if (service is null)
        {
            return (false, null, null, null, null, Results.BadRequest(new { message = "No se encontró el servicio seleccionado." }));
        }

        Currency? currency = null;
        if (currencyId.HasValue && currencyId.Value != Guid.Empty)
        {
            currency = await db.Currencies.FirstOrDefaultAsync(x => x.Id == currencyId.Value && x.IsActive);
            if (currency is null)
            {
                return (false, null, null, null, null, Results.BadRequest(new { message = "No se encontró la moneda seleccionada." }));
            }
        }

        return (true, company, customer, service, currency, null);
    }

    private static async Task<(bool Success, Company? Company, Customer? Customer, ServiceCatalogItem? Service, IResult? Error)> ResolveNoteDependenciesAsync(
        NanchesoftDbContext db,
        Guid? companyId,
        Guid? tenantId,
        Guid? customerId,
        Guid? serviceCatalogItemId)
    {
        var company = await ResolveCompanyAsync(db, companyId, tenantId);
        if (company is null)
        {
            return (false, null, null, null, Results.BadRequest(new { message = "No se encontró la empresa seleccionada." }));
        }

        Customer? customer = null;
        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == customerId.Value && x.CompanyId == company.Id && x.IsActive);
            if (customer is null)
            {
                return (false, null, null, null, Results.BadRequest(new { message = "No se encontró el cliente seleccionado." }));
            }
        }

        ServiceCatalogItem? service = null;
        if (serviceCatalogItemId.HasValue && serviceCatalogItemId.Value != Guid.Empty)
        {
            service = await db.ServiceCatalogItems.FirstOrDefaultAsync(x => x.Id == serviceCatalogItemId.Value && x.CompanyId == company.Id && x.IsActive);
            if (service is null)
            {
                return (false, null, null, null, Results.BadRequest(new { message = "No se encontró el servicio seleccionado." }));
            }
        }

        return (true, company, customer, service, null);
    }

    private static async Task<Company?> ResolveCompanyAsync(NanchesoftDbContext db, Guid? companyId, Guid? tenantId)
    {
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            return await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId.Value && x.IsActive);
        }

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            return await db.Companies
                .Where(x => x.TenantId == tenantId.Value && x.IsActive)
                .OrderBy(x => x.Name)
                .FirstOrDefaultAsync();
        }

        return await db.Companies.Where(x => x.IsActive).OrderBy(x => x.Name).FirstOrDefaultAsync();
    }

    private static async Task<string> ResolveFolioAsync(NanchesoftDbContext db, Company company, string? requestedFolio)
    {
        var folio = NormalizeCode(requestedFolio);
        if (!string.IsNullOrWhiteSpace(folio))
        {
            return folio;
        }

        var documentFolio = await db.DocumentFolios
            .Include(x => x.Series)
            .FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.DocumentType == ServiceNoteDocumentType);

        if (documentFolio is null)
        {
            return $"NS-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        documentFolio.CurrentNumber += 1;
        documentFolio.UpdatedAt = DateTime.UtcNow;
        documentFolio.UpdatedBy = "web-api";

        if (documentFolio.Series is not null && documentFolio.Series.CurrentNumber < documentFolio.CurrentNumber)
        {
            documentFolio.Series.CurrentNumber = documentFolio.CurrentNumber;
            documentFolio.Series.UpdatedAt = DateTime.UtcNow;
            documentFolio.Series.UpdatedBy = "web-api";
        }

        var prefix = string.IsNullOrWhiteSpace(documentFolio.Series?.Prefix) ? "NS" : documentFolio.Series!.Prefix;
        var numberLength = documentFolio.Series?.NumberLength ?? 3;
        numberLength = numberLength <= 0 ? 3 : numberLength;

        return $"{prefix}{documentFolio.CurrentNumber.ToString().PadLeft(numberLength, '0')}";
    }

    private static bool TryCalculateHours(string? start, string? end, int breakMinutes, out decimal hours)
    {
        hours = 0m;
        if (!TryParseTime(start, out var startTime) || !TryParseTime(end, out var endTime))
        {
            return false;
        }

        if (endTime < startTime)
        {
            endTime = endTime.Add(TimeSpan.FromDays(1));
        }

        var totalMinutes = (decimal)(endTime - startTime).TotalMinutes - Math.Max(0, breakMinutes);
        if (totalMinutes < 0)
        {
            totalMinutes = 0;
        }

        hours = Math.Round(totalMinutes / 60m, 2, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool TryParseTime(string? value, out TimeSpan result)
    {
        result = default;
        var text = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var formats = new[]
        {
            "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss",
            "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt",
            "h:mm:ss tt", "hh:mm:ss tt"
        };

        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            result = parsed.TimeOfDay;
            return true;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
        {
            result = parsed.TimeOfDay;
            return true;
        }

        return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out result);
    }

    private static void NormalizeTrackedDateTimesToUtc(DbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries().Where(x => x.State != EntityState.Detached))
        {
            foreach (var property in entry.Properties)
            {
                var clrType = Nullable.GetUnderlyingType(property.Metadata.ClrType) ?? property.Metadata.ClrType;
                if (clrType != typeof(DateTime))
                {
                    continue;
                }

                if (property.CurrentValue is DateTime dt)
                {
                    property.CurrentValue = NormalizeUtcDateTime(dt);
                }
            }

            var entity = entry.Entity;
            if (entity is null)
            {
                continue;
            }

            var reflectionDateProperties = entity.GetType()
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) == typeof(DateTime));

            foreach (var prop in reflectionDateProperties)
            {
                var value = prop.GetValue(entity);
                if (value is DateTime dt)
                {
                    prop.SetValue(entity, NormalizeUtcDateTime(dt));
                }
            }
        }
    }

    private static string DescribeUnspecifiedDateTimes(DbContext db)
    {
        var values = new List<string>();

        foreach (var entry in db.ChangeTracker.Entries().Where(x => x.State != EntityState.Detached))
        {
            foreach (var property in entry.Properties)
            {
                var clrType = Nullable.GetUnderlyingType(property.Metadata.ClrType) ?? property.Metadata.ClrType;
                if (clrType != typeof(DateTime))
                {
                    continue;
                }

                if (property.CurrentValue is DateTime dt && dt != default && dt.Kind == DateTimeKind.Unspecified)
                {
                    values.Add($"{entry.Metadata.ClrType.Name}.{property.Metadata.Name}");
                }
            }
        }

        return string.Join(", ", values.Distinct().OrderBy(x => x));
    }

    private static DateTime NormalizeUtcDateTime(DateTime value)
    {
        if (value == default)
        {
            return value;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static DateTime? EnsureUtcDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return NormalizeBusinessDateUtc(value.Value);
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

    private static DateTime UtcToday()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static async Task<Guid> GetDefaultCurrencyIdAsync(NanchesoftDbContext db)
        => (await db.Currencies.AsNoTracking().OrderBy(x => x.CreatedAt).FirstAsync()).Id;

    private static string NormalizeBillingUnit(string? value)
    {
        var normalized = NormalizeCode(value);
        return normalized switch
        {
            "HORA" => "HORA",
            "DIA" => "DIA",
            "EVENTO" => "EVENTO",
            "MENSUALIDAD" => "MENSUALIDAD",
            _ => "HORA"
        };
    }

    private static string NormalizePaymentStatus(string? value, string fallback = "PENDIENTE")
    {
        var normalized = NormalizeCode(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        return normalized switch
        {
            "PENDIENTE" => "PENDIENTE",
            "PAGADA" or "PAGADO" or "COBRADA" or "COBRADO" => "PAGADA",
            "CANCELADA" or "CANCELADO" => "CANCELADA",
            _ => fallback
        };
    }

    private static string NormalizePaymentMethod(string? value, string fallback = "POR_DEFINIR")
    {
        var normalized = NormalizeCode(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        return normalized switch
        {
            "EFECTIVO" => "EFECTIVO",
            "DEPOSITO" => "DEPOSITO",
            "TRANSFERENCIA" => "TRANSFERENCIA",
            "POR_DEFINIR" => "POR_DEFINIR",
            _ => fallback
        };
    }

    private static string ResolveDescription(string? requestedDescription, ServiceCatalogItem? service, string fallback = "")
    {
        var description = NormalizeText(requestedDescription);
        if (!string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        if (!string.IsNullOrWhiteSpace(service?.Name))
        {
            return service.Name.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    private static string NormalizeCode(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string NormalizeLabel(string? value)
        => (value ?? string.Empty).Trim();

    private static string NormalizeText(string? value)
        => (value ?? string.Empty).Trim();

    private static string? NormalizeNullableText(string? value)
    {
        var normalized = NormalizeText(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

internal readonly record struct ServiceNotePaymentTarget(string TargetType, Guid? CashAccountId, Guid? BankAccountId, Guid? CurrencyId);
internal readonly record struct ServiceNotePaymentDestination(string? TargetType, string? Code, string? AccountNumber);

public sealed class ServiceNoteListItemDto
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

public sealed class ServiceCatalogItemListItemDto
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

public sealed class CustomerServiceRateListItemDto
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