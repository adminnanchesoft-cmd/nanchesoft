using System.Text.Json;
using Nanchesoft.Web.Services.Administration;
using Nanchesoft.Web.Services.Branches;
using Nanchesoft.Web.Services.Companies;
using Nanchesoft.Web.Services.HumanResources;
using Nanchesoft.Web.Services.Inventory;
using Nanchesoft.Web.Services.Platform;
using Nanchesoft.Web.Services.Products;
using Nanchesoft.Web.Services.ProfessionalServices;
using Nanchesoft.Web.Services.Purchases;
using Nanchesoft.Web.Services.Sales;
using Nanchesoft.Web.Services.Security;
using Nanchesoft.Web.Services.ThirdPartiesProducts;
using Nanchesoft.Web.Services.Production;
using Nanchesoft.Web.Services.Warehouses;

namespace Nanchesoft.Web.Services.Catalogs;

public sealed class CatalogCrudDispatcher
{
    private static readonly HashSet<string> SupportedCatalogKeys = new(StringComparer.OrdinalIgnoreCase)
    {
            "accesslogs",
            "addresses",
            "attendance-daily-summaries",
            "bank-accounts",
            "banks",
            "barcodes",
            "branches",
            "brands",
            "categories",
            "cities",
            "companies",
            "company-settings",
            "contacts",
            "countries",
            "credit-notes",
            "currencies",
            "customers",
            "document-folios",
            "document-series",
            "embroidery-patterns",
            "employee-contracts",
            "employee-loan-deductions",
            "employee-loans",
            "exchange-rates",
            "families",
            "finished-product-materials",
            "finished-products",
            "hr-candidate-applications",
            "hr-competency-assessments",
            "hr-departments",
            "hr-employee-certifications",
            "hr-employee-documents",
            "hr-employee-movements",
            "hr-employees",
            "hr-incidents",
            "hr-leave-types",
            "hr-onboarding-checklists",
            "hr-performance-reviews",
            "hr-positions",
            "hr-recruitment-vacancies",
            "hr-shifts",
            "hr-succession-plans",
            "hr-time-clock-devices",
            "hr-vacation-requests",
            "hr-work-schedules",
            "inventory-adjustments",
            "inventory-entries",
            "inventory-exits",
            "inventory-transfers",
            "item-engineering-profiles",
            "items",
            "kardex",
            "lasts",
            "lines",
            "lots",
            "material-families",
            "material-items",
            "material-suppliers",
            "material-supplier-cost-history",
            "material-subfamilies",
            "models",
            "payroll-accounting-postings",
            "payroll-concepts",
            "payroll-dispersion-batches",
            "payroll-dispersion-lines",
            "payroll-employer-obligations",
            "payroll-fiscal-reconciliations",
            "payroll-periods",
            "payroll-receipt-control",
            "payroll-recurring-movements",
            "payroll-run-closings",
            "payroll-run-line-details",
            "payroll-run-lines",
            "payroll-runs",
            "payroll-source-applications",
            "payroll-tax-accumulators",
            "permissions",
            "physical-counts",
            "prepayroll-adjustments",
            "prepayroll-cutoffs",
            "price-lists",
            "product-components",
            "product-consumption-profiles",
            "purchase-invoices",
            "purchase-orders",
            "purchase-receipts",
            "purchase-requisitions",
            "purchase-returns",
            "roles",
            "sales-invoices",
            "sales-orders",
            "sales-quotes",
            "sales-returns",
            "sales-shipments",
            "serials",
            "sessions",
            "size-runs",
            "service-catalog",
            "customer-service-rates",
            "service-catalog",
            "customer-service-rates",
            "service-notes",
            "states",
            "stock-balances",
            "styles",
            "suppliers",
            "taxes",
            "tenants",
            "time-clock",
            "unit-conversions",
            "units",
            "users",
            "warehouses",
            "colors",
            "manufacturing-types",
            "toe-caps",
            "dies",
            "production-cells",
            "production-vouchers",
            "production-in-process",
            "production-surplus",
            "piecework-records",
            "piecework-rates",
    };

