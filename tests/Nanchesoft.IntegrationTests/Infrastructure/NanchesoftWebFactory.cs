using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API against the local dev PostgreSQL database.
/// Tests that need a clean state should truncate relevant tables in their constructor.
/// </summary>
public class NanchesoftWebFactory : WebApplicationFactory<Program>
{
    public static readonly string TestConnectionString =
        Environment.GetEnvironmentVariable("NANCHESOFT_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=nanchesoftdb_test;Username=nancheadmin;Password=CambiaEstaClave123*";

    // Fixed seed IDs so tests can reference them if needed
    public static readonly Guid SeedPlanId     = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid SeedTenantId   = Guid.Parse("11111111-0000-0000-0000-000000000002");
    public static readonly Guid SeedCompanyId  = Guid.Parse("11111111-0000-0000-0000-000000000003");
    public static readonly Guid SeedBranchId   = Guid.Parse("11111111-0000-0000-0000-000000000004");
    public static readonly Guid SeedCurrencyId = Guid.Parse("11111111-0000-0000-0000-000000000005");
    public static readonly Guid SeedWarehouseId = Guid.Parse("11111111-0000-0000-0000-000000000006");

    // Called before DeferredHostBuilder runs Program.Main, so the env var is visible
    // to Program.cs when it reads the connection string on startup.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("NANCHESOFT_TEST_DB", TestConnectionString);
        var host = base.CreateHost(builder);
        SeedDatabaseAsync().GetAwaiter().GetResult();
        return host;
    }

    private async Task SeedDatabaseAsync()
    {
        await using var db = CreateDbContext();

        if (await db.Companies.AnyAsync()) return;

        var plan = new Plan
        {
            Id = SeedPlanId,
            Code = "BASIC",
            Name = "Basic Plan",
            CreatedBy = "seed"
        };
        var tenant = new Tenant
        {
            Id = SeedTenantId,
            PlanId = plan.Id,
            Code = "TEST",
            Name = "Test Tenant",
            LegalName = "Test Tenant S.A.",
            CreatedBy = "seed"
        };
        var company = new Company
        {
            Id = SeedCompanyId,
            TenantId = tenant.Id,
            Code = "TST",
            Name = "Test Company",
            LegalName = "Test Company S.A.",
            TaxId = "TEST123456",
            CreatedBy = "seed"
        };
        var branch = new Branch
        {
            Id = SeedBranchId,
            TenantId = tenant.Id,
            CompanyId = company.Id,
            Code = "MAIN",
            Name = "Main Branch",
            CreatedBy = "seed"
        };
        var currency = new Currency
        {
            Id = SeedCurrencyId,
            TenantId = tenant.Id,
            Code = "MXN",
            Name = "Peso Mexicano",
            Symbol = "$",
            IsDefault = true,
            CreatedBy = "seed"
        };
        var warehouse = new Warehouse
        {
            Id = SeedWarehouseId,
            TenantId = tenant.Id,
            CompanyId = company.Id,
            BranchId = branch.Id,
            Code = "ALM01",
            Name = "Almacén Principal",
            CreatedBy = "seed"
        };

        db.Plans.Add(plan);
        db.Tenants.Add(tenant);
        db.Companies.Add(company);
        db.Branches.Add(branch);
        db.Currencies.Add(currency);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace the registered DbContext with one pointing at the test DB
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<NanchesoftDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<NanchesoftDbContext>(options =>
                options.UseNpgsql(TestConnectionString));
        });
    }

    public NanchesoftDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NanchesoftDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;
        return new NanchesoftDbContext(options);
    }
}
