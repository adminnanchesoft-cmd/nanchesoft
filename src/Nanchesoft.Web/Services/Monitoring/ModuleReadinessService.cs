using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.Monitoring;

public sealed class ModuleReadinessService
{
    private readonly CatalogCrudDispatcher _dispatcher;

    private static readonly List<ModuleReadinessRow> CatalogRows = new()
    {
        // Productos
        new("Productos", "Familias material", "/products/material-families", "material-families"),
        new("Productos", "Subfamilias material", "/products/material-subfamilies", "material-subfamilies"),
        new("Productos", "Materiales", "/products/material-items", "material-items"),
        new("Productos", "Productos terminados", "/products/finished-products", "finished-products"),
        new("Productos", "Componentes producto", "/products/product-components", "product-components"),
        new("Productos", "Materiales por producto", "/products/finished-product-materials", "finished-product-materials"),
        new("Productos", "Plantillas de consumo", "/products/consumption-templates", "product-consumption-profiles"),
        new("Productos", "Insumos por producto", "/products/finished-product-supplies", "finished-product-supplies"),
        new("Productos", "Distribución de tallas", "/products/material-size-distributions", "material-size-distributions"),
        // Compras
        new("Compras", "Requisiciones", "/purchases/requisitions", "purchase-requisitions"),
        new("Compras", "Órdenes de compra", "/purchases/orders", "purchase-orders"),
        new("Compras", "Recepciones", "/purchases/receipts", "purchase-receipts"),
        // Ventas
        new("Ventas", "Cotizaciones", "/sales/quotes", "sales-quotes"),
        new("Ventas", "Pedidos", "/sales/orders", "sales-orders"),
        // Inventario
        new("Inventario", "Entradas", "/inventory/entries", "inventory-entries"),
        new("Inventario", "Salidas", "/inventory/exits", "inventory-exits"),
        new("Inventario", "Saldos (solo lectura)", "/inventory/stock-balances", "stock-balances"),
        new("Inventario", "Kardex (solo lectura)", "/inventory/kardex", "kardex"),
        // Nómina
        new("Nómina", "Períodos", "/payroll/periods", "payroll-periods"),
        new("Nómina", "Conceptos", "/payroll/concepts", "payroll-concepts"),
        new("Nómina", "Corridas", "/payroll/runs", "payroll-runs"),
        new("Nómina", "Préstamos", "/payroll/loans", "payroll-loans"),
        new("Nómina", "Movimientos recurrentes", "/payroll/recurring-movements", "payroll-recurring-movements"),
        new("Nómina", "Fuentes de aplicación", "/payroll/source-applications", "payroll-source-applications"),
        new("Nómina", "Control de recibos", "/payroll/receipt-control", "payroll-receipt-control"),
        // Recursos Humanos
        new("Recursos Humanos", "Departamentos", "/human-resources/departments", "hr-departments"),
        new("Recursos Humanos", "Puestos", "/human-resources/positions", "hr-positions"),
        new("Recursos Humanos", "Empleados", "/human-resources/employees", "hr-employees"),
        new("Recursos Humanos", "Turnos", "/human-resources/shifts", "hr-shifts"),
        new("Recursos Humanos", "Vacaciones y permisos", "/human-resources/vacation-requests", "hr-vacation-requests"),
        new("Recursos Humanos", "Incidencias", "/human-resources/incidents", "hr-incidents"),
        new("Recursos Humanos", "Documentos empleado", "/human-resources/employee-documents", "hr-employee-documents"),
        new("Recursos Humanos", "Movimientos empleado", "/human-resources/employee-movements", "hr-employee-movements"),
        // Tesorería
        new("Tesorería", "Cuentas de caja", "/treasury/cash-accounts", "cash-accounts"),
        new("Tesorería", "Cuentas bancarias", "/treasury/bank-accounts", "bank-accounts-own"),
        new("Tesorería", "Ingresos", "/treasury/incomes", "treasury-incomes"),
        new("Tesorería", "Egresos", "/treasury/expenses", "treasury-expenses"),
        new("Tesorería", "Recibos", "/treasury/receipts", "treasury-receipts"),
        new("Tesorería", "Pagos", "/treasury/payments", "treasury-payments"),
        new("Tesorería", "Conciliaciones", "/treasury/reconciliations", "treasury-reconciliations"),
    };

    public ModuleReadinessService(CatalogCrudDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<List<ModuleReadinessResult>> GetRowsAsync()
    {
        var rows = CatalogRows
            .Select(x =>
            {
                var canHandle = _dispatcher.CanHandle(x.CatalogKey);
                return new ModuleReadinessResult(
                    x.Module,
                    x.Name,
                    x.Route,
                    x.CatalogKey,
                    canHandle,
                    canHandle ? "Listo" : "Sin mapeo catálogo",
                    canHandle ? "El catálogo está conectado al dispatcher compartido." : "Falta mapear este catálogo en CatalogCrudDispatcher.");
            })
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Name)
            .ToList();

        return Task.FromResult(rows);
    }

    private sealed record ModuleReadinessRow(string Module, string Name, string Route, string CatalogKey);
}

public sealed record ModuleReadinessResult(
    string Module,
    string Name,
    string Route,
    string CatalogKey,
    bool IsReady,
    string Status,
    string Notes);