    private static readonly HashSet<string> ReadOnlyCatalogKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "stock-balances",
        "kardex",
        "lots",
        "serials"
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly CatalogAppService _fallback = CatalogAppService.Instance;

    public CatalogCrudDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyCollection<string> GetSupportedCatalogKeys() => SupportedCatalogKeys;

    public bool CanHandle(string? catalogKey)
        => !string.IsNullOrWhiteSpace(catalogKey) && SupportedCatalogKeys.Contains(Normalize(catalogKey));

    public Task<CatalogViewDefinition> GetDefinitionAsync(string catalogKey)
    {
        var normalized = Normalize(catalogKey);
        return normalized switch
        {
            "tenants" => Resolve<TenantApiService>().GetCatalogAsync(),
            "companies" => Resolve<CompanyApiService>().GetCatalogAsync(),
            "branches" => Resolve<BranchApiService>().GetCatalogAsync(),
            "warehouses" => Resolve<WarehouseApiService>().GetCatalogAsync(),
            "users" => Resolve<UserApiService>().GetCatalogAsync(),
            "roles" => Resolve<RoleApiService>().GetCatalogAsync(),
            "permissions" => Resolve<PermissionApiService>().GetCatalogAsync(),
            "sessions" => Resolve<SessionApiService>().GetCatalogAsync(),
            "accesslogs" => Resolve<AccessLogApiService>().GetCatalogAsync(),
            "currencies" or "exchange-rates" or "taxes" or "units" or "banks" or "countries" or "states" or "cities" or "document-series" or "document-folios" or "company-settings"
                => Resolve<MasterCatalogApiService>().GetCatalogAsync(normalized),
            "customers" or "suppliers" or "contacts" or "addresses" or "bank-accounts" or "categories" or "brands" or "models" or "items" or "price-lists" or "barcodes"
                => Resolve<ThirdPartiesProductsApiService>().GetCatalogAsync(normalized),
            "purchase-requisitions" or "purchase-orders" or "purchase-receipts" or "purchase-invoices" or "purchase-returns"
                => Resolve<PurchaseApiService>().GetCatalogAsync(normalized),
            "stock-balances" or "kardex" or "inventory-entries" or "inventory-exits" or "inventory-transfers" or "inventory-adjustments" or "physical-counts" or "lots" or "serials"
                => Resolve<InventoryApiService>().GetCatalogAsync(normalized),
            "sales-quotes" or "sales-orders" or "sales-shipments" or "sales-invoices" or "sales-returns" or "credit-notes"
                => Resolve<SalesApiService>().GetCatalogAsync(normalized),
            "service-catalog" or "customer-service-rates" or "service-notes" => Resolve<ServiceBillingApiService>().GetCatalogAsync(normalized),
            "hr-departments" or "hr-positions" or "hr-employees" or "hr-incidents" or "employee-contracts" or "payroll-periods" or "payroll-concepts" or "payroll-runs" or "payroll-run-lines" or "payroll-run-line-details"
                => Resolve<HumanResourcesApiService>().GetCatalogAsync(normalized),
            "time-clock" or "attendance-daily-summaries" or "payroll-recurring-movements" or "prepayroll-adjustments" or "prepayroll-cutoffs" or "employee-loans" or "employee-loan-deductions" or "payroll-source-applications" or "payroll-receipt-control" or "payroll-run-closings" or "payroll-dispersion-batches" or "payroll-dispersion-lines" or "payroll-accounting-postings" or "payroll-tax-accumulators" or "payroll-employer-obligations" or "payroll-fiscal-reconciliations"
                => Resolve<PayrollOperationsApiService>().GetCatalogAsync(normalized),
            "hr-shifts" or "hr-work-schedules" or "hr-time-clock-devices" or "hr-leave-types" or "hr-vacation-requests" or "hr-employee-documents" or "hr-employee-movements" or "hr-employee-certifications" or "hr-recruitment-vacancies" or "hr-candidate-applications" or "hr-onboarding-checklists" or "hr-performance-reviews" or "hr-competency-assessments" or "hr-succession-plans"
                => Resolve<HumanResourcesEnterpriseApiService>().GetCatalogAsync(normalized),
            "unit-conversions" or "size-runs" or "families" or "lasts" or "lines" or "styles" or "embroidery-patterns" or "item-engineering-profiles" or "material-families" or "material-subfamilies" or "material-items" or "material-suppliers" or "material-supplier-cost-history" or "finished-products" or "product-components" or "finished-product-materials" or "product-consumption-profiles" or "colors" or "manufacturing-types" or "toe-caps" or "dies"
                => Resolve<ProductEngineeringApiService>().GetCatalogAsync(normalized),
            "production-cells" or "production-vouchers" or "production-in-process" or "production-surplus" or "piecework-records" or "piecework-rates"
                => Resolve<ProductionApiService>().GetCatalogAsync(normalized),
            _ => _fallback.GetAsync(normalized)
        };
    }

