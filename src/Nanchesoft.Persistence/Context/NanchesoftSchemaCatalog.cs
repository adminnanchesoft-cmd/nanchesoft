using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Context;

public static class NanchesoftSchemaCatalog
{
    public const string AccountingSchema = "accounting";
    public const string AuthSchema = "auth";
    public const string CatalogSchema = "catalog";
    public const string ConfigSchema = "config";
    public const string CoreSchema = "core";
    public const string FinanceSchema = "finance";
    public const string HrSchema = "hr";
    public const string InventorySchema = "inventory";
    public const string OrgSchema = "org";
    public const string PayrollSchema = "payroll";
    public const string ProductSchema = "product";
    public const string PurchaseSchema = "purchase";
    public const string SalesSchema = "sales";
    public const string SubscriptionSchema = "subscription";
    public const string ProductionSchema = "production";

    public static IReadOnlyList<string> AllSchemas { get; } = new[]
    {
        AccountingSchema,
        AuthSchema,
        CatalogSchema,
        ConfigSchema,
        CoreSchema,
        FinanceSchema,
        HrSchema,
        InventorySchema,
        OrgSchema,
        PayrollSchema,
        ProductSchema,
        ProductionSchema,
        PurchaseSchema,
        SalesSchema,
        SubscriptionSchema,
    };

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>().ToTable("tenants", CoreSchema);
        modelBuilder.Entity<Plan>().ToTable("plans", SubscriptionSchema);
        modelBuilder.Entity<SubscriptionCharge>().ToTable("subscription_charges", SubscriptionSchema);
        modelBuilder.Entity<Company>().ToTable("companies", CoreSchema);
        modelBuilder.Entity<Branch>().ToTable("branches", CoreSchema);
        modelBuilder.Entity<Warehouse>().ToTable("warehouses", InventorySchema);
        modelBuilder.Entity<User>().ToTable("users", AuthSchema);
        modelBuilder.Entity<Role>().ToTable("roles", AuthSchema);
        modelBuilder.Entity<Permission>().ToTable("permissions", AuthSchema);
        modelBuilder.Entity<UserRole>().ToTable("user_roles", AuthSchema);
        modelBuilder.Entity<RolePermission>().ToTable("role_permissions", AuthSchema);
        modelBuilder.Entity<UserSession>().ToTable("user_sessions", AuthSchema);
        modelBuilder.Entity<AccessLog>().ToTable("access_logs", AuthSchema);
        modelBuilder.Entity<NavigationItem>().ToTable("navigation_items", CoreSchema);
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs", CoreSchema);
        modelBuilder.Entity<ErrorLog>().ToTable("error_logs", CoreSchema);
        modelBuilder.Entity<Currency>().ToTable("currencies", CatalogSchema);
        modelBuilder.Entity<ExchangeRate>().ToTable("exchange_rates", CatalogSchema);
        modelBuilder.Entity<Tax>().ToTable("taxes", CatalogSchema);
        modelBuilder.Entity<Unit>().ToTable("units", CatalogSchema);
        modelBuilder.Entity<Bank>().ToTable("banks", CatalogSchema);
        modelBuilder.Entity<Country>().ToTable("countries", CatalogSchema);
        modelBuilder.Entity<State>().ToTable("states", CatalogSchema);
        modelBuilder.Entity<City>().ToTable("cities", CatalogSchema);
        modelBuilder.Entity<DocumentSeries>().ToTable("document_series", CoreSchema);
        modelBuilder.Entity<DocumentFolio>().ToTable("document_folios", CoreSchema);
        modelBuilder.Entity<CompanySetting>().ToTable("company_settings", ConfigSchema);
        modelBuilder.Entity<Customer>().ToTable("customers", OrgSchema);
        modelBuilder.Entity<CustomerLegalEntity>().ToTable("customer_legal_entities", OrgSchema);
        modelBuilder.Entity<Supplier>().ToTable("suppliers", OrgSchema);
        modelBuilder.Entity<ThirdPartyContact>().ToTable("third_party_contacts", OrgSchema);
        modelBuilder.Entity<ThirdPartyAddress>().ToTable("third_party_addresses", OrgSchema);
        modelBuilder.Entity<ThirdPartyBankAccount>().ToTable("third_party_bank_accounts", OrgSchema);
        modelBuilder.Entity<ItemCategory>().ToTable("item_categories", ProductSchema);
        modelBuilder.Entity<ItemBrand>().ToTable("item_brands", ProductSchema);
        modelBuilder.Entity<ItemModel>().ToTable("item_models", ProductSchema);
        modelBuilder.Entity<Item>().ToTable("items", ProductSchema);
        modelBuilder.Entity<ItemPriceList>().ToTable("item_price_lists", ProductSchema);
        modelBuilder.Entity<ItemPriceListDetail>().ToTable("item_price_list_details", ProductSchema);
        modelBuilder.Entity<ItemBarcode>().ToTable("item_barcodes", ProductSchema);
        modelBuilder.Entity<UnitConversion>().ToTable("unit_conversions", CatalogSchema);
        modelBuilder.Entity<ProductSizeRun>().ToTable("product_size_runs", ProductSchema);
        modelBuilder.Entity<ProductSizeRunSize>().ToTable("product_size_run_sizes", ProductSchema);
        modelBuilder.Entity<ProductFamily>().ToTable("product_families", ProductSchema);
        modelBuilder.Entity<ProductLast>().ToTable("product_lasts", ProductSchema);
        modelBuilder.Entity<ProductLine>().ToTable("product_lines", ProductSchema);
        modelBuilder.Entity<ProductStyle>().ToTable("product_styles", ProductSchema);
        modelBuilder.Entity<ProductColor>().ToTable("product_colors", ProductSchema);
        modelBuilder.Entity<ProductManufacturingType>().ToTable("product_manufacturing_types", ProductSchema);
        modelBuilder.Entity<ProductToeCap>().ToTable("product_toe_caps", ProductSchema);
        modelBuilder.Entity<ProductSoleColor>().ToTable("product_sole_colors", ProductSchema);
        modelBuilder.Entity<ProductDie>().ToTable("product_dies", ProductSchema);
        modelBuilder.Entity<QualityControlDie>().ToTable("quality_control_dies", ProductSchema);
        modelBuilder.Entity<ProductLeatherType>().ToTable("product_leather_types", ProductSchema);
        modelBuilder.Entity<ProductSole>().ToTable("product_soles", ProductSchema);
        modelBuilder.Entity<ProductFolioPattern>().ToTable("product_folio_patterns", ProductSchema);
        modelBuilder.Entity<EmbroideryPattern>().ToTable("embroidery_patterns", ProductSchema);
        modelBuilder.Entity<ItemEngineeringProfile>().ToTable("item_engineering_profiles", ProductSchema);
        modelBuilder.Entity<ProductionPhase>().ToTable("production_phases", ProductSchema);

