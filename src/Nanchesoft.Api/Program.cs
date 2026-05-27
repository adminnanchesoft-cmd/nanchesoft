using Nanchesoft.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Api.Endpoints;
using Nanchesoft.Persistence.Context;
using Nanchesoft.Persistence.Seed;
using Npgsql;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("Copomex").ConfigureHttpClient(c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("NanchesoftCors", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

var defaultConnection = Environment.GetEnvironmentVariable("NANCHESOFT_TEST_DB")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

builder.Services.AddDbContext<NanchesoftDbContext>(options =>
    options.UseNpgsql(defaultConnection));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("NanchesoftCors");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

await EnsureDatabaseExistsAsync(defaultConnection);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NanchesoftDbContext>();

    await using (var schemaCommand = dbContext.Database.GetDbConnection().CreateCommand())
    {
        if (schemaCommand.Connection is not null && schemaCommand.Connection.State != System.Data.ConnectionState.Open)
        {
            await schemaCommand.Connection.OpenAsync();
        }

        schemaCommand.CommandText = NanchesoftSchemaCatalog.BuildEnsureSchemasSql();
        await schemaCommand.ExecuteNonQueryAsync();
    }

    dbContext.Database.EnsureCreated();
    await SubscriptionControlSeeder.EnsureAsync(dbContext);
    await InitialDataSeeder.SeedAsync(dbContext);
    await CommercialTenantsSeeder.SeedAsync(dbContext);
    await ThirdPartiesProductsSeeder.SeedAsync(dbContext);
    if (app.Environment.IsDevelopment())
    {
        await ProductEngineeringFoundationSeeder.SeedAsync(dbContext);
    }
    await PurchasesSeeder.SeedAsync(dbContext);
    await InventorySeeder.SeedAsync(dbContext);
    await SalesSeeder.SeedAsync(dbContext);
    await TreasurySeeder.SeedAsync(dbContext);
    await AccountsReceivableSeeder.SeedAsync(dbContext);
    await AccountsPayableSeeder.SeedAsync(dbContext);
    await CfdiSeeder.SeedAsync(dbContext);
    await AccountingSeeder.SeedAsync(dbContext);
    await HumanResourcesSeeder.SeedAsync(dbContext);
    await PayrollAdvancedSeeder.SeedAsync(dbContext);
    await HumanResourcesEnterpriseSeeder.SeedAsync(dbContext);
    await HumanResourcesLifecycleSeeder.SeedAsync(dbContext);
    await HumanResourcesTalentSeeder.SeedAsync(dbContext);
    await HumanResourcesPerformanceSeeder.SeedAsync(dbContext);
    await PayrollPrePayrollSeeder.SeedAsync(dbContext);
    await PayrollCalculatedSeeder.SeedAsync(dbContext);
    await PayrollDisbursementSeeder.SeedAsync(dbContext);
    await PayrollFiscalSeeder.SeedAsync(dbContext);
    await ProductTechnicalCostingSeeder.SeedAsync(dbContext);
    await ProfessionalServicesSeeder.SeedAsync(dbContext);
    await ProductionSeeder.SeedAsync(dbContext);
}

app.MapPost("/api/auth/login", async (AuthLoginRequest request, NanchesoftDbContext db) =>
{
    var usernameOrEmail = (request.UsernameOrEmail ?? string.Empty).Trim();
    var password = request.Password ?? string.Empty;

    var user = await db.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(x =>
            (x.Username == usernameOrEmail || x.Email == usernameOrEmail) &&
            x.IsActive &&
            !x.IsLocked);

    if (user is null || password != "Admin123*")
    {
        return Results.Unauthorized();
    }

    var tenant = await db.Tenants
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == user.TenantId && x.IsActive);

    if (tenant is null)
    {
        tenant = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            join t in db.Tenants.AsNoTracking() on r.TenantId equals t.Id
            where ur.UserId == user.Id && ur.IsActive && t.IsActive
            orderby t.Name
            select t)
            .FirstOrDefaultAsync();
    }

    var effectiveTenantId = tenant?.Id ?? user.TenantId;

    var company = await db.Companies
        .AsNoTracking()
        .Where(x => x.TenantId == effectiveTenantId && x.IsActive)
        .OrderBy(x => x.Name)
        .FirstOrDefaultAsync();

    var branch = company is null
        ? null
        : await db.Branches
            .AsNoTracking()
            .Where(x => x.TenantId == effectiveTenantId && x.CompanyId == company.Id && x.IsActive)
            .OrderBy(x => x.Name)
            .FirstOrDefaultAsync();

    var roleInfos = await (
        from ur in db.UserRoles.AsNoTracking()
        join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
        where ur.UserId == user.Id && ur.IsActive
        orderby r.IsSystemRole descending, r.Name
        select new
        {
            r.Code,
            r.Name
        })
        .ToListAsync();

    var roleInfo = roleInfos.FirstOrDefault();

    var trackedUser = await db.Users.FirstAsync(x => x.Id == user.Id);
    trackedUser.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var isPlatformOwner = roleInfos.Any(x =>
        string.Equals(x.Code, "PLATFORM_OWNER", StringComparison.OrdinalIgnoreCase)
        || string.Equals(x.Code, "SYSTEM_ADMIN", StringComparison.OrdinalIgnoreCase)
        || string.Equals(x.Code, "OWNER", StringComparison.OrdinalIgnoreCase));

    return Results.Ok(new
    {
        token = "demo-token",
        refreshToken = "demo-refresh-token",
        userId = user.Id,
        username = user.Username,
        email = user.Email,
        displayName = string.IsNullOrWhiteSpace(user.GetDisplayName()) ? user.Username : user.GetDisplayName(),
        firstName = user.FirstName,
        lastName = user.LastName,
        roleName = roleInfo?.Name ?? "Tenant admin",
        isPlatformOwner,
        tenantId = effectiveTenantId,
        tenantCode = tenant?.Code ?? string.Empty,
        tenantName = tenant?.Name ?? string.Empty,
        companyId = company?.Id,
        companyName = company?.Name ?? string.Empty,
        branchId = branch?.Id,
        branchName = branch?.Name ?? string.Empty,
        requiresTenantSelection = false
    });
});