    public Task<CatalogViewDefinition> ExecuteWriteAsync(string catalogKey, string action, string? key, JsonElement row)
    {
        var normalized = Normalize(catalogKey);

        if (ReadOnlyCatalogKeys.Contains(normalized))
        {
            throw new InvalidOperationException($"El catálogo '{normalized}' es solo consulta.");
        }

        return normalized switch
        {
            "tenants" => action == "insert" ? Resolve<TenantApiService>().InsertAsync(row) : Resolve<TenantApiService>().UpdateAsync(key!, row),
            "companies" => action == "insert" ? Resolve<CompanyApiService>().InsertAsync(row) : Resolve<CompanyApiService>().UpdateAsync(key!, row),
            "branches" => action == "insert" ? Resolve<BranchApiService>().InsertAsync(row) : Resolve<BranchApiService>().UpdateAsync(key!, row),
            "warehouses" => action == "insert" ? Resolve<WarehouseApiService>().InsertAsync(row) : Resolve<WarehouseApiService>().UpdateAsync(key!, row),
            "users" => action == "insert" ? Resolve<UserApiService>().InsertAsync(row) : Resolve<UserApiService>().UpdateAsync(key!, row),
            "roles" => action == "insert" ? Resolve<RoleApiService>().InsertAsync(row) : Resolve<RoleApiService>().UpdateAsync(key!, row),
            "permissions" => action == "insert" ? Resolve<PermissionApiService>().InsertAsync(row) : Resolve<PermissionApiService>().UpdateAsync(key!, row),
            "sessions" => action == "insert" ? Resolve<SessionApiService>().InsertAsync(row) : Resolve<SessionApiService>().UpdateAsync(key!, row),
            "accesslogs" => action == "insert" ? Resolve<AccessLogApiService>().InsertAsync(row) : Resolve<AccessLogApiService>().UpdateAsync(key!, row),
            "currencies" or "exchange-rates" or "taxes" or "units" or "banks" or "countries" or "states" or "cities" or "document-series" or "document-folios" or "company-settings"
                => action == "insert" ? Resolve<MasterCatalogApiService>().InsertAsync(normalized, row) : Resolve<MasterCatalogApiService>().UpdateAsync(normalized, key!, row),
            "customers" or "suppliers" or "contacts" or "addresses" or "bank-accounts" or "categories" or "brands" or "models" or "items" or "price-lists" or "barcodes"
                => action == "insert" ? Resolve<ThirdPartiesProductsApiService>().InsertAsync(normalized, row) : Resolve<ThirdPartiesProductsApiService>().UpdateAsync(normalized, key!, row),
            "purchase-requisitions" or "purchase-orders" or "purchase-receipts" or "purchase-invoices" or "purchase-returns"
                => action == "insert" ? Resolve<PurchaseApiService>().InsertAsync(normalized, row) : Resolve<PurchaseApiService>().UpdateAsync(normalized, key!, row),
            "inventory-entries" or "inventory-exits" or "inventory-transfers" or "inventory-adjustments" or "physical-counts"
                => action == "insert" ? Resolve<InventoryApiService>().InsertAsync(normalized, row) : Resolve<InventoryApiService>().UpdateAsync(normalized, key!, row),
            "sales-quotes" or "sales-orders" or "sales-shipments" or "sales-invoices" or "sales-returns" or "credit-notes"
                => action == "insert" ? Resolve<SalesApiService>().InsertAsync(normalized, row) : Resolve<SalesApiService>().UpdateAsync(normalized, key!, row),
            "service-catalog" or "customer-service-rates" or "service-notes" => action == "insert" ? Resolve<ServiceBillingApiService>().InsertAsync(normalized, row) : Resolve<ServiceBillingApiService>().UpdateAsync(normalized, key!, row),
            "hr-departments" or "hr-positions" or "hr-employees" or "hr-incidents" or "employee-contracts" or "payroll-periods" or "payroll-concepts" or "payroll-runs" or "payroll-run-lines" or "payroll-run-line-details"
                => action == "insert" ? Resolve<HumanResourcesApiService>().InsertAsync(normalized, row) : Resolve<HumanResourcesApiService>().UpdateAsync(normalized, key!, row),
            "time-clock" or "attendance-daily-summaries" or "payroll-recurring-movements" or "prepayroll-adjustments" or "prepayroll-cutoffs" or "employee-loans" or "employee-loan-deductions" or "payroll-source-applications" or "payroll-receipt-control" or "payroll-run-closings" or "payroll-dispersion-batches" or "payroll-dispersion-lines" or "payroll-accounting-postings" or "payroll-tax-accumulators" or "payroll-employer-obligations" or "payroll-fiscal-reconciliations"
                => action == "insert" ? Resolve<PayrollOperationsApiService>().InsertAsync(normalized, row) : Resolve<PayrollOperationsApiService>().UpdateAsync(normalized, key!, row),
            "hr-shifts" or "hr-work-schedules" or "hr-time-clock-devices" or "hr-leave-types" or "hr-vacation-requests" or "hr-employee-documents" or "hr-employee-movements" or "hr-employee-certifications" or "hr-recruitment-vacancies" or "hr-candidate-applications" or "hr-onboarding-checklists" or "hr-performance-reviews" or "hr-competency-assessments" or "hr-succession-plans"
                => action == "insert" ? Resolve<HumanResourcesEnterpriseApiService>().InsertAsync(normalized, row) : Resolve<HumanResourcesEnterpriseApiService>().UpdateAsync(normalized, key!, row),
            "unit-conversions" or "size-runs" or "families" or "lasts" or "lines" or "styles" or "embroidery-patterns" or "item-engineering-profiles" or "material-families" or "material-subfamilies" or "material-items" or "material-suppliers" or "material-supplier-cost-history" or "finished-products" or "product-components" or "finished-product-materials" or "product-consumption-profiles" or "colors" or "manufacturing-types" or "toe-caps" or "dies"
                => action == "insert" ? Resolve<ProductEngineeringApiService>().InsertAsync(normalized, row) : Resolve<ProductEngineeringApiService>().UpdateAsync(normalized, key!, row),
            "production-cells" or "production-vouchers" or "production-in-process" or "production-surplus" or "piecework-records" or "piecework-rates"
                => action == "insert" ? Resolve<ProductionApiService>().InsertAsync(normalized, row) : Resolve<ProductionApiService>().UpdateAsync(normalized, key!, row),
            _ => action == "insert" ? _fallback.InsertAsync(normalized, row) : _fallback.UpdateAsync(normalized, key!, row)
        };
    }

