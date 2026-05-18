using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services;

public sealed class NavigationService
{
    private readonly AuthState _authState;

    public NavigationService(AuthState authState)
    {
        _authState = authState;
    }

    public List<NavigationMenuGroup> GetMenu()
    {
        var menu = new List<NavigationMenuGroup>
        {
            new() { Title = "Inicio", Items = [ new NavigationMenuItem { Title = "Dashboard", Route = "/dashboard" }, new NavigationMenuItem { Title = "Perfil", Route = "/profile" } ] },
            new() { Title = "Organización", Items = [ new NavigationMenuItem { Title = "Empresas", Route = "/organization/companies" }, new NavigationMenuItem { Title = "Sucursales", Route = "/organization/branches" }, new NavigationMenuItem { Title = "Almacenes", Route = "/organization/warehouses" } ] },
            new() { Title = "Recursos Humanos", Items = [ new NavigationMenuItem { Title = "Colaboradores", Route = "/human-resources/employees" }, new NavigationMenuItem { Title = "Departamentos", Route = "/human-resources/departments" }, new NavigationMenuItem { Title = "Puestos", Route = "/human-resources/positions" }, new NavigationMenuItem { Title = "Turnos", Route = "/human-resources/shifts", Badge = "NEW" }, new NavigationMenuItem { Title = "Horarios laborales", Route = "/human-resources/work-schedules", Badge = "NEW" }, new NavigationMenuItem { Title = "Incidencias", Route = "/human-resources/incidents" }, new NavigationMenuItem { Title = "Tipos de ausencia", Route = "/human-resources/leave-types", Badge = "NEW" }, new NavigationMenuItem { Title = "Vacaciones y permisos", Route = "/human-resources/vacation-requests", Badge = "FLOW" }, new NavigationMenuItem { Title = "Reloj checador", Route = "/human-resources/time-clock", Badge = "AUTO" }, new NavigationMenuItem { Title = "Resumen asistencia", Route = "/human-resources/attendance-daily-summaries", Badge = "AUTO" }, new NavigationMenuItem { Title = "Documentos empleados", Route = "/human-resources/employee-documents", Badge = "DOC" }, new NavigationMenuItem { Title = "Movimientos laborales", Route = "/human-resources/employee-movements", Badge = "FLOW" } ] },
            new() { Title = "Nómina", Items = [ new NavigationMenuItem { Title = "✦ Procesar nómina", Route = "/payroll/procesar", Badge = "MVP" }, new NavigationMenuItem { Title = "Periodos", Route = "/payroll/periods" }, new NavigationMenuItem { Title = "Conceptos", Route = "/payroll/concepts" }, new NavigationMenuItem { Title = "Procesamientos", Route = "/payroll/runs" }, new NavigationMenuItem { Title = "Detalle nómina", Route = "/payroll/run-lines" }, new NavigationMenuItem { Title = "Percepciones y deducciones", Route = "/payroll/run-line-details", Badge = "CFDI" }, new NavigationMenuItem { Title = "Préstamos", Route = "/payroll/loans", Badge = "AUTO" }, new NavigationMenuItem { Title = "Descuentos préstamos", Route = "/payroll/loan-deductions" }, new NavigationMenuItem { Title = "Control de recibos", Route = "/payroll/receipt-control", Badge = "NEW" }, new NavigationMenuItem { Title = "Recibos de nómina", Route = "/payroll/receipts" }, new NavigationMenuItem { Title = "Impresión nómina", Route = "/payroll/print-center" }, new NavigationMenuItem { Title = "Dispersiones bancarias", Route = "/payroll/dispersion-batches", Badge = "BANK" }, new NavigationMenuItem { Title = "Cierres de nómina", Route = "/payroll/run-closings", Badge = "FLOW" }, new NavigationMenuItem { Title = "Pólizas nómina", Route = "/payroll/accounting-postings", Badge = "GL" }, new NavigationMenuItem { Title = "Acumulados fiscales", Route = "/payroll/tax-accumulators", Badge = "FISC" }, new NavigationMenuItem { Title = "Conciliación fiscal", Route = "/payroll/fiscal-reconciliations", Badge = "SAT" } ] },
            new() { Title = "Plataforma SaaS", Items = [ new NavigationMenuItem { Title = "Tenants", Route = "/platform/tenants", Badge = "SaaS" }, new NavigationMenuItem { Title = "Planes SaaS", Route = "/platform/plans", Badge = "PLAN" }, new NavigationMenuItem { Title = "Suscripciones SaaS", Route = "/platform/subscriptions", Badge = "MRR" } ] },
            new() { Title = "Seguridad", Items = [ new NavigationMenuItem { Title = "Usuarios", Route = "/security/users" }, new NavigationMenuItem { Title = "Roles", Route = "/security/roles" }, new NavigationMenuItem { Title = "Permisos", Route = "/security/permissions", Badge = "RO" } ] },
            new() { Title = "Catálogos", Items = [ new NavigationMenuItem { Title = "Monedas", Route = "/catalogs/currencies" }, new NavigationMenuItem { Title = "Tipos de cambio", Route = "/catalogs/exchange-rates" }, new NavigationMenuItem { Title = "Impuestos", Route = "/catalogs/taxes" }, new NavigationMenuItem { Title = "Unidades", Route = "/catalogs/units" }, new NavigationMenuItem { Title = "Bancos", Route = "/catalogs/banks" }, new NavigationMenuItem { Title = "Países", Route = "/catalogs/countries" }, new NavigationMenuItem { Title = "Estados", Route = "/catalogs/states" }, new NavigationMenuItem { Title = "Ciudades", Route = "/catalogs/cities" } ] },
            new() { Title = "Terceros", Items = [ new NavigationMenuItem { Title = "Clientes", Route = "/third-parties/customers" }, new NavigationMenuItem { Title = "Proveedores", Route = "/third-parties/suppliers" }, new NavigationMenuItem { Title = "Contactos", Route = "/third-parties/contacts" }, new NavigationMenuItem { Title = "Direcciones", Route = "/third-parties/addresses" }, new NavigationMenuItem { Title = "Cuentas bancarias", Route = "/third-parties/bank-accounts" } ] },
            new() { Title = "Materiales", Items = [ new NavigationMenuItem { Title = "Centro de materiales", Route = "/materials/control-center", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Características de material", Route = "/products/material-characteristics", Badge = "NUEVO" }, new NavigationMenuItem { Title = "Tallas de material", Route = "/products/material-sizes", Badge = "NUEVO" }, new NavigationMenuItem { Title = "Familias de materiales", Route = "/products/material-families", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Subfamilias de materiales", Route = "/products/material-subfamilies", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Catálogo de materiales", Route = "/products/material-items", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Proveedores por material", Route = "/products/material-suppliers", Badge = "COST" }, new NavigationMenuItem { Title = "Histórico de costos", Route = "/products/material-supplier-cost-history", Badge = "COST" }, new NavigationMenuItem { Title = "Conversiones de unidad", Route = "/products/unit-conversions", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Fases de producción", Route = "/products/production-phases", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Componentes del producto", Route = "/products/product-components", Badge = "ORANGE" } ] },
            new() { Title = "Productos", Items = [ new NavigationMenuItem { Title = "Centro de productos", Route = "/products/orange-control-center", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Colores", Route = "/products/colors", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Manufacturas", Route = "/products/manufacturing-types", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Cascos", Route = "/products/toe-caps", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Color suela", Route = "/products/sole-colors", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Troqueles", Route = "/products/dies", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Troquel C. calidad", Route = "/products/quality-control-dies", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Foliados", Route = "/products/folio-patterns", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Centro técnico de producción", Route = "/products/production-center", Badge = "TECH" }, new NavigationMenuItem { Title = "Familias de productos", Route = "/products/families", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Hormas", Route = "/products/lasts", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Líneas", Route = "/products/lines", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Estilos", Route = "/products/styles", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Marcas", Route = "/products/brands" }, new NavigationMenuItem { Title = "Modelos", Route = "/products/models" }, new NavigationMenuItem { Title = "Corridas", Route = "/products/size-runs", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Bordados", Route = "/products/embroidery-patterns", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Perfiles de ingeniería", Route = "/products/item-engineering-profiles", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Productos terminados", Route = "/products/finished-products", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Materiales por producto", Route = "/products/finished-product-materials", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Consumos por producto (legacy)", Route = "/products/product-consumption-profiles", Badge = "LEGACY" }, new NavigationMenuItem { Title = "Plantillas de consumo", Route = "/products/consumption-templates", Badge = "NUEVO" }, new NavigationMenuItem { Title = "Insumos por producto", Route = "/products/finished-product-supplies", Badge = "NUEVO" }, new NavigationMenuItem { Title = "Distribución de tallas", Route = "/products/material-size-distributions", Badge = "NUEVO" }, new NavigationMenuItem { Title = "Explosión de materiales", Route = "/products/material-explosion", Badge = "NUEVO" }, new NavigationMenuItem { Title = "Fichas técnicas", Route = "/products/technical-sheets", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Hojas de costo", Route = "/products/cost-sheets", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Autorizaciones de producto", Route = "/products/authorizations", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Variaciones por talla", Route = "/products/size-consumption-variations", Badge = "ORANGE" }, new NavigationMenuItem { Title = "Categorías comerciales", Route = "/products/categories" }, new NavigationMenuItem { Title = "Items comerciales", Route = "/products/items" }, new NavigationMenuItem { Title = "Listas de precios", Route = "/products/price-lists" }, new NavigationMenuItem { Title = "Códigos de barras", Route = "/products/barcodes" } ] },
            new() { Title = "Compras", Items = [ new NavigationMenuItem { Title = "Requisiciones", Route = "/purchases/requisitions" }, new NavigationMenuItem { Title = "Órdenes de compra", Route = "/purchases/orders" }, new NavigationMenuItem { Title = "Recepciones", Route = "/purchases/receipts" }, new NavigationMenuItem { Title = "Facturas proveedor", Route = "/purchases/invoices" }, new NavigationMenuItem { Title = "Devoluciones compra", Route = "/purchases/returns" }, new NavigationMenuItem { Title = "Dashboard compras", Route = "/purchases/dashboard" } ] },
            new() { Title = "Inventario", Items = [ new NavigationMenuItem { Title = "Existencias", Route = "/inventory/stock-balances" }, new NavigationMenuItem { Title = "Kardex", Route = "/inventory/kardex" }, new NavigationMenuItem { Title = "Entradas", Route = "/inventory/entries" }, new NavigationMenuItem { Title = "Salidas", Route = "/inventory/exits" }, new NavigationMenuItem { Title = "Traspasos", Route = "/inventory/transfers" }, new NavigationMenuItem { Title = "Ajustes", Route = "/inventory/adjustments" }, new NavigationMenuItem { Title = "Conteos físicos", Route = "/inventory/physical-counts" }, new NavigationMenuItem { Title = "Lotes", Route = "/inventory/lots" }, new NavigationMenuItem { Title = "Series", Route = "/inventory/serials" }, new NavigationMenuItem { Title = "Dashboard inventario", Route = "/inventory/dashboard" }, new NavigationMenuItem { Title = "Impresión inventario", Route = "/inventory/print-center" } ] },
            new() { Title = "Producción", Items = [
                new NavigationMenuItem { Title = "Dashboard producción", Route = "/produccion/dashboard" },
                new NavigationMenuItem { Title = "Centro premium", Route = "/produccion/control-center", Badge = "KPI" },
                new NavigationMenuItem { Title = "Órdenes de producción", Route = "/produccion/ordenes", Badge = "FLOW" },
                new NavigationMenuItem { Title = "Programación semanal", Route = "/produccion/programacion", Badge = "PLAN" },
                new NavigationMenuItem { Title = "Explosión de materiales", Route = "/products/material-explosion", Badge = "PLAN" },
                new NavigationMenuItem { Title = "Vales de proceso", Route = "/produccion/vales", Badge = "FLOW" },
                new NavigationMenuItem { Title = "Destajo", Route = "/produccion/destajo", Badge = "OPS" },
                new NavigationMenuItem { Title = "Tarifas de destajo", Route = "/produccion/tarifas-destajo", Badge = "CAT" },
                new NavigationMenuItem { Title = "En proceso", Route = "/produccion/en-proceso", Badge = "RO" },
                new NavigationMenuItem { Title = "Control de calidad", Route = "/produccion/control-calidad", Badge = "QC" },
                new NavigationMenuItem { Title = "Celdas de producción", Route = "/produccion/celdas", Badge = "CAT" },
                new NavigationMenuItem { Title = "Sobrantes", Route = "/produccion/sobrantes", Badge = "OPS" },
                new NavigationMenuItem { Title = "Reportes producción", Route = "/produccion/reportes" }
            ] },
            new() { Title = "Ventas", Items = [ new NavigationMenuItem { Title = "Cotizaciones", Route = "/sales/quotes" }, new NavigationMenuItem { Title = "Pedidos", Route = "/sales/orders" }, new NavigationMenuItem { Title = "Remisiones", Route = "/sales/shipments" }, new NavigationMenuItem { Title = "Facturas", Route = "/sales/invoices" }, new NavigationMenuItem { Title = "Devoluciones", Route = "/sales/returns" }, new NavigationMenuItem { Title = "Notas de crédito", Route = "/sales/credit-notes" }, new NavigationMenuItem { Title = "Dashboard ventas", Route = "/sales/dashboard" }, new NavigationMenuItem { Title = "Impresión ventas", Route = "/sales/print-center" } ] },
            new() { Title = "Servicios", Items = [ new NavigationMenuItem { Title = "Catálogo de servicios", Route = "/services/catalog", Badge = "SETUP" }, new NavigationMenuItem { Title = "Tarifas por cliente", Route = "/services/customer-rates", Badge = "HRS" }, new NavigationMenuItem { Title = "Notas de servicio", Route = "/services/notes", Badge = "DOC" }, new NavigationMenuItem { Title = "Impresión servicios", Route = "/services/print-center", Badge = "PRINT" }, new NavigationMenuItem { Title = "Clientes", Route = "/third-parties/customers", Badge = "CRM" } ] },
            new() { Title = "Tesorería", Items = [ new NavigationMenuItem { Title = "Cajas", Route = "/treasury/cash-accounts" }, new NavigationMenuItem { Title = "Bancos propios", Route = "/treasury/bank-accounts" }, new NavigationMenuItem { Title = "Ingresos", Route = "/treasury/incomes" }, new NavigationMenuItem { Title = "Egresos", Route = "/treasury/expenses" }, new NavigationMenuItem { Title = "Recibos", Route = "/treasury/receipts" }, new NavigationMenuItem { Title = "Pagos", Route = "/treasury/payments" }, new NavigationMenuItem { Title = "Conciliaciones", Route = "/treasury/reconciliations" }, new NavigationMenuItem { Title = "Dashboard tesorería", Route = "/treasury/dashboard" } ] },
            new() { Title = "CxC", Items = [ new NavigationMenuItem { Title = "Dashboard cobranza", Route = "/accounts-receivable/dashboard" }, new NavigationMenuItem { Title = "Saldos por cliente", Route = "/accounts-receivable/balances" }, new NavigationMenuItem { Title = "Estado de cuenta", Route = "/accounts-receivable/statements" }, new NavigationMenuItem { Title = "Antigüedad de saldos", Route = "/accounts-receivable/aging" }, new NavigationMenuItem { Title = "Aplicaciones de recibos", Route = "/accounts-receivable/applications" } ] },
            new() { Title = "CxP", Items = [ new NavigationMenuItem { Title = "Dashboard cuentas por pagar", Route = "/accounts-payable/dashboard" }, new NavigationMenuItem { Title = "Saldos por proveedor", Route = "/accounts-payable/balances" }, new NavigationMenuItem { Title = "Estado de cuenta", Route = "/accounts-payable/statements" }, new NavigationMenuItem { Title = "Antigüedad de saldos", Route = "/accounts-payable/aging" }, new NavigationMenuItem { Title = "Aplicaciones de pagos", Route = "/accounts-payable/applications" } ] },
            new() { Title = "CFDI", Items = [ new NavigationMenuItem { Title = "Dashboard CFDI", Route = "/cfdi/dashboard" }, new NavigationMenuItem { Title = "Documentos CFDI", Route = "/cfdi/documents" }, new NavigationMenuItem { Title = "Cola de timbrado", Route = "/cfdi/stamp-queue" }, new NavigationMenuItem { Title = "Cancelación", Route = "/cfdi/cancellation" }, new NavigationMenuItem { Title = "Configuración CFDI", Route = "/cfdi/configuration" } ] },
            new() { Title = "Contabilidad", Items = [ new NavigationMenuItem { Title = "Dashboard contable", Route = "/accounting/dashboard" }, new NavigationMenuItem { Title = "Cuentas contables", Route = "/accounting/chart-of-accounts" }, new NavigationMenuItem { Title = "Pólizas", Route = "/accounting/journal-entries" }, new NavigationMenuItem { Title = "Balanza", Route = "/accounting/trial-balance" }, new NavigationMenuItem { Title = "Auxiliar", Route = "/accounting/ledger" }, new NavigationMenuItem { Title = "Pólizas automáticas", Route = "/accounting/auto-policies" }, new NavigationMenuItem { Title = "Cierre mensual", Route = "/accounting/monthly-close" }, new NavigationMenuItem { Title = "Contabilizar documentos", Route = "/accounting/document-posting" } ] },
            new() { Title = "Finanzas", Items = [ new NavigationMenuItem { Title = "Centro financiero", Route = "/finance/executive-dashboard" }, new NavigationMenuItem { Title = "Flujo proyectado", Route = "/finance/cash-flow" }, new NavigationMenuItem { Title = "Calendario cobranza", Route = "/finance/collections-calendar" }, new NavigationMenuItem { Title = "Calendario pagos", Route = "/finance/payments-calendar" }, new NavigationMenuItem { Title = "Escenarios financieros", Route = "/finance/scenarios" }, new NavigationMenuItem { Title = "Compromisos de cobro", Route = "/finance/collection-commitments" }, new NavigationMenuItem { Title = "Programación de pagos", Route = "/finance/payment-schedule" }, new NavigationMenuItem { Title = "Plan semanal tesorería", Route = "/finance/weekly-treasury-plan" }, new NavigationMenuItem { Title = "Seguimiento financiero", Route = "/finance/commitment-follow-up" }, new NavigationMenuItem { Title = "Rentabilidad mensual", Route = "/finance/monthly-profitability" }, new NavigationMenuItem { Title = "Desempeño de cobranza", Route = "/finance/collections-performance" }, new NavigationMenuItem { Title = "Desempeño de pagos", Route = "/finance/payments-performance" }, new NavigationMenuItem { Title = "Concentración cartera", Route = "/finance/concentration-analysis" }, new NavigationMenuItem { Title = "Paquete directivo", Route = "/finance/board-pack" }, new NavigationMenuItem { Title = "Radar liquidez", Route = "/finance/liquidity-radar" }, new NavigationMenuItem { Title = "Ciclo conversión efectivo", Route = "/finance/cash-conversion-cycle" }, new NavigationMenuItem { Title = "Pruebas de estrés", Route = "/finance/stress-tests" }, new NavigationMenuItem { Title = "Comparativo anual", Route = "/finance/year-over-year" }, new NavigationMenuItem { Title = "Scorecard KPI", Route = "/finance/kpi-scorecard" }, new NavigationMenuItem { Title = "Puente capital trabajo", Route = "/finance/working-capital-bridge" }, new NavigationMenuItem { Title = "Rankings de variación", Route = "/finance/variation-rankings" }, new NavigationMenuItem { Title = "Presupuestos", Route = "/finance/budgets" }, new NavigationMenuItem { Title = "Real vs presupuesto", Route = "/finance/budget-vs-actual" }, new NavigationMenuItem { Title = "Metas mensuales", Route = "/finance/monthly-goals" }, new NavigationMenuItem { Title = "Autorizaciones", Route = "/finance/authorizations" }, new NavigationMenuItem { Title = "Alertas", Route = "/finance/alerts" }, new NavigationMenuItem { Title = "Semáforos", Route = "/finance/semaphores" }, new NavigationMenuItem { Title = "Control documental", Route = "/finance/document-control" }, new NavigationMenuItem { Title = "Excepciones", Route = "/finance/exceptions" }, new NavigationMenuItem { Title = "Centro de acciones", Route = "/finance/action-center" }, new NavigationMenuItem { Title = "Cockpit de cierre", Route = "/finance/closing-cockpit" }, new NavigationMenuItem { Title = "Covenants", Route = "/finance/covenants" }, new NavigationMenuItem { Title = "Acuerdos ejecutivos", Route = "/finance/executive-agreements" }, new NavigationMenuItem { Title = "Forecast rolling", Route = "/finance/rolling-forecast" }, new NavigationMenuItem { Title = "Política de crédito", Route = "/finance/credit-policy" }, new NavigationMenuItem { Title = "Riesgo proveedores", Route = "/finance/supplier-risk" }, new NavigationMenuItem { Title = "Matriz recuperación", Route = "/finance/recovery-matrix" }, new NavigationMenuItem { Title = "Radar clientes", Route = "/finance/customer-radar" }, new NavigationMenuItem { Title = "Radar proveedores", Route = "/finance/supplier-radar" }, new NavigationMenuItem { Title = "Pulso sucursales", Route = "/finance/branch-pulse" }, new NavigationMenuItem { Title = "Puente liquidez mensual", Route = "/finance/monthly-liquidity-bridge" }, new NavigationMenuItem { Title = "Sala de decisión", Route = "/finance/decision-room" }, new NavigationMenuItem { Title = "Cliente 360", Route = "/finance/customer-control-tower" }, new NavigationMenuItem { Title = "Proveedor 360", Route = "/finance/supplier-control-tower" }, new NavigationMenuItem { Title = "Mando sucursales", Route = "/finance/branch-command-center" }, new NavigationMenuItem { Title = "Comité forecast", Route = "/finance/forecast-committee" } ] },
            new() { Title = "Reportes", Items = [ new NavigationMenuItem { Title = "Operativos", Route = "/reports/operational" }, new NavigationMenuItem { Title = "Ejecutivos", Route = "/reports/executive" } ] },
            new() { Title = "Auditoría", Items = [ new NavigationMenuItem { Title = "Bitácora de cambios", Route = "/audit/change-log", Badge = "RO" }, new NavigationMenuItem { Title = "Bitácora documental", Route = "/audit/document-log", Badge = "RO" } ] },
            new() { Title = "Monitoreo", Items = [ new NavigationMenuItem { Title = "Errores", Route = "/monitoring/errors", Badge = "RO" }, new NavigationMenuItem { Title = "Revisión de seguridad", Route = "/monitoring/security-review", Badge = "RO" }, new NavigationMenuItem { Title = "Salud del sistema", Route = "/monitoring/health", Badge = "RO" } ] },
            new() { Title = "Administración", Items = [ new NavigationMenuItem { Title = "Sesiones", Route = "/administration/sessions", Badge = "RO" }, new NavigationMenuItem { Title = "Bitácora", Route = "/administration/access-logs", Badge = "RO" }, new NavigationMenuItem { Title = "Series documentales", Route = "/administration/document-series" }, new NavigationMenuItem { Title = "Folios documentales", Route = "/administration/document-folios" }, new NavigationMenuItem { Title = "Configuración empresa", Route = "/administration/company-settings" }, new NavigationMenuItem { Title = "Impresión compras", Route = "/purchases/print-center" } ] }
        };

        foreach (var group in menu)
        {
            foreach (var item in group.Items)
            {
                item.Module ??= group.Title;
            }
        }

        if (!_authState.IsPlatformOwner)
        {
            menu = menu
                .Where(group => !string.Equals(group.Title, "Plataforma SaaS", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return menu;
    }

    public string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/dashboard";
        }

        value = value.Replace("~", string.Empty).Trim();
        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        if (value.Length > 1 && value.EndsWith('/'))
        {
            value = value[..^1];
        }

        return value.ToLowerInvariant();
    }

    public NavigationMenuGroup? GetActiveGroup(string? route)
    {
        var normalized = Normalize(route);
        var menu = GetMenu();

        return menu.FirstOrDefault(group => group.Items.Any(item => IsRouteMatch(normalized, item.Route)))
               ?? menu.FirstOrDefault();
    }

    public NavigationMenuItem? GetActiveItem(string? route)
    {
        var normalized = Normalize(route);

        return GetMenu()
            .SelectMany(group => group.Items)
            .FirstOrDefault(item => IsRouteMatch(normalized, item.Route));
    }

    public IReadOnlyList<NavigationMenuItem> GetAllItems()
        => GetMenu().SelectMany(group => group.Items).ToList();

    public List<NavigationMenuItem> Search(string? term)
    {
        var query = NormalizeSearch(term);
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<NavigationMenuItem>();
        }

        return GetAllItems()
            .Select(item => new
            {
                Item = item,
                Score = Score(item, query)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.Module)
            .ThenBy(x => x.Item.Title)
            .Select(x => x.Item)
            .ToList();
    }

    private bool IsRouteMatch(string normalizedRoute, string? itemRoute)
    {
        var item = Normalize(itemRoute);
        return normalizedRoute == item || normalizedRoute.StartsWith(item + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static int Score(NavigationMenuItem item, string query)
    {
        var title = NormalizeSearch(item.Title);
        var route = NormalizeSearch(item.Route);
        var module = NormalizeSearch(item.Module);
        var badge = NormalizeSearch(item.Badge);

        var score = 0;

        if (title == query) score += 400;
        if (title.StartsWith(query, StringComparison.Ordinal)) score += 240;
        if (title.Contains(query, StringComparison.Ordinal)) score += 180;
        if (module.StartsWith(query, StringComparison.Ordinal)) score += 130;
        if (module.Contains(query, StringComparison.Ordinal)) score += 100;
        if (route.Contains(query, StringComparison.Ordinal)) score += 95;
        if (badge.Contains(query, StringComparison.Ordinal)) score += 60;

        foreach (var token in query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (title.StartsWith(token, StringComparison.Ordinal)) score += 75;
            else if (title.Contains(token, StringComparison.Ordinal)) score += 45;

            if (module.Contains(token, StringComparison.Ordinal)) score += 24;
            if (route.Contains(token, StringComparison.Ordinal)) score += 22;
            if (badge.Contains(token, StringComparison.Ordinal)) score += 15;
        }

        return score;
    }

    private static string NormalizeSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray())
            .ToLowerInvariant()
            .Trim();
    }
}


public sealed class NavigationMenuGroup
{
    public string Title { get; set; } = string.Empty;
    public List<NavigationMenuItem> Items { get; set; } = new();
}

public sealed class NavigationMenuItem
{
    public string Title { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string? Badge { get; set; }
    public string? Module { get; set; }
}
