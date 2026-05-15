using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Persistence.Seed;

public static class ProfessionalServicesSeeder
{
    private sealed record SeedService(string Code, string Name, string Description, decimal DefaultRate);

    private static readonly SeedService[] DefaultServices =
    [
        new("DESA-HRS", "Desarrollo de software por hora", "Servicio por horas para desarrollo, mejora y mantenimiento de software.", 400m),
        new("SOP-HRS", "Soporte técnico por hora", "Atención correctiva, soporte remoto y atención a incidencias.", 300m),
        new("CONS-HRS", "Consultoría por hora", "Análisis funcional, levantamiento y acompañamiento técnico.", 500m),
        new("IMPL-HRS", "Implementación por hora", "Parametrización, arranque y acompañamiento de implantación.", 450m)
    ];

    public static async Task SeedAsync(NanchesoftDbContext dbContext)
    {
        var companies = await dbContext.Companies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        if (companies.Count == 0)
        {
            return;
        }

        foreach (var company in companies)
        {
            foreach (var seed in DefaultServices)
            {
                var service = await dbContext.ServiceCatalogItems.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == seed.Code);
                if (service is null)
                {
                    dbContext.ServiceCatalogItems.Add(new ServiceCatalogItem
                    {
                        TenantId = company.TenantId,
                        CompanyId = company.Id,
                        Code = seed.Code,
                        Name = seed.Name,
                        Description = seed.Description,
                        BillingUnit = "HORA",
                        DefaultRate = seed.DefaultRate,
                        Notes = "Semilla inicial del módulo de servicios.",
                        CreatedBy = "seed"
                    });
                }
            }
        }

        await dbContext.SaveChangesAsync();

        var developmentServices = await dbContext.ServiceCatalogItems
            .Where(x => x.Code == "DESA-HRS")
            .ToDictionaryAsync(x => x.CompanyId, x => x);

        var notesToLink = await dbContext.ServiceNotes
            .Where(x => x.ServiceCatalogItemId == null && EF.Functions.ILike(x.Description, "%DESARROLLO%"))
            .ToListAsync();

        foreach (var note in notesToLink)
        {
            if (!developmentServices.TryGetValue(note.CompanyId, out var service))
            {
                continue;
            }

            note.ServiceCatalogItemId = service.Id;
            if (string.IsNullOrWhiteSpace(note.ServiceCodeSnapshot))
            {
                note.ServiceCodeSnapshot = service.Code;
            }

            if (string.IsNullOrWhiteSpace(note.ServiceNameSnapshot))
            {
                note.ServiceNameSnapshot = service.Name;
            }

            if (note.HourlyRate <= 0)
            {
                note.HourlyRate = service.DefaultRate;
            }

            note.Subtotal = Math.Round(note.HoursWorked * note.HourlyRate, 2, MidpointRounding.AwayFromZero);
            note.Total = note.Subtotal;
            note.UpdatedAt = DateTime.UtcNow;
            note.UpdatedBy = "seed";
        }

        if (notesToLink.Count > 0)
        {
            await dbContext.SaveChangesAsync();
        }

        await EnsureSilvasoftCustomerRatesAsync(dbContext);
    }

    private static async Task EnsureSilvasoftCustomerRatesAsync(NanchesoftDbContext dbContext)
    {
        const string seedUser = "seed-service-billing";

        var company = await dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "SIL001" || x.Name == "Silvasoft");

        if (company is null)
        {
            return;
        }

        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "VCI");
        if (customer is null)
        {
            customer = new Customer
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                Code = "VCI",
                Name = "VCI",
                LegalName = "VCI",
                TaxId = "XAXX010101000",
                Email = string.Empty,
                Phone = string.Empty,
                PaymentTermDays = 1,
                CreditLimit = 0,
                CreatedBy = seedUser
            };
            dbContext.Customers.Add(customer);
            await dbContext.SaveChangesAsync();
        }

        var service = await dbContext.ServiceCatalogItems.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "DESA-HRS");
        if (service is null)
        {
            return;
        }

        var mxn = await dbContext.Currencies.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "MXN");
        var effectiveFrom = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);

        if (!await dbContext.CustomerServiceRates.AnyAsync(x => x.CompanyId == company.Id && x.CustomerId == customer.Id && x.ServiceCatalogItemId == service.Id && x.EffectiveFrom == effectiveFrom))
        {
            dbContext.CustomerServiceRates.Add(new CustomerServiceRate
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                CustomerId = customer.Id,
                ServiceCatalogItemId = service.Id,
                CurrencyId = mxn?.Id,
                Rate = 300m,
                EffectiveFrom = effectiveFrom,
                Notes = "Tarifa por hora negociada para VCI.",
                CreatedBy = seedUser
            });
        }

        if (!await dbContext.ServiceNotes.AnyAsync(x => x.CompanyId == company.Id && x.Folio == "VCI003"))
        {
            dbContext.ServiceNotes.Add(new ServiceNote
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                CustomerId = customer.Id,
                ServiceCatalogItemId = service.Id,
                CustomerNameSnapshot = customer.Name,
                ServiceCodeSnapshot = service.Code,
                ServiceNameSnapshot = service.Name,
                Folio = "VCI003",
                NoteDate = effectiveFrom,
                Description = "Desarrollo de sistemas de 10:30 pm a 7:30 pm - 30 min. de comida",
                StartTimeText = "22:30",
                EndTimeText = "07:30",
                BreakMinutes = 30,
                HoursWorked = 8.5m,
                HourlyRate = 300m,
                Subtotal = 2550m,
                Total = 2550m,
                PaymentStatus = "PENDIENTE",
                PaymentMethod = "POR_DEFINIR",
                Notes = "Nota base capturada desde el formato comercial del cliente.",
                CreatedBy = seedUser
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