        // Production module — operational tables
        modelBuilder.Entity<ProductionCell>().ToTable("production_cells", ProductionSchema);
        modelBuilder.Entity<ProductionCellEmployee>().ToTable("production_cell_employees", ProductionSchema);
        modelBuilder.Entity<ProductionOrder>().ToTable("production_orders", ProductionSchema);
        modelBuilder.Entity<ProductionOrderLine>().ToTable("production_order_lines", ProductionSchema);
        modelBuilder.Entity<ProductionSchedule>().ToTable("production_schedules", ProductionSchema);
        modelBuilder.Entity<ProductionScheduleLine>().ToTable("production_schedule_lines", ProductionSchema);
        modelBuilder.Entity<ProductionPhaseProgress>().ToTable("production_phase_progress", ProductionSchema);
        modelBuilder.Entity<ProductionVoucher>().ToTable("production_vouchers", ProductionSchema);
        modelBuilder.Entity<ProductionVoucherDetail>().ToTable("production_voucher_details", ProductionSchema);
        modelBuilder.Entity<PieceWorkRate>().ToTable("piece_work_rates", ProductionSchema);
        modelBuilder.Entity<PieceWorkRecord>().ToTable("piece_work_records", ProductionSchema);
        modelBuilder.Entity<MaterialRequirement>().ToTable("material_requirements", ProductionSchema);
        modelBuilder.Entity<MaterialRequirementLine>().ToTable("material_requirement_lines", ProductionSchema);
        modelBuilder.Entity<ProductionInProcess>().ToTable("production_in_process", ProductionSchema);
        modelBuilder.Entity<SurplusRecord>().ToTable("surplus_records", ProductionSchema);
        modelBuilder.Entity<PhaseRestriction>().ToTable("phase_restrictions", ProductionSchema);
        modelBuilder.Entity<MaterialCharacteristic>().ToTable("material_characteristics", ProductSchema);
        modelBuilder.Entity<MaterialSize>().ToTable("material_sizes", ProductSchema);
        modelBuilder.Entity<MaterialFamily>().ToTable("material_families", ProductSchema);
        modelBuilder.Entity<MaterialSubfamily>().ToTable("material_subfamilies", ProductSchema);
        modelBuilder.Entity<MaterialItem>().ToTable("material_items", ProductSchema);
        modelBuilder.Entity<MaterialSupplierAssignment>().ToTable("material_supplier_assignments", ProductSchema);
        modelBuilder.Entity<MaterialSupplierCostHistory>().ToTable("material_supplier_cost_history", ProductSchema);
        modelBuilder.Entity<FinishedProduct>().ToTable("finished_products", ProductSchema);
        modelBuilder.Entity<ProductComponent>().ToTable("product_components", ProductSchema);
        modelBuilder.Entity<FinishedProductMaterial>().ToTable("finished_product_materials", ProductSchema);
        modelBuilder.Entity<ProductConsumptionProfile>().ToTable("product_consumption_profiles", ProductSchema);
        modelBuilder.Entity<ConsumptionTemplate>().ToTable("consumption_templates", ProductSchema);
        modelBuilder.Entity<ConsumptionTemplateDetail>().ToTable("consumption_template_details", ProductSchema);
        modelBuilder.Entity<ConsumptionTemplateSize>().ToTable("consumption_template_sizes", ProductSchema);
        modelBuilder.Entity<FinishedProductSupply>().ToTable("finished_product_supplies", ProductSchema);
        modelBuilder.Entity<FinishedProductSupplySize>().ToTable("finished_product_supply_sizes", ProductSchema);
        modelBuilder.Entity<MaterialSizeDistribution>().ToTable("material_size_distributions", ProductSchema);
        modelBuilder.Entity<MaterialSizeDistributionDetail>().ToTable("material_size_distribution_details", ProductSchema);
        modelBuilder.Entity<ProductVariant>().ToTable("product_variants", ProductSchema);
        modelBuilder.Entity<ProductTechnicalSheet>().ToTable("product_technical_sheets", ProductSchema);
        modelBuilder.Entity<ProductTechnicalSheetMaterial>().ToTable("product_technical_sheet_materials", ProductSchema);
        modelBuilder.Entity<ProductTechnicalSheetProcess>().ToTable("product_technical_sheet_processes", ProductSchema);
        modelBuilder.Entity<ProductCostSheet>().ToTable("product_cost_sheets", ProductSchema);
        modelBuilder.Entity<ProductAuthorizationRecord>().ToTable("product_authorization_records", ProductSchema);
        modelBuilder.Entity<ProductSizeConsumptionVariation>().ToTable("product_size_consumption_variations", ProductSchema);
        modelBuilder.Entity<Department>().ToTable("hr_departments", HrSchema);
        modelBuilder.Entity<Position>().ToTable("hr_positions", HrSchema);
        modelBuilder.Entity<Employee>().ToTable("hr_employees", HrSchema);
        modelBuilder.Entity<HrBank>().ToTable("hr_banks", HrSchema);
        modelBuilder.Entity<HrTerminationReason>().ToTable("hr_termination_reasons", HrSchema);
        modelBuilder.Entity<HrEmployerRegistration>().ToTable("hr_employer_registrations", HrSchema);
        modelBuilder.Entity<EmployeeContract>().ToTable("employee_contracts", HrSchema);
        modelBuilder.Entity<EmployeeIncident>().ToTable("hr_employee_incidents", HrSchema);
        modelBuilder.Entity<PayrollPeriodType>().ToTable("payroll_period_types", PayrollSchema);
        modelBuilder.Entity<PayrollPeriod>().ToTable("payroll_periods", PayrollSchema);
        modelBuilder.Entity<PayrollConcept>().ToTable("payroll_concepts", PayrollSchema);
        modelBuilder.Entity<PayrollRun>().ToTable("payroll_runs", PayrollSchema);
        modelBuilder.Entity<PayrollRunLine>().ToTable("payroll_run_lines", PayrollSchema);
        modelBuilder.Entity<PayrollRunLineDetail>().ToTable("payroll_run_line_details", PayrollSchema);
        modelBuilder.Entity<AttendancePunch>().ToTable("hr_attendance_punches", HrSchema);
        modelBuilder.Entity<PayrollRecurringMovement>().ToTable("payroll_recurring_movements", PayrollSchema);
        modelBuilder.Entity<EmployeeLoan>().ToTable("employee_loans", PayrollSchema);
        modelBuilder.Entity<EmployeeLoanDeduction>().ToTable("employee_loan_deductions", PayrollSchema);
        modelBuilder.Entity<WorkShift>().ToTable("hr_work_shifts", HrSchema);
        modelBuilder.Entity<WorkSchedule>().ToTable("hr_work_schedules", HrSchema);
        modelBuilder.Entity<TimeClockDevice>().ToTable("hr_time_clock_devices", HrSchema);
        modelBuilder.Entity<LeaveType>().ToTable("hr_leave_types", HrSchema);
        modelBuilder.Entity<VacationRequest>().ToTable("hr_vacation_requests", HrSchema);
        modelBuilder.Entity<EmployeeDocumentRecord>().ToTable("hr_employee_documents", HrSchema);
        modelBuilder.Entity<EmployeeLaborMovement>().ToTable("hr_employee_movements", HrSchema);
        modelBuilder.Entity<EmployeeCertificationRecord>().ToTable("hr_employee_certifications", HrSchema);
        modelBuilder.Entity<RecruitmentVacancy>().ToTable("hr_recruitment_vacancies", HrSchema);
        modelBuilder.Entity<CandidateApplication>().ToTable("hr_candidate_applications", HrSchema);
        modelBuilder.Entity<OnboardingChecklistRecord>().ToTable("hr_onboarding_checklists", HrSchema);
        modelBuilder.Entity<EmployeePerformanceReview>().ToTable("hr_employee_performance_reviews", HrSchema);
        modelBuilder.Entity<EmployeeCompetencyAssessment>().ToTable("hr_employee_competency_assessments", HrSchema);
        modelBuilder.Entity<SuccessionPlanRecord>().ToTable("hr_succession_plan_records", HrSchema);
        modelBuilder.Entity<AttendanceDailySummary>().ToTable("payroll_attendance_daily_summaries", PayrollSchema);
        modelBuilder.Entity<PrePayrollAdjustment>().ToTable("payroll_prepayroll_adjustments", PayrollSchema);
        modelBuilder.Entity<PrePayrollCutoff>().ToTable("payroll_prepayroll_cutoffs", PayrollSchema);
        modelBuilder.Entity<PayrollSourceApplication>().ToTable("payroll_source_applications", PayrollSchema);
        modelBuilder.Entity<PayrollReceiptControl>().ToTable("payroll_receipt_controls", PayrollSchema);
        modelBuilder.Entity<PayrollRunClosing>().ToTable("payroll_run_closings", PayrollSchema);
        modelBuilder.Entity<PayrollDispersionBatch>().ToTable("payroll_dispersion_batches", PayrollSchema);
        modelBuilder.Entity<PayrollDispersionLine>().ToTable("payroll_dispersion_lines", PayrollSchema);
        modelBuilder.Entity<PayrollAccountingPosting>().ToTable("payroll_accounting_postings", PayrollSchema);
        modelBuilder.Entity<PayrollTaxAccumulator>().ToTable("payroll_tax_accumulators", PayrollSchema);
        modelBuilder.Entity<PayrollEmployerObligation>().ToTable("payroll_employer_obligations", PayrollSchema);
        modelBuilder.Entity<PayrollFiscalReconciliation>().ToTable("payroll_fiscal_reconciliations", PayrollSchema);
        modelBuilder.Entity<PurchaseRequisition>().ToTable("purchase_requisitions", PurchaseSchema);
        modelBuilder.Entity<PurchaseRequisitionLine>().ToTable("purchase_requisition_lines", PurchaseSchema);
        modelBuilder.Entity<PurchaseOrder>().ToTable("purchase_orders", PurchaseSchema);
        modelBuilder.Entity<PurchaseOrderLine>().ToTable("purchase_order_lines", PurchaseSchema);
        modelBuilder.Entity<PurchaseReceipt>().ToTable("purchase_receipts", PurchaseSchema);
        modelBuilder.Entity<PurchaseReceiptLine>().ToTable("purchase_receipt_lines", PurchaseSchema);
        modelBuilder.Entity<PurchaseInvoice>().ToTable("purchase_invoices", PurchaseSchema);
        modelBuilder.Entity<PurchaseInvoiceLine>().ToTable("purchase_invoice_lines", PurchaseSchema);
        modelBuilder.Entity<PurchaseReturn>().ToTable("purchase_returns", PurchaseSchema);
        modelBuilder.Entity<PurchaseReturnLine>().ToTable("purchase_return_lines", PurchaseSchema);
        modelBuilder.Entity<PurchasePayment>().ToTable("purchase_payments", PurchaseSchema);
        modelBuilder.Entity<PurchaseReceiptDiff>().ToTable("purchase_receipt_diffs", PurchaseSchema);
        modelBuilder.Entity<PurchaseReceiptDiffLine>().ToTable("purchase_receipt_diff_lines", PurchaseSchema);
        modelBuilder.Entity<MaterialStockBalance>().ToTable("material_stock_balances", InventorySchema);
        modelBuilder.Entity<MaterialInventoryMovement>().ToTable("material_inventory_movements", InventorySchema);
        modelBuilder.Entity<StockBalance>().ToTable("stock_balances", InventorySchema);
        modelBuilder.Entity<InventoryMovement>().ToTable("inventory_movements", InventorySchema);
        modelBuilder.Entity<InventoryEntry>().ToTable("inventory_entries", InventorySchema);
        modelBuilder.Entity<InventoryEntryLine>().ToTable("inventory_entry_lines", InventorySchema);
        modelBuilder.Entity<InventoryExit>().ToTable("inventory_exits", InventorySchema);
        modelBuilder.Entity<InventoryExitLine>().ToTable("inventory_exit_lines", InventorySchema);
        modelBuilder.Entity<InventoryTransfer>().ToTable("inventory_transfers", InventorySchema);
        modelBuilder.Entity<InventoryTransferLine>().ToTable("inventory_transfer_lines", InventorySchema);
        modelBuilder.Entity<InventoryAdjustment>().ToTable("inventory_adjustments", InventorySchema);
        modelBuilder.Entity<InventoryAdjustmentLine>().ToTable("inventory_adjustment_lines", InventorySchema);
        modelBuilder.Entity<PhysicalCount>().ToTable("physical_counts", InventorySchema);
        modelBuilder.Entity<PhysicalCountLine>().ToTable("physical_count_lines", InventorySchema);
        modelBuilder.Entity<ItemLot>().ToTable("item_lots", InventorySchema);
        modelBuilder.Entity<ItemSerial>().ToTable("item_serials", InventorySchema);
        modelBuilder.Entity<SalesQuote>().ToTable("sales_quotes", SalesSchema);
        modelBuilder.Entity<SalesQuoteLine>().ToTable("sales_quote_lines", SalesSchema);
        modelBuilder.Entity<SalesOrder>().ToTable("sales_orders", SalesSchema);
        modelBuilder.Entity<SalesOrderLine>().ToTable("sales_order_lines", SalesSchema);
        modelBuilder.Entity<SalesShipment>().ToTable("sales_shipments", SalesSchema);
        modelBuilder.Entity<SalesShipmentLine>().ToTable("sales_shipment_lines", SalesSchema);
        modelBuilder.Entity<SalesInvoice>().ToTable("sales_invoices", SalesSchema);
        modelBuilder.Entity<SalesInvoiceLine>().ToTable("sales_invoice_lines", SalesSchema);
        modelBuilder.Entity<SalesReturn>().ToTable("sales_returns", SalesSchema);
        modelBuilder.Entity<SalesReturnLine>().ToTable("sales_return_lines", SalesSchema);
        modelBuilder.Entity<CreditNote>().ToTable("credit_notes", SalesSchema);
        modelBuilder.Entity<CreditNoteLine>().ToTable("credit_note_lines", SalesSchema);
        modelBuilder.Entity<ServiceCatalogItem>().ToTable("service_catalog_items", SalesSchema);
        modelBuilder.Entity<CustomerServiceRate>().ToTable("customer_service_rates", SalesSchema);
        modelBuilder.Entity<ServiceNote>().ToTable("service_notes", SalesSchema);
        modelBuilder.Entity<CashAccount>().ToTable("cash_accounts", FinanceSchema);
        modelBuilder.Entity<BankAccount>().ToTable("bank_accounts", FinanceSchema);
        modelBuilder.Entity<CashMovement>().ToTable("cash_movements", FinanceSchema);
        modelBuilder.Entity<BankMovement>().ToTable("bank_movements", FinanceSchema);
        modelBuilder.Entity<TreasuryIncome>().ToTable("treasury_incomes", FinanceSchema);
        modelBuilder.Entity<TreasuryIncomeLine>().ToTable("treasury_income_lines", FinanceSchema);
        modelBuilder.Entity<TreasuryExpense>().ToTable("treasury_expenses", FinanceSchema);
        modelBuilder.Entity<TreasuryExpenseLine>().ToTable("treasury_expense_lines", FinanceSchema);
        modelBuilder.Entity<Receipt>().ToTable("receipts", FinanceSchema);
        modelBuilder.Entity<ReceiptLine>().ToTable("receipt_lines", FinanceSchema);
        modelBuilder.Entity<Payment>().ToTable("payments", FinanceSchema);
        modelBuilder.Entity<PaymentLine>().ToTable("payment_lines", FinanceSchema);
        modelBuilder.Entity<Reconciliation>().ToTable("reconciliations", FinanceSchema);
        modelBuilder.Entity<ReconciliationLine>().ToTable("reconciliation_lines", FinanceSchema);
        modelBuilder.Entity<BankStatement>().ToTable("bank_statements", FinanceSchema);
        modelBuilder.Entity<BankStatementEntry>().ToTable("bank_statement_entries", FinanceSchema);
        modelBuilder.Entity<InternalTransfer>().ToTable("internal_transfers", FinanceSchema);
        modelBuilder.Entity<CheckBook>().ToTable("check_books", FinanceSchema);
        modelBuilder.Entity<Check>().ToTable("checks", FinanceSchema);
        modelBuilder.Entity<FinanceMovementType>().ToTable("movement_types", FinanceSchema);
        modelBuilder.Entity<FinanceConcept>().ToTable("concepts", FinanceSchema);
        modelBuilder.Entity<PaymentBatch>().ToTable("payment_batches", FinanceSchema);
        modelBuilder.Entity<PaymentBatchLine>().ToTable("payment_batch_lines", FinanceSchema);
        modelBuilder.Entity<PaymentBatchAudit>().ToTable("payment_batch_audits", FinanceSchema);
        modelBuilder.Entity<AccountsReceivableAccount>().ToTable("accounts_receivable_accounts", FinanceSchema);
        modelBuilder.Entity<AccountsReceivableMovement>().ToTable("accounts_receivable_movements", FinanceSchema);
        modelBuilder.Entity<ReceiptApplication>().ToTable("receipt_applications", FinanceSchema);
        modelBuilder.Entity<UserThemePreference>().ToTable("user_theme_preferences", AuthSchema);
        modelBuilder.Entity<AccountingAccount>().ToTable("accounting_accounts", AccountingSchema);
        modelBuilder.Entity<AccountingFiscalPeriod>().ToTable("accounting_fiscal_periods", AccountingSchema);
        modelBuilder.Entity<AccountingJournalEntry>().ToTable("accounting_journal_entries", AccountingSchema);
        modelBuilder.Entity<AccountingJournalEntryLine>().ToTable("accounting_journal_entry_lines", AccountingSchema);
        modelBuilder.Entity<AccountingLedgerSnapshot>().ToTable("accounting_ledger_snapshots", AccountingSchema);
    }

    public static string BuildEnsureSchemasSql()
    {
        return string.Join(Environment.NewLine, AllSchemas.Select(schema => $"CREATE SCHEMA IF NOT EXISTS \"{schema}\";"));
    }
}