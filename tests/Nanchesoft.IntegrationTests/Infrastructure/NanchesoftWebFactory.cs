using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    // Called before DeferredHostBuilder runs Program.Main, so the env var is visible
    // to Program.cs when it reads the connection string on startup.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("NANCHESOFT_TEST_DB", TestConnectionString);
        return base.CreateHost(builder);
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