    public Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        var normalized = Normalize(catalogKey);

        if (ReadOnlyCatalogKeys.Contains(normalized))
        {
            throw new InvalidOperationException($"El catálogo '{normalized}' es solo consulta.");
        }

        return normalized switch
        {
            "tenants" => Resolve<TenantApiService>().DeleteAsync(key),
            "companies" => Resolve<CompanyApiService>().DeleteAsync(key),
            "branches" => Resolve<BranchApiService>().DeleteAsync(key),
            "warehouses" => Resolve<WarehouseApiService>().DeleteAsync(key),
            "users" => Resolve<UserApiService>().DeleteAsync(key),
            "roles" => Resolve<RoleApiService>().DeleteAsync(key),
            "permissions" => Resolve<PermissionApiService>().DeleteAsync(key),
            "sessions" => Resolve<SessionApiService>().DeleteAsync(key),
            "accesslogs" => Resolve<AccessLogApiService>().DeleteAsync(key),
            "currencies" or "exchange-rates" or "taxes" or "units" or "banks" or "countries" or "states" or "cities" or "document-series" or "document-folios" or "company-settings"
                => Resolve<MasterCatalogApiService>().DeleteAsync(normalized, key),
            "customers" or "suppliers" or "contacts" or "addresses" or "bank-accounts" or "categories" or "brands" or "models" or "items" or "price-lists" or "barcodes"
                => Resolve<ThirdPartiesProductsApiService>().DeleteAsync(normalized, key),
            "purchase-requisitions" or "purchase-orders" or "purchase-receipts" or "purchase-invoices" or "purchase-returns"
                => Resolve<PurchaseApiService>().DeleteAsync(normalized, key),
            "inventory-entries" or "inventory-exits" or "inventory-transfers" or "inventory-adjustments" or "physical-counts"
                => Resolve<InventoryApiService>().DeleteAsync(normalized, key),
            "sales-quotes" or "sales-orders" or "sales-shipments" or "sales-invoices" or "sales-returns" or "credit-notes"
                => Resolve<SalesApiService>().DeleteAsync(normalized, key),
            "service-catalog" or "customer-service-rates" or "service-notes" => Resolve<ServiceBillingApiService>().DeleteAsync(normalized, key),
            "hr-departments" or "hr-positions" or "hr-employees" or "hr-incidents" or "employee-contracts" or "payroll-periods" or "payroll-concepts" or "payroll-runs" or "payroll-run-lines" or "payroll-run-line-details"
                => Resolve<HumanResourcesApiService>().DeleteAsync(normalized, key),
            "time-clock" or "attendance-daily-summaries" or "payroll-recurring-movements" or "prepayroll-adjustments" or "prepayroll-cutoffs" or "employee-loans" or "employee-loan-deductions" or "payroll-source-applications" or "payroll-receipt-control" or "payroll-run-closings" or "payroll-dispersion-batches" or "payroll-dispersion-lines" or "payroll-accounting-postings" or "payroll-tax-accumulators" or "payroll-employer-obligations" or "payroll-fiscal-reconciliations"
                => Resolve<PayrollOperationsApiService>().DeleteAsync(normalized, key),
            "hr-shifts" or "hr-work-schedules" or "hr-time-clock-devices" or "hr-leave-types" or "hr-vacation-requests" or "hr-employee-documents" or "hr-employee-movements" or "hr-employee-certifications" or "hr-recruitment-vacancies" or "hr-candidate-applications" or "hr-onboarding-checklists" or "hr-performance-reviews" or "hr-competency-assessments" or "hr-succession-plans"
                => Resolve<HumanResourcesEnterpriseApiService>().DeleteAsync(normalized, key),
            "unit-conversions" or "size-runs" or "families" or "lasts" or "lines" or "styles" or "embroidery-patterns" or "item-engineering-profiles" or "material-families" or "material-subfamilies" or "material-items" or "material-suppliers" or "material-supplier-cost-history" or "finished-products" or "product-components" or "finished-product-materials" or "product-consumption-profiles" or "colors" or "manufacturing-types" or "toe-caps" or "dies"
                => Resolve<ProductEngineeringApiService>().DeleteAsync(normalized, key),
            "production-cells" or "production-vouchers" or "production-in-process" or "production-surplus" or "piecework-records" or "piecework-rates"
                => Resolve<ProductionApiService>().DeleteAsync(normalized, key),
            _ => _fallback.DeleteAsync(normalized, key)
        };
    }

    private T Resolve<T>() where T : notnull
        => _serviceProvider.GetRequiredService<T>();

    private static string Normalize(string catalogKey)
    {
        if (string.IsNullOrWhiteSpace(catalogKey))
        {
            throw new InvalidOperationException("El catálogo no fue especificado.");
        }

        return catalogKey.Trim().ToLowerInvariant();
    }
}