app.MapPostalCodeEndpoints();
app.MapCompanyEndpoints();
app.MapBranchEndpoints();
app.MapWarehouseEndpoints();
app.MapUserEndpoints();
app.MapRoleEndpoints();
app.MapPermissionEndpoints();
app.MapSessionEndpoints();
app.MapAccessLogEndpoints();
app.MapMasterCatalogEndpoints();
app.MapThirdPartiesAndProductsEndpoints();
app.MapProductEngineeringEndpoints();
app.MapProductOrangeCatalogEndpoints();
app.MapProductSizeRunEnterpriseEndpoints();
app.MapProductMaterialSupplierEndpoints();
app.MapPurchaseEndpoints();
app.MapInventoryEndpoints();
app.MapSalesEndpoints();
app.MapTreasuryEndpoints();
app.MapReportsEndpoints();
app.MapAuditEndpoints();
app.MapMonitoringEndpoints();
app.MapAccountsReceivableEndpoints();
app.MapAccountsPayableEndpoints();
app.MapCfdiEndpoints();
app.MapAccountingEndpoints();
app.MapFinanceEndpoints();
app.MapHumanResourcesEndpoints();
app.MapHumanResourcesCatalogsEndpoints();
app.MapPayrollDetailEndpoints();
app.MapPayrollAdvancedEndpoints();
app.MapHumanResourcesEnterpriseEndpoints();
app.MapHumanResourcesLifecycleEndpoints();
app.MapHumanResourcesTalentEndpoints();
app.MapHumanResourcesPerformanceEndpoints();
app.MapPayrollPrePayrollEndpoints();
app.MapPayrollCalculatedEndpoints();
app.MapPayrollDisbursementEndpoints();
app.MapPayrollFiscalEndpoints();
app.MapPayrollMvpEndpoints();
app.MapProductTechnicalCostingEndpoints();
app.MapProductTechnicalCenterEndpoints();
app.MapConsumptionTemplateEndpoints();
app.MapMaterialExplosionEndpoints();
app.MapProductionOrderEndpoints();
app.MapProductionScheduleEndpoints();
app.MapProductionVoucherEndpoints();
app.MapProductionPieceWorkEndpoints();
app.MapProductionDashboardEndpoints();
app.MapQualityControlEndpoints();
app.MapServiceBillingEndpoints();
app.MapTenantEndpoints();
app.MapPlanEndpoints();
app.MapSubscriptionControlEndpoints();
app.MapUniversalImportEndpoints();

app.Run();

static async Task EnsureDatabaseExistsAsync(string connectionString)
{
    var targetBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    var databaseName = targetBuilder.Database;

    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException("La cadena de conexión no tiene nombre de base de datos.");
    }

    var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    };

    await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
    await connection.OpenAsync();

    await using (var existsCommand = connection.CreateCommand())
    {
        existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName;";
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);

        var exists = await existsCommand.ExecuteScalarAsync();
        if (exists is not null)
        {
            return;
        }
    }

    var quotedDatabaseName = QuoteIdentifier(databaseName);
    await using var createCommand = connection.CreateCommand();
    createCommand.CommandText = $"CREATE DATABASE {quotedDatabaseName};";
    await createCommand.ExecuteNonQueryAsync();
}

static string QuoteIdentifier(string identifier)
{
    return "\"" + identifier.Replace("\"", "\"\"") + "\"";
}

public sealed class AuthLoginRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
