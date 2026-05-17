using Microsoft.EntityFrameworkCore;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Context;

public sealed class NanchesoftDbContext : DbContext
{
    public NanchesoftDbContext(DbContextOptions<NanchesoftDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<SubscriptionCharge> SubscriptionCharges => Set<SubscriptionCharge>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();
    public DbSet<NavigationItem> NavigationItems => Set<NavigationItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Tax> Taxes => Set<Tax>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<DocumentSeries> DocumentSeries => Set<DocumentSeries>();
    public DbSet<DocumentFolio> DocumentFolios => Set<DocumentFolio>();
    public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ThirdPartyContact> ThirdPartyContacts => Set<ThirdPartyContact>();
    public DbSet<ThirdPartyAddress> ThirdPartyAddresses => Set<ThirdPartyAddress>();
    public DbSet<ThirdPartyBankAccount> ThirdPartyBankAccounts => Set<ThirdPartyBankAccount>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<ItemBrand> ItemBrands => Set<ItemBrand>();
    public DbSet<ItemModel> ItemModels => Set<ItemModel>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemPriceList> ItemPriceLists => Set<ItemPriceList>();
    public DbSet<ItemPriceListDetail> ItemPriceListDetails => Set<ItemPriceListDetail>();
    public DbSet<ItemBarcode> ItemBarcodes => Set<ItemBarcode>();

    // Orange / Silvasoft product engineering foundation
    public DbSet<UnitConversion> UnitConversions => Set<UnitConversion>();
    public DbSet<ProductSizeRun> ProductSizeRuns => Set<ProductSizeRun>();
    public DbSet<ProductSizeRunSize> ProductSizeRunSizes => Set<ProductSizeRunSize>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductFamily> ProductFamilies => Set<ProductFamily>();
    public DbSet<ProductLast> ProductLasts => Set<ProductLast>();
    public DbSet<ProductLine> ProductLines => Set<ProductLine>();
    public DbSet<ProductStyle> ProductStyles => Set<ProductStyle>();
    public DbSet<ProductColor> ProductColors => Set<ProductColor>();
    public DbSet<ProductManufacturingType> ProductManufacturingTypes => Set<ProductManufacturingType>();
    public DbSet<ProductToeCap> ProductToeCaps => Set<ProductToeCap>();
    public DbSet<ProductSoleColor> ProductSoleColors => Set<ProductSoleColor>();
    public DbSet<ProductDie> ProductDies => Set<ProductDie>();
    public DbSet<QualityControlDie> QualityControlDies => Set<QualityControlDie>();
    public DbSet<ProductLeatherType> ProductLeatherTypes => Set<ProductLeatherType>();
    public DbSet<ProductSole> ProductSoles => Set<ProductSole>();
    public DbSet<ProductFolioPattern> ProductFolioPatterns => Set<ProductFolioPattern>();
    public DbSet<EmbroideryPattern> EmbroideryPatterns => Set<EmbroideryPattern>();
    public DbSet<ItemEngineeringProfile> ItemEngineeringProfiles => Set<ItemEngineeringProfile>();
    public DbSet<ProcessVoucher> ProcessVouchers => Set<ProcessVoucher>();

    // Orange / Silvasoft product catalog operations
    public DbSet<ProductionPhase> ProductionPhases => Set<ProductionPhase>();
    public DbSet<MaterialCharacteristic> MaterialCharacteristics => Set<MaterialCharacteristic>();
    public DbSet<MaterialSize> MaterialSizes => Set<MaterialSize>();
    public DbSet<MaterialFamily> MaterialFamilies => Set<MaterialFamily>();
    public DbSet<MaterialSubfamily> MaterialSubfamilies => Set<MaterialSubfamily>();
    public DbSet<MaterialItem> MaterialItems => Set<MaterialItem>();
    public DbSet<MaterialSupplierAssignment> MaterialSupplierAssignments => Set<MaterialSupplierAssignment>();
    public DbSet<MaterialSupplierCostHistory> MaterialSupplierCostHistory => Set<MaterialSupplierCostHistory>();
    public DbSet<FinishedProduct> FinishedProducts => Set<FinishedProduct>();
    public DbSet<ProductComponent> ProductComponents => Set<ProductComponent>();
    public DbSet<FinishedProductMaterial> FinishedProductMaterials => Set<FinishedProductMaterial>();
    public DbSet<ProductConsumptionProfile> ProductConsumptionProfiles => Set<ProductConsumptionProfile>();
    public DbSet<ConsumptionTemplate> ConsumptionTemplates => Set<ConsumptionTemplate>();
    public DbSet<ConsumptionTemplateDetail> ConsumptionTemplateDetails => Set<ConsumptionTemplateDetail>();
    public DbSet<ConsumptionTemplateSize> ConsumptionTemplateSizes => Set<ConsumptionTemplateSize>();
    public DbSet<FinishedProductSupply> FinishedProductSupplies => Set<FinishedProductSupply>();
    public DbSet<FinishedProductSupplySize> FinishedProductSupplySizes => Set<FinishedProductSupplySize>();
    public DbSet<MaterialSizeDistribution> MaterialSizeDistributions => Set<MaterialSizeDistribution>();
    public DbSet<MaterialSizeDistributionDetail> MaterialSizeDistributionDetails => Set<MaterialSizeDistributionDetail>();

    public DbSet<ProductTechnicalSheet> ProductTechnicalSheets => Set<ProductTechnicalSheet>();
    public DbSet<ProductTechnicalSheetMaterial> ProductTechnicalSheetMaterials => Set<ProductTechnicalSheetMaterial>();
    public DbSet<ProductTechnicalSheetProcess> ProductTechnicalSheetProcesses => Set<ProductTechnicalSheetProcess>();
    public DbSet<ProductCostSheet> ProductCostSheets => Set<ProductCostSheet>();
    public DbSet<ProductAuthorizationRecord> ProductAuthorizationRecords => Set<ProductAuthorizationRecord>();
    public DbSet<ProductSizeConsumptionVariation> ProductSizeConsumptionVariations => Set<ProductSizeConsumptionVariation>();

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeContract> EmployeeContracts => Set<EmployeeContract>();
    public DbSet<EmployeeIncident> EmployeeIncidents => Set<EmployeeIncident>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollConcept> PayrollConcepts => Set<PayrollConcept>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollRunLine> PayrollRunLines => Set<PayrollRunLine>();
    public DbSet<PayrollRunLineDetail> PayrollRunLineDetails => Set<PayrollRunLineDetail>();

    public DbSet<AttendancePunch> AttendancePunches => Set<AttendancePunch>();
    public DbSet<PayrollRecurringMovement> PayrollRecurringMovements => Set<PayrollRecurringMovement>();
    public DbSet<EmployeeLoan> EmployeeLoans => Set<EmployeeLoan>();
    public DbSet<EmployeeLoanDeduction> EmployeeLoanDeductions => Set<EmployeeLoanDeduction>();

    // HR enterprise
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<TimeClockDevice> TimeClockDevices => Set<TimeClockDevice>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<VacationRequest> VacationRequests => Set<VacationRequest>();
    public DbSet<EmployeeDocumentRecord> EmployeeDocumentRecords => Set<EmployeeDocumentRecord>();
    public DbSet<EmployeeLaborMovement> EmployeeLaborMovements => Set<EmployeeLaborMovement>();
    public DbSet<EmployeeCertificationRecord> EmployeeCertificationRecords => Set<EmployeeCertificationRecord>();
    public DbSet<RecruitmentVacancy> RecruitmentVacancies => Set<RecruitmentVacancy>();
    public DbSet<CandidateApplication> CandidateApplications => Set<CandidateApplication>();
    public DbSet<OnboardingChecklistRecord> OnboardingChecklistRecords => Set<OnboardingChecklistRecord>();
    public DbSet<EmployeePerformanceReview> EmployeePerformanceReviews => Set<EmployeePerformanceReview>();
    public DbSet<EmployeeCompetencyAssessment> EmployeeCompetencyAssessments => Set<EmployeeCompetencyAssessment>();
    public DbSet<SuccessionPlanRecord> SuccessionPlanRecords => Set<SuccessionPlanRecord>();

    // Payroll enterprise
    public DbSet<AttendanceDailySummary> AttendanceDailySummaries => Set<AttendanceDailySummary>();
    public DbSet<PrePayrollAdjustment> PrePayrollAdjustments => Set<PrePayrollAdjustment>();
    public DbSet<PrePayrollCutoff> PrePayrollCutoffs => Set<PrePayrollCutoff>();
    public DbSet<PayrollSourceApplication> PayrollSourceApplications => Set<PayrollSourceApplication>();
    public DbSet<PayrollReceiptControl> PayrollReceiptControls => Set<PayrollReceiptControl>();
    public DbSet<PayrollRunClosing> PayrollRunClosings => Set<PayrollRunClosing>();
    public DbSet<PayrollDispersionBatch> PayrollDispersionBatches => Set<PayrollDispersionBatch>();
    public DbSet<PayrollDispersionLine> PayrollDispersionLines => Set<PayrollDispersionLine>();
    public DbSet<PayrollAccountingPosting> PayrollAccountingPostings => Set<PayrollAccountingPosting>();
    public DbSet<PayrollTaxAccumulator> PayrollTaxAccumulators => Set<PayrollTaxAccumulator>();
    public DbSet<PayrollEmployerObligation> PayrollEmployerObligations => Set<PayrollEmployerObligation>();
    public DbSet<PayrollFiscalReconciliation> PayrollFiscalReconciliations => Set<PayrollFiscalReconciliation>();

    // Production module
    public DbSet<ProductionCell> ProductionCells => Set<ProductionCell>();
    public DbSet<ProductionCellEmployee> ProductionCellEmployees => Set<ProductionCellEmployee>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<ProductionOrderLine> ProductionOrderLines => Set<ProductionOrderLine>();
    public DbSet<ProductionSchedule> ProductionSchedules => Set<ProductionSchedule>();
    public DbSet<ProductionScheduleLine> ProductionScheduleLines => Set<ProductionScheduleLine>();
    public DbSet<ProductionPhaseProgress> ProductionPhaseProgress => Set<ProductionPhaseProgress>();
    public DbSet<ProductionVoucher> ProductionVouchers => Set<ProductionVoucher>();
    public DbSet<ProductionVoucherDetail> ProductionVoucherDetails => Set<ProductionVoucherDetail>();
    public DbSet<PieceWorkRate> PieceWorkRates => Set<PieceWorkRate>();
    public DbSet<PieceWorkRecord> PieceWorkRecords => Set<PieceWorkRecord>();
    public DbSet<MaterialRequirement> MaterialRequirements => Set<MaterialRequirement>();
    public DbSet<MaterialRequirementLine> MaterialRequirementLines => Set<MaterialRequirementLine>();
    public DbSet<ProductionInProcess> ProductionInProcess => Set<ProductionInProcess>();
    public DbSet<SurplusRecord> SurplusRecords => Set<SurplusRecord>();
    public DbSet<PhaseRestriction> PhaseRestrictions => Set<PhaseRestriction>();
    public DbSet<QualityControlRecord> QualityControlRecords => Set<QualityControlRecord>();
    public DbSet<QualityDefect> QualityDefects => Set<QualityDefect>();

    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();

    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<InventoryEntry> InventoryEntries => Set<InventoryEntry>();
    public DbSet<InventoryEntryLine> InventoryEntryLines => Set<InventoryEntryLine>();
    public DbSet<InventoryExit> InventoryExits => Set<InventoryExit>();
    public DbSet<InventoryExitLine> InventoryExitLines => Set<InventoryExitLine>();
    public DbSet<InventoryTransfer> InventoryTransfers => Set<InventoryTransfer>();
    public DbSet<InventoryTransferLine> InventoryTransferLines => Set<InventoryTransferLine>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<InventoryAdjustmentLine> InventoryAdjustmentLines => Set<InventoryAdjustmentLine>();
    public DbSet<PhysicalCount> PhysicalCounts => Set<PhysicalCount>();
    public DbSet<PhysicalCountLine> PhysicalCountLines => Set<PhysicalCountLine>();
    public DbSet<ItemLot> ItemLots => Set<ItemLot>();
    public DbSet<ItemSerial> ItemSerials => Set<ItemSerial>();

    public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();
    public DbSet<SalesQuoteLine> SalesQuoteLines => Set<SalesQuoteLine>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<SalesShipment> SalesShipments => Set<SalesShipment>();
    public DbSet<SalesShipmentLine> SalesShipmentLines => Set<SalesShipmentLine>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<CreditNoteLine> CreditNoteLines => Set<CreditNoteLine>();
    public DbSet<ServiceCatalogItem> ServiceCatalogItems => Set<ServiceCatalogItem>();
    public DbSet<CustomerServiceRate> CustomerServiceRates => Set<CustomerServiceRate>();
    public DbSet<ServiceNote> ServiceNotes => Set<ServiceNote>();

    public DbSet<CashAccount> CashAccounts => Set<CashAccount>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<BankMovement> BankMovements => Set<BankMovement>();
    public DbSet<TreasuryIncome> TreasuryIncomes => Set<TreasuryIncome>();
    public DbSet<TreasuryIncomeLine> TreasuryIncomeLines => Set<TreasuryIncomeLine>();
    public DbSet<TreasuryExpense> TreasuryExpenses => Set<TreasuryExpense>();
    public DbSet<TreasuryExpenseLine> TreasuryExpenseLines => Set<TreasuryExpenseLine>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentLine> PaymentLines => Set<PaymentLine>();
    public DbSet<Reconciliation> Reconciliations => Set<Reconciliation>();
    public DbSet<ReconciliationLine> ReconciliationLines => Set<ReconciliationLine>();

    public DbSet<AccountsReceivableAccount> AccountsReceivableAccounts => Set<AccountsReceivableAccount>();
    public DbSet<AccountsReceivableMovement> AccountsReceivableMovements => Set<AccountsReceivableMovement>();
    public DbSet<ReceiptApplication> ReceiptApplications => Set<ReceiptApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NanchesoftDbContext).Assembly);
        NanchesoftSchemaCatalog.Apply(modelBuilder);
        modelBuilder.UseNanchesoftSnakeCaseNames();
        base.OnModelCreating(modelBuilder);
    }
}
