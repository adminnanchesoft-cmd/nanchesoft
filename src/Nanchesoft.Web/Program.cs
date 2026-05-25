using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Nanchesoft.Web;
using Nanchesoft.Web.Services;
using Nanchesoft.Web.Services.Accounting;
using Nanchesoft.Web.Services.AccountsPayable;
using Nanchesoft.Web.Services.AccountsReceivable;
using Nanchesoft.Web.Services.Administration;
using Nanchesoft.Web.Services.Audit;
using Nanchesoft.Web.Services.Branches;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.Services.Cfdi;
using Nanchesoft.Web.Services.Companies;
using Nanchesoft.Web.Services.Finance;
using Nanchesoft.Web.Services.HumanResources;
using Nanchesoft.Web.Services.Inventory;
using Nanchesoft.Web.Services.Monitoring;
using Nanchesoft.Web.Services.Platform;
using Nanchesoft.Web.Services.Products;
using Nanchesoft.Web.Services.ProfessionalServices;
using Nanchesoft.Web.Services.Purchases;
using Nanchesoft.Web.Services.Reports;
using Nanchesoft.Web.Services.Sales;
using Nanchesoft.Web.Services.Security;
using Nanchesoft.Web.Services.ThirdPartiesProducts;
using Nanchesoft.Web.Services.Treasury;
using Nanchesoft.Web.Services.Production;
using Nanchesoft.Web.Services.Warehouses;
using Nanchesoft.Web.State;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxConcurrentConnections = 1000;
    kestrel.Limits.MaxConcurrentUpgradedConnections = 1000;
    kestrel.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    kestrel.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
        options.DisconnectedCircuitMaxRetained = 300;
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
        options.MaxBufferedUnacknowledgedRenderBatches = 20;
    });

builder.Services.Configure<HubOptions>(options =>
{
    // KeepAlive pings every 30s; server times out client after 90s of silence.
    // ClientTimeoutInterval must be > 2× KeepAliveInterval.
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(90);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 1024 * 1024;
    options.StreamBufferCapacity = 20;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ApiTenantScopeHandler>();

builder.Services.AddHttpClient("Nanchesoft.Api", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiSettings:BaseUrl"]
        ?? "http://localhost:5192/");
})
.AddHttpMessageHandler<ApiTenantScopeHandler>();

builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ShellState>();
builder.Services.AddScoped<ThemeState>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<NavigationService>();
builder.Services.AddScoped<ContextService>();
builder.Services.AddScoped<DashboardService>();

builder.Services.AddScoped<TenantApiService>();
builder.Services.AddScoped<PlanApiService>();
builder.Services.AddScoped<SubscriptionControlApiService>();
builder.Services.AddScoped<CompanyApiService>();
builder.Services.AddScoped<BranchApiService>();
builder.Services.AddScoped<WarehouseApiService>();
builder.Services.AddScoped<UserApiService>();
builder.Services.AddScoped<RoleApiService>();
builder.Services.AddScoped<PermissionApiService>();
builder.Services.AddScoped<SessionApiService>();
builder.Services.AddScoped<AccessLogApiService>();
builder.Services.AddScoped<MasterCatalogApiService>();
builder.Services.AddScoped<ThirdPartiesProductsApiService>();
builder.Services.AddScoped<ProductEngineeringApiService>();
builder.Services.AddScoped<PurchaseApiService>();
builder.Services.AddScoped<MaterialPurchaseApiService>();
builder.Services.AddScoped<InventoryApiService>();
builder.Services.AddScoped<SalesApiService>();
builder.Services.AddScoped<TreasuryApiService>();
builder.Services.AddScoped<ReportsApiService>();
builder.Services.AddScoped<AuditApiService>();
builder.Services.AddScoped<MonitoringApiService>();
builder.Services.AddScoped<AccountsReceivableApiService>();
builder.Services.AddScoped<AccountingApiService>();
builder.Services.AddScoped<CfdiApiService>();
builder.Services.AddScoped<FinanceApiService>();
builder.Services.AddScoped<Nanchesoft.Web.Services.Finance.FinancePhase1ApiService>();
builder.Services.AddScoped<Nanchesoft.Web.Services.Finance.FinancePhase2ApiService>();
builder.Services.AddScoped<Nanchesoft.Web.Services.Finance.PaymentCentralApiService>();
builder.Services.AddScoped<HumanResourcesApiService>();
builder.Services.AddScoped<HumanResourcesEnterpriseApiService>();
builder.Services.AddScoped<PayrollOperationsApiService>();
builder.Services.AddScoped<PayrollMvpApiService>();
builder.Services.AddScoped<AccountsPayableApiService>();
builder.Services.AddScoped<ServiceBillingApiService>();
builder.Services.AddScoped<ProductionApiService>();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
