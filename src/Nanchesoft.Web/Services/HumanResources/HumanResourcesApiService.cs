using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.HumanResources;

public sealed class HumanResourcesApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public HumanResourcesApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    // El IHttpClientFactory resuelve los DelegatingHandler en su propio scope DI,
    // distinto del circuito Blazor, así que el ApiTenantScopeHandler ve un AppState
    // vacío. Aquí ponemos los headers en DefaultRequestHeaders del HttpClient
    // (cada CreateClient devuelve una instancia nueva con sus headers propios)
    // usando el AppState/AuthState reales del circuito.
    private HttpClient CreateScopedClient()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        Set("X-Tenant-Id", _appState.CurrentTenantId ?? _authState.TenantId);
        Set("X-Company-Id", _appState.CurrentCompanyId ?? _authState.CompanyId);
        Set("X-Branch-Id", _appState.CurrentBranchId ?? _authState.BranchId);
        Set("X-User-Id", _authState.UserId);
        client.DefaultRequestHeaders.Remove("X-Is-Platform-Owner");
        client.DefaultRequestHeaders.Add("X-Is-Platform-Owner", _authState.IsPlatformOwner ? "true" : "false");
        return client;

        void Set(string header, Guid? value)
        {
            client.DefaultRequestHeaders.Remove(header);
            if (value.HasValue && value.Value != Guid.Empty)
                client.DefaultRequestHeaders.Add(header, value.Value.ToString("D"));
        }
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey.ToLowerInvariant() switch
        {
            "hr-departments" => GetDepartmentsAsync(),
            "hr-positions" => GetPositionsAsync(),
            "hr-employees" => GetEmployeesAsync(),
            "hr-incidents" => GetIncidentsAsync(),
            "hr-recurring-incidents" => GetRecurringIncidentsAsync(),
            "employee-contracts" => GetContractsAsync(),
            "payroll-incident-types" => GetPayrollIncidentTypesAsync(),
            "hr-banks" => GetHrBanksAsync(),
            "hr-termination-reasons" => GetTerminationReasonsAsync(),
            "hr-employer-registrations" => GetEmployerRegistrationsAsync(),
            "payroll-period-types" => GetPayrollPeriodTypesAsync(),
            "payroll-periods" => GetPayrollPeriodsAsync(),
            "payroll-concepts" => GetPayrollConceptsAsync(),
            "payroll-runs" => GetPayrollRunsAsync(),
            "payroll-run-lines" => GetPayrollRunLinesAsync(),
            "payroll-run-line-details" => GetPayrollRunLineDetailsAsync(),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = CreateScopedClient();

        var response = catalogKey.ToLowerInvariant() switch
        {
            "hr-departments" => await client.PostAsJsonAsync("/api/hr/departments", MapDepartmentRequest(payload)),
            "hr-positions" => await client.PostAsJsonAsync("/api/hr/positions", MapPositionRequest(payload)),
            "hr-employees" => await client.PostAsJsonAsync("/api/hr/employees", MapEmployeeRequest(payload)),
            "hr-incidents" => await client.PostAsJsonAsync("/api/hr/incidents", MapIncidentRequest(payload)),
            "hr-recurring-incidents" => await client.PostAsJsonAsync("/api/hr/recurring-incidents", MapRecurringIncidentRuleRequest(payload)),
            "employee-contracts" => await client.PostAsJsonAsync("/api/contracts/employee-contracts", MapContractRequest(payload)),
            "payroll-incident-types" => await client.PostAsJsonAsync("/api/payroll/incident-types", MapIncidentTypeRequest(payload)),
            "hr-banks" => await client.PostAsJsonAsync("/api/hr/banks", MapHrBankRequest(payload)),
            "hr-termination-reasons" => await client.PostAsJsonAsync("/api/hr/termination-reasons", MapTerminationReasonRequest(payload)),
            "hr-employer-registrations" => await client.PostAsJsonAsync("/api/hr/employer-registrations", MapEmployerRegistrationRequest(payload)),
            "payroll-period-types" => await client.PostAsJsonAsync("/api/payroll/period-types", MapPayrollPeriodTypeRequest(payload)),
            "payroll-periods" => await client.PostAsJsonAsync("/api/payroll/periods", MapPayrollPeriodRequest(payload)),
            "payroll-concepts" => await client.PostAsJsonAsync("/api/payroll/concepts", MapPayrollConceptRequest(payload)),
            "payroll-runs" => await client.PostAsJsonAsync("/api/payroll/runs", MapPayrollRunRequest(payload)),
            "payroll-run-lines" => await client.PostAsJsonAsync("/api/payroll/run-lines", MapPayrollRunLineRequest(payload)),
            "payroll-run-line-details" => await client.PostAsJsonAsync("/api/payroll/run-line-details", MapPayrollRunLineDetailRequest(payload)),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
    {
        var client = CreateScopedClient();

        var response = catalogKey.ToLowerInvariant() switch
        {
            "hr-departments" => await client.PutAsJsonAsync($"/api/hr/departments/{key}", MapDepartmentRequest(payload)),
            "hr-positions" => await client.PutAsJsonAsync($"/api/hr/positions/{key}", MapPositionRequest(payload)),
            "hr-employees" => await client.PutAsJsonAsync($"/api/hr/employees/{key}", MapEmployeeRequest(payload)),
            "hr-incidents" => await client.PutAsJsonAsync($"/api/hr/incidents/{key}", MapIncidentRequest(payload)),
            "hr-recurring-incidents" => await client.PutAsJsonAsync($"/api/hr/recurring-incidents/{key}", MapRecurringIncidentRuleRequest(payload)),
            "employee-contracts" => await client.PutAsJsonAsync($"/api/contracts/employee-contracts/{key}", MapContractRequest(payload)),
            "payroll-incident-types" => await client.PutAsJsonAsync($"/api/payroll/incident-types/{key}", MapIncidentTypeRequest(payload)),
            "hr-banks" => await client.PutAsJsonAsync($"/api/hr/banks/{key}", MapHrBankRequest(payload)),
            "hr-termination-reasons" => await client.PutAsJsonAsync($"/api/hr/termination-reasons/{key}", MapTerminationReasonRequest(payload)),
            "hr-employer-registrations" => await client.PutAsJsonAsync($"/api/hr/employer-registrations/{key}", MapEmployerRegistrationRequest(payload)),
            "payroll-period-types" => await client.PutAsJsonAsync($"/api/payroll/period-types/{key}", MapPayrollPeriodTypeRequest(payload)),
            "payroll-periods" => await client.PutAsJsonAsync($"/api/payroll/periods/{key}", MapPayrollPeriodRequest(payload)),
            "payroll-concepts" => await client.PutAsJsonAsync($"/api/payroll/concepts/{key}", MapPayrollConceptRequest(payload)),
            "payroll-runs" => await client.PutAsJsonAsync($"/api/payroll/runs/{key}", MapPayrollRunRequest(payload)),
            "payroll-run-lines" => await client.PutAsJsonAsync($"/api/payroll/run-lines/{key}", MapPayrollRunLineRequest(payload)),
            "payroll-run-line-details" => await client.PutAsJsonAsync($"/api/payroll/run-line-details/{key}", MapPayrollRunLineDetailRequest(payload)),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        var client = CreateScopedClient();

        var endpoint = catalogKey.ToLowerInvariant() switch
        {
            "hr-departments" => $"/api/hr/departments/{key}",
            "hr-positions" => $"/api/hr/positions/{key}",
            "hr-employees" => $"/api/hr/employees/{key}",
            "hr-incidents" => $"/api/hr/incidents/{key}",
            "hr-recurring-incidents" => $"/api/hr/recurring-incidents/{key}",
            "employee-contracts" => $"/api/contracts/employee-contracts/{key}",
            "payroll-incident-types" => $"/api/payroll/incident-types/{key}",
            "hr-banks" => $"/api/hr/banks/{key}",
            "hr-termination-reasons" => $"/api/hr/termination-reasons/{key}",
            "hr-employer-registrations" => $"/api/hr/employer-registrations/{key}",
            "payroll-period-types" => $"/api/payroll/period-types/{key}",
            "payroll-periods" => $"/api/payroll/periods/{key}",
            "payroll-concepts" => $"/api/payroll/concepts/{key}",
            "payroll-runs" => $"/api/payroll/runs/{key}",
            "payroll-run-lines" => $"/api/payroll/run-lines/{key}",
            "payroll-run-line-details" => $"/api/payroll/run-line-details/{key}",
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        var response = await client.DeleteAsync(endpoint);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    private async Task<CatalogViewDefinition> GetDepartmentsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<DepartmentDto>>("/api/hr/departments") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "hr-departments",
            "Departamentos",
            "Estructura base de recursos humanos por empresa.",
            "DepartmentId",
            [
                TextColumn("DepartmentId", "Department ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("Code", "Código", required: true, width: 120),
                TextColumn("Name", "Departamento", required: true, width: 220),
                TextColumn("Description", "Descripción", width: 280),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("DepartmentId", x.DepartmentId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Description", x.Description),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetPositionsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PositionDto>>("/api/hr/positions") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var departments = await GetDepartmentLookupsAsync();

        return BuildView(
            "hr-positions",
            "Puestos",
            "Puestos organizacionales con grupo de nómina y salario base de referencia.",
            "PositionId",
            [
                TextColumn("PositionId", "Position ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("DepartmentId", "Departamento", departments, width: 220, quickCreateKey: "hr-departments"),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Puesto", required: true, width: 220),
                TextColumn("Description", "Descripción", width: 280),
                TextColumn("PayrollGroup", "Grupo nómina", width: 140),
                NumberColumn("BaseSalary", "Salario base", width: 120),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PositionId", x.PositionId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("DepartmentId", x.DepartmentId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Description", x.Description),
                ("PayrollGroup", x.PayrollGroup),
                ("BaseSalary", x.BaseSalary),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetEmployeesAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<EmployeeDto>>("/api/hr/employees") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();
        var departments = await GetDepartmentLookupsAsync();
        var positions = await GetPositionLookupsAsync();
        var workSchedules = await GetWorkScheduleLookupsAsync();

        var view = BuildView(
            "hr-employees",
            "Colaboradores",
            "Expediente base del colaborador para RH, contratos y nómina.",
            "EmployeeId",
            [
                TextColumn("EmployeeId", "Employee ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("DepartmentId", "Departamento", departments, width: 220, quickCreateKey: "hr-departments"),
                LookupColumn("PositionId", "Puesto", positions, width: 220, quickCreateKey: "hr-positions"),
                LookupColumn("WorkScheduleId", "Horario", workSchedules, width: 180, quickCreateKey: "hr-work-schedules"),
                // Identificadores
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("EmployeeNumber", "Número empleado", required: true, width: 140),
                TextColumn("ClockKey", "Clave reloj", width: 120),
                TextColumn("NoiKey", "Clave NOI", width: 120),
                // Nombre
                TextColumn("FirstName", "Nombre", required: true, width: 160),
                TextColumn("LastName", "Apellido paterno", required: true, width: 180),
                TextColumn("SecondLastName", "Apellido materno", width: 180),
                TextColumn("MiddleName", "Segundo nombre", width: 160),
                // Contacto
                TextColumn("Email", "Email", width: 180),
                TextColumn("Phone", "Teléfono", width: 120),
                TextColumn("EmergencyPhone", "Tel. emergencia", width: 130),
                // Datos personales
                TextColumn("TaxId", "RFC", width: 140),
                TextColumn("NationalId", "CURP/ID", width: 140),
                DateColumn("HireDate", "Ingreso", required: true, width: 120),
                DateColumn("BirthDate", "Nacimiento", width: 120),
                LookupColumn("Gender", "Sexo", Genders(), width: 90),
                LookupColumn("BloodType", "Tipo sangre", BloodTypes(), width: 110),
                LookupColumn("MaritalStatus", "Estado civil", MaritalStatuses(), width: 130),
                TextColumn("PlaceOfBirth", "Lugar nacimiento", width: 160),
                TextColumn("Nationality", "Nacionalidad", width: 130),
                TextColumn("FatherName", "Nombre del padre", width: 160),
                TextColumn("MotherName", "Nombre de la madre", width: 160),
                // Domicilio
                TextColumn("AddressStreet", "Calle y número", width: 180),
                TextColumn("AddressColony", "Colonia", width: 150),
                TextColumn("AddressCity", "Ciudad", width: 130),
                TextColumn("AddressState", "Estado", width: 130),
                TextColumn("AddressZipCode", "C.P.", width: 80),
                // Salario
                NumberColumn("PeriodSalary", "Sueldo del periodo", width: 140),
                NumberColumn("DailySalary", "Salario diario", width: 120),
                NumberColumn("IntegratedDailySalary", "SDI", width: 120),
                NumberColumn("SbcFija", "SBC Fija", width: 110),
                TextColumn("Status", "Estatus", width: 110),
                // Baja
                DateColumn("TerminationDate", "Fecha baja", width: 120),
                TextColumn("TerminationReason", "Motivo baja", width: 150),
                DateColumn("ReentryDate", "Fecha reingreso", width: 130),
                // IMSS / SAT
                TextColumn("Curp", "CURP", width: 180),
                TextColumn("Nss", "NSS", width: 130),
                TextColumn("ImssRegId", "Reg. Patronal", width: 150),
                BoolColumn("IsImssRegistered", "IMSS registrado", width: 130),
                DateColumn("ImssRegistrationDate", "Fecha IMSS alta", width: 140),
                DateColumn("ImssTerminationDate", "Fecha IMSS baja", width: 140),
                TextColumn("Umf", "UMF", width: 80),
                LookupColumn("ContractType", "Tipo contrato", ContractTypes(), width: 170),
                LookupColumn("CotizationBase", "Base cotización", CotizationBases(), width: 150),
                LookupColumn("TaxRegime", "Régimen fiscal", TaxRegimes(), width: 190),
                LookupColumn("EmployeeType", "Tipo empleado", EmployeeTypes(), width: 140),
                LookupColumn("SalaryZone", "Zona salarial", SalaryZones(), width: 110),
                LookupColumn("PayrollPeriodType", "Periodo nómina", PeriodTypes(), width: 180),
                // Fondos
                TextColumn("Afore", "AFORE", width: 120),
                TextColumn("Fonacot", "FONACOT", width: 120),
                TextColumn("Infonavit", "INFONAVIT", width: 130),
                // Banco
                LookupColumn("PaymentForm", "Forma pago", PaymentForms(), width: 150),
                TextColumn("BankCode", "Banco", width: 100),
                TextColumn("BankAccount", "Cuenta", width: 150),
                TextColumn("Clabe", "CLABE", width: 190),
                TextColumn("BankBranch", "Sucursal banco", width: 130),
                // Otros
                TextColumn("ImmediateSupervisor", "Jefe directo", width: 160),
                TextColumn("Category", "Categoría", width: 130),
                TextColumn("Notes", "Notas", width: 180),
                BoolColumn("PrintReceipt", "Imprime recibo", width: 120),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("EmployeeId", x.EmployeeId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("DepartmentId", x.DepartmentId?.ToString("D")),
                ("PositionId", x.PositionId?.ToString("D")),
                ("WorkScheduleId", x.WorkScheduleId?.ToString("D")),
                ("Code", x.Code),
                ("EmployeeNumber", x.EmployeeNumber),
                ("ClockKey", x.ClockKey),
                ("NoiKey", x.NoiKey),
                ("FirstName", x.FirstName),
                ("LastName", x.LastName),
                ("SecondLastName", x.SecondLastName),
                ("MiddleName", x.MiddleName),
                ("Email", x.Email),
                ("Phone", x.Phone),
                ("EmergencyPhone", x.EmergencyPhone),
                ("TaxId", x.TaxId),
                ("NationalId", x.NationalId),
                ("HireDate", x.HireDate),
                ("BirthDate", x.BirthDate),
                ("Gender", x.Gender),
                ("BloodType", x.BloodType),
                ("MaritalStatus", x.MaritalStatus),
                ("PlaceOfBirth", x.PlaceOfBirth),
                ("Nationality", x.Nationality),
                ("FatherName", x.FatherName),
                ("MotherName", x.MotherName),
                ("AddressStreet", x.AddressStreet),
                ("AddressColony", x.AddressColony),
                ("AddressCity", x.AddressCity),
                ("AddressState", x.AddressState),
                ("AddressZipCode", x.AddressZipCode),
                ("PeriodSalary", x.PeriodSalary),
                ("DailySalary", x.DailySalary),
                ("IntegratedDailySalary", x.IntegratedDailySalary),
                ("SbcFija", x.SbcFija),
                ("Status", x.Status),
                ("TerminationDate", x.TerminationDate),
                ("TerminationReason", x.TerminationReason),
                ("ReentryDate", x.ReentryDate),
                ("Curp", x.Curp),
                ("Nss", x.Nss),
                ("ImssRegId", x.ImssRegId),
                ("IsImssRegistered", x.IsImssRegistered),
                ("ImssRegistrationDate", x.ImssRegistrationDate),
                ("ImssTerminationDate", x.ImssTerminationDate),
                ("Umf", x.Umf),
                ("ContractType", x.ContractType),
                ("CotizationBase", x.CotizationBase),
                ("TaxRegime", x.TaxRegime),
                ("EmployeeType", x.EmployeeType),
                ("SalaryZone", x.SalaryZone),
                ("PayrollPeriodType", x.PayrollPeriodType),
                ("Afore", x.Afore),
                ("Fonacot", x.Fonacot),
                ("Infonavit", x.Infonavit),
                ("PaymentForm", x.PaymentForm),
                ("BankCode", x.BankCode),
                ("BankAccount", x.BankAccount),
                ("Clabe", x.Clabe),
                ("BankBranch", x.BankBranch),
                ("ImmediateSupervisor", x.ImmediateSupervisor),
                ("Category", x.Category),
                ("Notes", x.Notes),
                ("PrintReceipt", x.PrintReceipt),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
        view.NewUrl = "/human-resources/empleado";
        view.EditUrl = "/human-resources/empleado";
        return view;
    }

    private async Task<CatalogViewDefinition> GetIncidentsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<EmployeeIncidentDto>>("/api/hr/incidents") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync(onlyCurrentBranch: true);
        var employees = await GetEmployeeLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync(onlyOpen: true);
        var incidentTypes = await GetPayrollIncidentTypeLookupsAsync();

        var view = BuildView(
            "hr-incidents",
            "Incidencias",
            "Incidencias operativas que alimentan el cálculo de nómina.",
            "EmployeeIncidentId",
            [
                TextColumn("EmployeeIncidentId", "Incident ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("PayrollPeriodId", "Periodo nómina", periods, width: 220),
                DateColumn("IncidentDate", "Fecha", required: true, width: 120),
                LookupColumn("PayrollIncidentTypeId", "Tipo", incidentTypes, required: true, width: 300, quickCreateKey: "payroll-incident-types"),
                ReadOnlyLookupColumn("IncidentCategory", "Categoría", IncidentCategories(), width: 130),
                TextColumn("AffectType", "Afecta", allowEditing: false, width: 110),
                TextColumn("PayrollConceptType", "Concepto", allowEditing: false, width: 140),
                NumberColumn("Quantity", "Cantidad", width: 110),
                NumberColumn("Amount", "Importe", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                LookupColumn("Status", "Estatus", IncidentStatuses(), width: 130),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("EmployeeIncidentId", x.EmployeeIncidentId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("IncidentDate", x.IncidentDate),
                ("PayrollIncidentTypeId", (x.PayrollIncidentTypeId ?? x.NomPayrollIncidentTypeId)?.ToString("D")),
                ("NomPayrollIncidentTypeId", (x.PayrollIncidentTypeId ?? x.NomPayrollIncidentTypeId)?.ToString("D")),
                ("IncidentCategory", x.IncidentCategory),
                ("AffectType", x.AffectType),
                ("PayrollConceptType", x.PayrollConceptType),
                ("Quantity", x.Quantity),
                ("Amount", x.Amount),
                ("Notes", x.Notes),
                ("Status", x.Status),
                ("IsActive", x.IsActive)))
            .ToList());

        view.Metadata["DiscountCount"] = rows.Count(x => IsIncidentCategory(x.IncidentCategory, "DEDUCCION"));
        view.Metadata["PerceptionCount"] = rows.Count(x => IsIncidentCategory(x.IncidentCategory, "PERCEPCION"));
        view.Metadata["InformativeCount"] = rows.Count(x => IsIncidentCategory(x.IncidentCategory, "INFORMATIVA"));
        return view;
    }

    private async Task<CatalogViewDefinition> GetRecurringIncidentsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<HrRecurringIncidentRuleDto>>("/api/hr/recurring-incidents") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync(onlyCurrentBranch: true);
        var employees = await GetEmployeeLookupsAsync();
        var incidentTypes = await GetPayrollIncidentTypeLookupsAsync();

        return BuildView(
            "hr-recurring-incidents",
            "Incidencias recurrentes",
            "Reglas de incidencias que se generan para periodos abiertos sin duplicar.",
            "HrRecurringIncidentRuleId",
            [
                TextColumn("HrRecurringIncidentRuleId", "Rule ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("NomPayrollIncidentTypeId", "Tipo", incidentTypes, required: true, width: 300, quickCreateKey: "payroll-incident-types"),
                NumberColumn("Amount", "Importe", width: 120),
                NumberColumn("Quantity", "Cantidad", width: 120),
                DateColumn("StartDate", "Inicio", required: true, width: 120),
                DateColumn("EndDate", "Fin", width: 120),
                LookupColumn("Frequency", "Frecuencia", RecurringFrequencies(), required: true, width: 150),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("RequiresAuthorization", "Autoriza", width: 95),
                TextColumn("AuthorizedBy", "Autorizó", width: 150),
                DateColumn("AuthorizedAt", "Fecha autorización", width: 150),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("HrRecurringIncidentRuleId", x.HrRecurringIncidentRuleId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("NomPayrollIncidentTypeId", x.NomPayrollIncidentTypeId?.ToString("D")),
                ("Amount", x.Amount),
                ("Quantity", x.Quantity),
                ("StartDate", x.StartDate),
                ("EndDate", x.EndDate),
                ("Frequency", x.Frequency),
                ("Notes", x.Notes),
                ("RequiresAuthorization", x.RequiresAuthorization),
                ("AuthorizedBy", x.AuthorizedBy),
                ("AuthorizedAt", x.AuthorizedAt),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollIncidentTypesAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<NomPayrollIncidentTypeDto>>("/api/payroll/incident-types?includeInactive=true") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();

        return BuildView(
            "payroll-incident-types",
            "Tipos de incidencia de nómina",
            "Catálogo formal para clasificar incidencias de RH y su efecto en nómina.",
            "NomPayrollIncidentTypeId",
            [
                TextColumn("NomPayrollIncidentTypeId", "ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
                TextColumn("Code", "Código", required: true, width: 150),
                TextColumn("Name", "Nombre", required: true, width: 240),
                TextColumn("Description", "Descripción", width: 280),
                LookupColumn("IncidentCategory", "Categoría", IncidentCategories(), required: true, width: 150),
                LookupColumn("AffectType", "Afectación", AffectTypes(), required: true, width: 130),
                LookupColumn("PayrollConceptType", "Tipo nómina", PayrollConceptTypes(), required: true, width: 170),
                TextColumn("SatCode", "SAT", width: 100),
                TextColumn("Color", "Color", width: 110),
                TextColumn("Icon", "Icono", width: 140),
                NumberColumn("SortOrder", "Orden", width: 90),
                BoolColumn("IsDiscount", "Descuento", width: 105),
                BoolColumn("IsPerception", "Percepción", width: 105),
                BoolColumn("IsInformative", "Informativa", width: 105),
                BoolColumn("RequiresAmount", "Importe", width: 90),
                BoolColumn("RequiresQuantity", "Cantidad", width: 95),
                BoolColumn("RequiresAuthorization", "Autoriza", width: 95),
                BoolColumn("AppliesToPayroll", "Nómina", width: 90),
                BoolColumn("IsSystem", "Sistema", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("NomPayrollIncidentTypeId", x.NomPayrollIncidentTypeId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Description", x.Description),
                ("IncidentCategory", x.IncidentCategory),
                ("AffectType", x.AffectType),
                ("PayrollConceptType", x.PayrollConceptType),
                ("SatCode", x.SatCode),
                ("Color", x.Color),
                ("Icon", x.Icon),
                ("SortOrder", x.SortOrder),
                ("IsDiscount", x.IsDiscount),
                ("IsPerception", x.IsPerception),
                ("IsInformative", x.IsInformative),
                ("RequiresAmount", x.RequiresAmount),
                ("RequiresQuantity", x.RequiresQuantity),
                ("RequiresAuthorization", x.RequiresAuthorization),
                ("AppliesToPayroll", x.AppliesToPayroll),
                ("IsSystem", x.IsSystem),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetHrBanksAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<HrBankDto>>("/api/hr/banks") ?? [];

        return BuildView(
            "hr-banks",
            "Bancos",
            "Catálogo de bancos para datos bancarios de colaboradores.",
            "BankId",
            [
                TextColumn("BankId", "ID", allowEditing: false, width: 220),
                TextColumn("Code", "Clave", required: true, width: 100),
                TextColumn("ShortName", "Nombre corto", required: true, width: 140),
                TextColumn("Name", "Nombre completo", required: true, width: 260),
                TextColumn("SatCode", "Clave SAT", width: 100),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("BankId", x.BankId.ToString("D")),
                ("Code", x.Code),
                ("ShortName", x.ShortName),
                ("Name", x.Name),
                ("SatCode", x.SatCode),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetTerminationReasonsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<HrTerminationReasonDto>>("/api/hr/termination-reasons") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "hr-termination-reasons",
            "Motivos de baja",
            "Causas de terminación de relación laboral por empresa.",
            "TerminationReasonId",
            [
                TextColumn("TerminationReasonId", "ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("Code", "Código", required: true, width: 100),
                TextColumn("Name", "Motivo", required: true, width: 220),
                TextColumn("Description", "Descripción", width: 280),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("TerminationReasonId", x.TerminationReasonId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Description", x.Description),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetEmployerRegistrationsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<HrEmployerRegistrationDto>>("/api/hr/employer-registrations") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "hr-employer-registrations",
            "Registros Patronales",
            "Registros ante el IMSS para cálculo de cuotas y emisión de comprobantes.",
            "EmployerRegistrationId",
            [
                TextColumn("EmployerRegistrationId", "ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("Code", "Código", required: true, width: 100),
                TextColumn("Name", "Nombre", required: true, width: 200),
                TextColumn("RegistrationNumber", "No. Registro patronal", required: true, width: 180),
                TextColumn("RiskClass", "Clase de riesgo", width: 120),
                TextColumn("State", "Estado", width: 130),
                TextColumn("Notes", "Notas", width: 200),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("EmployerRegistrationId", x.EmployerRegistrationId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("RegistrationNumber", x.RegistrationNumber),
                ("RiskClass", x.RiskClass),
                ("State", x.State),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetContractsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<EmployeeContractDto>>("/api/contracts/employee-contracts") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();

        return BuildView(
            "employee-contracts",
            "Contratos laborales",
            "Contratos vigentes, históricos y base salarial contractual.",
            "EmployeeContractId",
            [
                TextColumn("EmployeeContractId", "Contract ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                TextColumn("ContractNumber", "Contrato", required: true, width: 140),
                TextColumn("ContractType", "Tipo", required: true, width: 120),
                DateColumn("StartDate", "Inicio", required: true, width: 120),
                DateColumn("EndDate", "Término", width: 120),
                TextColumn("PaymentFrequency", "Frecuencia", required: true, width: 120),
                NumberColumn("BaseSalary", "Salario base", width: 120),
                NumberColumn("IntegratedSalary", "Salario integrado", width: 130),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("EmployeeContractId", x.EmployeeContractId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("ContractNumber", x.ContractNumber),
                ("ContractType", x.ContractType),
                ("StartDate", x.StartDate),
                ("EndDate", x.EndDate),
                ("PaymentFrequency", x.PaymentFrequency),
                ("BaseSalary", x.BaseSalary),
                ("IntegratedSalary", x.IntegratedSalary),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private static List<CatalogLookupItem> ConceptTypes() =>
    [
        new() { Id = "percepcion",  Name = "Percepción" },
        new() { Id = "deduccion",   Name = "Deducción" },
        new() { Id = "obligacion",  Name = "Obligación" },
    ];

    private static List<CatalogLookupItem> IncidentCategories() =>
    [
        new() { Id = "DEDUCCION", Name = "Deducción", Color = "#DC2626", Icon = "minus-circle", Group = "Nómina" },
        new() { Id = "PERCEPCION", Name = "Percepción", Color = "#16A34A", Icon = "plus-circle", Group = "Nómina" },
        new() { Id = "INFORMATIVA", Name = "Informativa", Color = "#2563EB", Icon = "info", Group = "Control" },
    ];

    private static List<CatalogLookupItem> IncidentStatuses() =>
    [
        new() { Id = "draft", Name = "Borrador", Color = "#64748B", Icon = "edit-3" },
        new() { Id = "active", Name = "Activa", Color = "#16A34A", Icon = "check-circle" },
        new() { Id = "applied", Name = "Aplicada", Color = "#2563EB", Icon = "badge-check" },
        new() { Id = "cancelled", Name = "Cancelada", Color = "#DC2626", Icon = "x-circle" },
    ];

    private static List<CatalogLookupItem> AffectTypes() =>
    [
        new() { Id = "SUMA", Name = "Suma", Color = "#16A34A", Icon = "plus" },
        new() { Id = "RESTA", Name = "Resta", Color = "#DC2626", Icon = "minus" },
        new() { Id = "NO_AFECTA", Name = "No afecta", Color = "#2563EB", Icon = "equal" },
    ];

    private static List<CatalogLookupItem> PayrollConceptTypes() =>
    [
        new() { Id = "FALTA", Name = "Falta", Group = "Asistencia" },
        new() { Id = "RETARDO", Name = "Retardo", Group = "Asistencia" },
        new() { Id = "HORAS_EXTRA", Name = "Horas extra", Group = "Percepciones" },
        new() { Id = "BONO", Name = "Bono", Group = "Percepciones" },
        new() { Id = "COMISION", Name = "Comisión", Group = "Percepciones" },
        new() { Id = "VACACIONES", Name = "Vacaciones", Group = "Ausencias" },
        new() { Id = "INCAPACIDAD", Name = "Incapacidad", Group = "Ausencias" },
        new() { Id = "PRESTAMO", Name = "Préstamo", Group = "Deducciones" },
        new() { Id = "DESCUENTO_DANOS", Name = "Descuento daños", Group = "Deducciones" },
        new() { Id = "OTRO", Name = "Otro", Group = "General" },
    ];

    private static List<CatalogLookupItem> RecurringFrequencies() =>
    [
        new() { Id = "semanal", Name = "Semanal", Group = "Calendario" },
        new() { Id = "quincenal", Name = "Quincenal", Group = "Calendario" },
        new() { Id = "mensual", Name = "Mensual", Group = "Calendario" },
        new() { Id = "cada_periodo", Name = "Cada periodo", Group = "Nómina" },
    ];

    private static List<CatalogLookupItem> PeriodTypes() =>
    [
        new() { Id = "semanal",    Name = "Semanal (7 días)" },
        new() { Id = "decenal",    Name = "Decenal (10 días)" },
        new() { Id = "quincenal",  Name = "Quincenal (15 días)" },
        new() { Id = "mensual",    Name = "Mensual (30/31 días)" },
    ];

    private static List<CatalogLookupItem> ContractTypes() =>
    [
        new() { Id = "indefinite",        Name = "Indeterminado" },
        new() { Id = "determined",        Name = "Determinado" },
        new() { Id = "construction_temp", Name = "Obra determinada" },
        new() { Id = "field_temp",        Name = "Temporada" },
        new() { Id = "trial",             Name = "A prueba" },
        new() { Id = "training",          Name = "Capacitación" },
    ];

    private static List<CatalogLookupItem> CotizationBases() =>
    [
        new() { Id = "fixed",    Name = "Fija" },
        new() { Id = "variable", Name = "Variable" },
        new() { Id = "mixed",    Name = "Mixta" },
    ];

    private static List<CatalogLookupItem> TaxRegimes() =>
    [
        new() { Id = "sueldos_salarios", Name = "Sueldos y Salarios" },
        new() { Id = "asimilados",       Name = "Asimilados a Salarios" },
    ];

    private static List<CatalogLookupItem> EmployeeTypes() =>
    [
        new() { Id = "base",      Name = "Base" },
        new() { Id = "confianza", Name = "Confianza" },
        new() { Id = "eventual",  Name = "Eventual" },
    ];

    private static List<CatalogLookupItem> SalaryZones() =>
    [
        new() { Id = "A", Name = "Zona A" },
        new() { Id = "B", Name = "Zona B" },
        new() { Id = "C", Name = "Zona C" },
    ];

    private static List<CatalogLookupItem> PaymentForms() =>
    [
        new() { Id = "tarjeta",       Name = "Tarjeta bancaria" },
        new() { Id = "transferencia", Name = "Transferencia" },
        new() { Id = "efectivo",      Name = "Efectivo" },
        new() { Id = "cheque",        Name = "Cheque" },
    ];

    private static List<CatalogLookupItem> Genders() =>
    [
        new() { Id = "M", Name = "Masculino" },
        new() { Id = "F", Name = "Femenino" },
    ];

    private static List<CatalogLookupItem> BloodTypes() =>
    [
        new() { Id = "A+",  Name = "A+" },
        new() { Id = "A-",  Name = "A-" },
        new() { Id = "B+",  Name = "B+" },
        new() { Id = "B-",  Name = "B-" },
        new() { Id = "AB+", Name = "AB+" },
        new() { Id = "AB-", Name = "AB-" },
        new() { Id = "O+",  Name = "O+" },
        new() { Id = "O-",  Name = "O-" },
    ];

    private static List<CatalogLookupItem> MaritalStatuses() =>
    [
        new() { Id = "soltero",   Name = "Soltero(a)" },
        new() { Id = "casado",    Name = "Casado(a)" },
        new() { Id = "divorciado",Name = "Divorciado(a)" },
        new() { Id = "viudo",     Name = "Viudo(a)" },
        new() { Id = "union",     Name = "Unión libre" },
    ];

    private static List<CatalogLookupItem> PeriodStatuses() =>
    [
        new() { Id = "draft",     Name = "Borrador" },
        new() { Id = "open",      Name = "Abierto" },
        new() { Id = "calculated",Name = "Calculado" },
        new() { Id = "closed",    Name = "Cerrado" },
    ];

    private static List<CatalogLookupItem> PayrollRunStatuses() =>
    [
        new() { Id = "draft",      Name = "Borrador", Color = "#64748B", Icon = "edit-3" },
        new() { Id = "open",       Name = "Abierta", Color = "#2563EB", Icon = "unlock" },
        new() { Id = "calculated", Name = "Calculada", Color = "#0891B2", Icon = "calculator" },
        new() { Id = "authorized", Name = "Autorizada", Color = "#16A34A", Icon = "badge-check" },
        new() { Id = "approved",   Name = "Aprobada", Color = "#16A34A", Icon = "check-circle" },
        new() { Id = "closed",     Name = "Cerrada", Color = "#334155", Icon = "lock" },
        new() { Id = "cancelled",  Name = "Cancelada", Color = "#DC2626", Icon = "x-circle" },
    ];

    private async Task<CatalogViewDefinition> GetPayrollPeriodTypesAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollPeriodTypeDto>>("/api/payroll/period-types") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "payroll-period-types",
            "Tipos de nómina",
            "Frecuencias de pago configurables: semanal, quincenal, mensual, etc.",
            "PayrollPeriodTypeId",
            [
                TextColumn("PayrollPeriodTypeId", "ID", allowEditing: false, width: 200),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("Code", "Código", required: true, width: 120),
                TextColumn("Name", "Nombre", required: true, width: 220),
                NumberColumn("DaysPerPeriod", "Días/Periodo", width: 120),
                NumberColumn("PeriodsPerYear", "Periodos/año", width: 120),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollPeriodTypeId", x.PayrollPeriodTypeId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("DaysPerPeriod", x.DaysPerPeriod),
                ("PeriodsPerYear", x.PeriodsPerYear),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetPayrollPeriodsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollPeriodDto>>("/api/payroll/periods") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var periodTypes = PeriodTypes();
        var periodStatuses = PeriodStatuses();

        return BuildView(
            "payroll-periods",
            "Periodos de nómina",
            "Ventanas operativas para cálculo, incidencias y pago — semanal, quincenal o mensual.",
            "PayrollPeriodId",
            [
                TextColumn("PayrollPeriodId", "ID", allowEditing: false, width: 200),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("Code", "Código", required: true, width: 130),
                TextColumn("Name", "Nombre del periodo", required: true, width: 240),
                LookupColumn("PeriodType", "Tipo de periodo", periodTypes, required: true, width: 180),
                DateColumn("StartDate", "Fecha inicio", required: true, width: 130),
                DateColumn("EndDate", "Fecha fin", required: true, width: 130),
                DateColumn("PaymentDate", "Fecha pago", required: true, width: 130),
                BoolColumn("IsImssInsured", "Asegurados IMSS", width: 130),
                LookupColumn("Status", "Estatus", periodStatuses, width: 130),
                BoolColumn("IsClosed", "Cerrado", width: 90),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollPeriodId", x.PayrollPeriodId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("PeriodType", x.PeriodType),
                ("StartDate", x.StartDate),
                ("EndDate", x.EndDate),
                ("PaymentDate", x.PaymentDate),
                ("IsImssInsured", x.IsImssInsured),
                ("Status", x.Status),
                ("IsClosed", x.IsClosed),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollConceptsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollConceptDto>>("/api/payroll/concepts") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "payroll-concepts",
            "Conceptos de nómina",
            "Percepciones y deducciones parametrizadas para cálculo futuro.",
            "PayrollConceptId",
            [
                TextColumn("PayrollConceptId", "PayrollConcept ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("Code", "Código", required: true, width: 110),
                TextColumn("Name", "Concepto", required: true, width: 220),
                LookupColumn("ConceptType", "Tipo", ConceptTypes(), required: true, width: 140),
                TextColumn("CalculationType", "Cálculo", required: true, width: 120),
                TextColumn("SatCode", "Código SAT", width: 100),
                TextColumn("SatAgrupador", "Agrupador SAT", width: 130),
                TextColumn("TaxableType", "Tipo fiscal", width: 120),
                NumberColumn("TaxablePercent", "% Gravado", width: 90),
                NumberColumn("ExemptPercent", "% Exento", width: 90),
                BoolColumn("IsRecurring", "Recurrente", width: 100),
                BoolColumn("IsAutomatic", "Auto", width: 80),
                BoolColumn("PrintOnReceipt", "Imprime", width: 90),
                NumberColumn("SortOrder", "Orden", width: 80),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollConceptId", x.PayrollConceptId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("ConceptType", x.ConceptType),
                ("CalculationType", x.CalculationType),
                ("SatCode", x.SatCode),
                ("SatAgrupador", x.SatAgrupador),
                ("TaxableType", x.TaxableType),
                ("TaxablePercent", x.TaxablePercent),
                ("ExemptPercent", x.ExemptPercent),
                ("IsRecurring", x.IsRecurring),
                ("IsAutomatic", x.IsAutomatic),
                ("PrintOnReceipt", x.PrintOnReceipt),
                ("SortOrder", x.SortOrder),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetPayrollRunsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollRunDto>>("/api/payroll/runs") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();
        var statuses = PayrollRunStatuses();

        return BuildView(
            "payroll-runs",
            "Procesamientos de nómina",
            "Corridas operativas por periodo con totales y estatus del proceso.",
            "PayrollRunId",
            [
                TextColumn("PayrollRunId", "PayrollRun ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("PayrollPeriodId", "Periodo nómina", periods, required: true, width: 220, quickCreateKey: "payroll-periods"),
                TextColumn("Folio", "Folio", required: true, width: 120),
                DateColumn("RunDate", "Fecha", required: true, width: 120),
                LookupColumn("Status", "Estatus", statuses, required: true, width: 140),
                NumberColumn("EmployeeCount", "Empleados", width: 100),
                NumberColumn("GrossAmount", "Bruto", width: 110),
                NumberColumn("DeductionsAmount", "Deducciones", width: 120),
                NumberColumn("NetAmount", "Neto", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollRunId", x.PayrollRunId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("Folio", x.Folio),
                ("RunDate", x.RunDate),
                ("Status", x.Status),
                ("EmployeeCount", x.EmployeeCount),
                ("GrossAmount", x.GrossAmount),
                ("DeductionsAmount", x.DeductionsAmount),
                ("NetAmount", x.NetAmount),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollRunLinesAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollRunLineDto>>("/api/payroll/run-lines") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var departments = await GetDepartmentLookupsAsync();
        var positions = await GetPositionLookupsAsync();

        return BuildView(
            "payroll-run-lines",
            "Detalle de nómina",
            "Recibos calculados por colaborador dentro de cada corrida.",
            "PayrollRunLineId",
            [
                TextColumn("PayrollRunLineId", "PayrollRunLine ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("DepartmentId", "Departamento", departments, width: 220, quickCreateKey: "hr-departments"),
                LookupColumn("PositionId", "Puesto", positions, width: 220, quickCreateKey: "hr-positions"),
                NumberColumn("DaysPaid", "Días pagados", width: 100),
                NumberColumn("GrossAmount", "Bruto", width: 110),
                NumberColumn("DeductionsAmount", "Deducciones", width: 120),
                NumberColumn("NetAmount", "Neto", width: 110),
                NumberColumn("IncidentsAmount", "Incidencias", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollRunLineId", x.PayrollRunLineId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("DepartmentId", x.DepartmentId?.ToString("D")),
                ("PositionId", x.PositionId?.ToString("D")),
                ("DaysPaid", x.DaysPaid),
                ("GrossAmount", x.GrossAmount),
                ("DeductionsAmount", x.DeductionsAmount),
                ("NetAmount", x.NetAmount),
                ("IncidentsAmount", x.IncidentsAmount),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollRunLineDetailsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollRunLineDetailDto>>("/api/payroll/run-line-details") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();
        var runLines = await GetPayrollRunLineLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var concepts = await GetPayrollConceptLookupsAsync();

        return BuildView(
            "payroll-run-line-details",
            "Percepciones y deducciones",
            "Detalle por colaborador listo para CFDI nómina 1.2: percepciones, deducciones y parte gravada/exenta.",
            "PayrollRunLineDetailId",
            [
                TextColumn("PayrollRunLineDetailId", "Detail ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                LookupColumn("PayrollRunLineId", "Recibo/colaborador", runLines, required: true, width: 240),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 220),
                LookupColumn("PayrollConceptId", "Concepto nómina", concepts, required: true, width: 220, quickCreateKey: "payroll-concepts"),
                TextColumn("ConceptCode", "Código", required: true, width: 100),
                TextColumn("ConceptName", "Concepto", required: true, width: 220),
                TextColumn("ConceptType", "Tipo", required: true, width: 110),
                TextColumn("SatCode", "SAT", width: 90),
                TextColumn("TaxableType", "Fiscal", width: 110),
                NumberColumn("Quantity", "Cantidad", width: 100),
                NumberColumn("Amount", "Importe", width: 110),
                NumberColumn("TaxableAmount", "Gravado", width: 110),
                NumberColumn("ExemptAmount", "Exento", width: 110),
                NumberColumn("SortOrder", "Orden", width: 90),
                BoolColumn("IsGenerated", "Generado", width: 95),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollRunLineDetailId", x.PayrollRunLineDetailId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("PayrollRunLineId", x.PayrollRunLineId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("PayrollConceptId", x.PayrollConceptId?.ToString("D")),
                ("ConceptCode", x.ConceptCode),
                ("ConceptName", x.ConceptName),
                ("ConceptType", x.ConceptType),
                ("SatCode", x.SatCode),
                ("TaxableType", x.TaxableType),
                ("Quantity", x.Quantity),
                ("Amount", x.Amount),
                ("TaxableAmount", x.TaxableAmount),
                ("ExemptAmount", x.ExemptAmount),
                ("SortOrder", x.SortOrder),
                ("IsGenerated", x.IsGenerated),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<List<CatalogLookupItem>> GetCompanyLookupsAsync()
    {
        var tenantId = _appState.CurrentTenantId ?? _authState.TenantId;
        var companyId = _appState.CurrentCompanyId ?? _authState.CompanyId;

        // No session context → hide the company field rather than leaking cross-tenant data
        if (!tenantId.HasValue && !companyId.HasValue)
            return [];

        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<CompanyLookupDto>>("/api/organization/companies") ?? [];

        if (tenantId.HasValue)
            rows = rows.Where(x => x.TenantId == tenantId.Value).ToList();
        else if (companyId.HasValue)
            rows = rows.Where(x => x.CompanyId == companyId.Value).ToList();

        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem
        {
            Id = x.CompanyId.ToString("D"),
            Name = x.Name
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetBranchLookupsAsync(bool onlyCurrentBranch = false)
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<BranchLookupDto>>("/api/organization/branches") ?? [];
        var tenantId = _appState.CurrentTenantId ?? _authState.TenantId;
        var companyId = _appState.CurrentCompanyId ?? _authState.CompanyId;
        var branchId = _appState.CurrentBranchId ?? _authState.BranchId;
        if (tenantId.HasValue)
            rows = rows.Where(x => x.TenantId == tenantId.Value).ToList();
        if (companyId.HasValue)
            rows = rows.Where(x => x.CompanyId == companyId.Value).ToList();
        if (onlyCurrentBranch && branchId.HasValue)
            rows = rows.Where(x => x.BranchId == branchId.Value).ToList();
        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem
        {
            Id = x.BranchId.ToString("D"),
            Name = $"{x.Name} · {x.CompanyName}",
            Group = x.CompanyName,
            IsActive = x.IsActive
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetDepartmentLookupsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<DepartmentDto>>("/api/hr/departments") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem
        {
            Id = x.DepartmentId.ToString("D"),
            Name = $"{x.Name} · {x.CompanyName}"
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPositionLookupsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PositionDto>>("/api/hr/positions") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem
        {
            Id = x.PositionId.ToString("D"),
            Name = $"{x.Name} · {x.CompanyName}"
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetWorkScheduleLookupsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<WorkScheduleLookupDto>>("/api/hr/work-schedules") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem
        {
            Id = x.WorkScheduleId.ToString("D"),
            Name = x.Name
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetEmployeeLookupsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<EmployeeDto>>("/api/hr/employees") ?? [];
        var branchId = _appState.CurrentBranchId ?? _authState.BranchId;
        if (branchId.HasValue)
            rows = rows.Where(x => !x.BranchId.HasValue || x.BranchId == branchId.Value).ToList();
        return rows.Where(x => x.IsActive).OrderBy(x => x.FullName).Select(x => new CatalogLookupItem
        {
            Id = x.EmployeeId.ToString("D"),
            Name = $"{x.EmployeeNumber} · {x.FullName}"
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPayrollPeriodLookupsAsync(bool onlyOpen = false)
    {
        var client = CreateScopedClient();
        var endpoint = onlyOpen ? "/api/payroll/periods?onlyOpen=true" : "/api/payroll/periods";
        var rows = await client.GetFromJsonAsync<List<PayrollPeriodDto>>(endpoint) ?? [];
        var tenantId = _appState.CurrentTenantId ?? _authState.TenantId;
        var companyId = _appState.CurrentCompanyId ?? _authState.CompanyId;
        if (tenantId.HasValue)
            rows = rows.Where(x => x.TenantId == tenantId.Value).ToList();
        if (companyId.HasValue)
            rows = rows.Where(x => x.CompanyId == companyId.Value).ToList();
        return rows.Where(x => x.IsActive && (!onlyOpen || IsOpenPayrollPeriod(x))).OrderByDescending(x => x.StartDate).Select(x => new CatalogLookupItem
        {
            Id = x.PayrollPeriodId.ToString("D"),
            Name = $"{x.Code} · {x.Name}",
            Group = x.Status,
            IsActive = x.IsActive
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPayrollRunLookupsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollRunDto>>("/api/payroll/runs") ?? [];
        return rows.Where(x => x.IsActive).OrderByDescending(x => x.RunDate).Select(x => new CatalogLookupItem
        {
            Id = x.PayrollRunId.ToString("D"),
            Name = $"{x.Folio} · {x.PayrollPeriodName}"
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPayrollConceptLookupsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollConceptDto>>("/api/payroll/concepts") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Code).Select(x => new CatalogLookupItem
        {
            Id = x.PayrollConceptId.ToString("D"),
            Name = $"{x.Code} · {x.Name}"
        }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPayrollIncidentTypeLookupsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<NomPayrollIncidentTypeDto>>("/api/payroll/incident-types") ?? [];
        var tenantId = _appState.CurrentTenantId ?? _authState.TenantId;
        var companyId = _appState.CurrentCompanyId ?? _authState.CompanyId;
        if (tenantId.HasValue)
            rows = rows.Where(x => x.TenantId == tenantId.Value).ToList();
        if (companyId.HasValue)
            rows = rows.Where(x => x.CompanyId == companyId.Value).ToList();
        var branchId = _appState.CurrentBranchId ?? _authState.BranchId;
        if (branchId.HasValue)
            rows = rows.Where(x => !x.BranchId.HasValue || x.BranchId == branchId.Value).ToList();

        return rows.Where(x => x.IsActive)
            .GroupBy(x => x.Code.Trim().ToUpperInvariant())
            .Select(g => g
                .OrderByDescending(x => branchId.HasValue && x.BranchId == branchId.Value)
                .ThenByDescending(x => companyId.HasValue && x.CompanyId == companyId.Value)
                .ThenBy(x => x.BranchId.HasValue)
                .ThenBy(x => x.Name)
                .First())
            .OrderBy(x => x.IncidentCategory).ThenBy(x => x.SortOrder).ThenBy(x => x.Code).Select(x => new CatalogLookupItem
        {
            Id = x.NomPayrollIncidentTypeId.ToString("D"),
            Name = $"{x.Code} · {x.Name}",
            Group = x.IncidentCategory,
            Color = string.IsNullOrWhiteSpace(x.Color) ? CategoryColor(x.IncidentCategory) : x.Color,
            Icon = x.Icon,
            Description = x.Description,
            Code = x.Code,
            AffectType = x.AffectType,
            IsActive = x.IsActive
        }).ToList();
    }

    private static bool IsOpenPayrollPeriod(PayrollPeriodDto period)
    {
        if (!period.IsActive || period.IsClosed)
            return false;
        var status = (period.Status ?? string.Empty).Trim().ToLowerInvariant();
        return status is "open" or "abierto" or "draft" or "captura" or "active" or "activo";
    }

    private async Task<List<CatalogLookupItem>> GetPayrollRunLineLookupsAsync()
    {
        var client = CreateScopedClient();
        var rows = await client.GetFromJsonAsync<List<PayrollRunLineDto>>("/api/payroll/run-lines") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.PayrollRunFolio).ThenBy(x => x.EmployeeName).Select(x => new CatalogLookupItem
        {
            Id = x.PayrollRunLineId.ToString("D"),
            Name = $"{x.PayrollRunFolio} · {x.EmployeeName}"
        }).ToList();
    }

    private static DepartmentRequest MapDepartmentRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Description = ReadString(payload, "Description"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PositionRequest MapPositionRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        DepartmentId = ReadGuid(payload, "DepartmentId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Description = ReadString(payload, "Description"),
        PayrollGroup = ReadString(payload, "PayrollGroup"),
        BaseSalary = ReadDecimal(payload, "BaseSalary"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static EmployeeRequest MapEmployeeRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        DepartmentId = ReadGuid(payload, "DepartmentId"),
        PositionId = ReadGuid(payload, "PositionId"),
        WorkScheduleId = ReadGuid(payload, "WorkScheduleId"),
        Code = ReadString(payload, "Code"),
        EmployeeNumber = ReadString(payload, "EmployeeNumber"),
        ClockKey = ReadString(payload, "ClockKey"),
        NoiKey = ReadString(payload, "NoiKey"),
        FirstName = ReadString(payload, "FirstName"),
        MiddleName = ReadString(payload, "MiddleName"),
        LastName = ReadString(payload, "LastName"),
        SecondLastName = ReadString(payload, "SecondLastName"),
        Email = ReadString(payload, "Email"),
        Phone = ReadString(payload, "Phone"),
        EmergencyPhone = ReadString(payload, "EmergencyPhone"),
        TaxId = ReadString(payload, "TaxId"),
        NationalId = ReadString(payload, "NationalId"),
        HireDate = ReadDate(payload, "HireDate"),
        BirthDate = ReadDate(payload, "BirthDate"),
        Gender = ReadString(payload, "Gender"),
        BloodType = ReadString(payload, "BloodType"),
        MaritalStatus = ReadString(payload, "MaritalStatus"),
        PlaceOfBirth = ReadString(payload, "PlaceOfBirth"),
        Nationality = ReadString(payload, "Nationality"),
        FatherName = ReadString(payload, "FatherName"),
        MotherName = ReadString(payload, "MotherName"),
        AddressStreet = ReadString(payload, "AddressStreet"),
        AddressColony = ReadString(payload, "AddressColony"),
        AddressCity = ReadString(payload, "AddressCity"),
        AddressState = ReadString(payload, "AddressState"),
        AddressZipCode = ReadString(payload, "AddressZipCode"),
        PeriodSalary = ReadDecimal(payload, "PeriodSalary"),
        DailySalary = ReadDecimal(payload, "DailySalary"),
        IntegratedDailySalary = ReadDecimal(payload, "IntegratedDailySalary"),
        SbcFija = ReadDecimal(payload, "SbcFija"),
        Status = ReadString(payload, "Status"),
        TerminationDate = ReadDate(payload, "TerminationDate"),
        TerminationReason = ReadString(payload, "TerminationReason"),
        ReentryDate = ReadDate(payload, "ReentryDate"),
        Curp = ReadString(payload, "Curp"),
        Nss = ReadString(payload, "Nss"),
        ImssRegId = ReadString(payload, "ImssRegId"),
        IsImssRegistered = ReadBool(payload, "IsImssRegistered"),
        ImssRegistrationDate = ReadDate(payload, "ImssRegistrationDate"),
        ImssTerminationDate = ReadDate(payload, "ImssTerminationDate"),
        Umf = ReadString(payload, "Umf"),
        ContractType = ReadString(payload, "ContractType"),
        CotizationBase = ReadString(payload, "CotizationBase"),
        TaxRegime = ReadString(payload, "TaxRegime"),
        EmployeeType = ReadString(payload, "EmployeeType"),
        SalaryZone = ReadString(payload, "SalaryZone"),
        PayrollPeriodType = ReadString(payload, "PayrollPeriodType"),
        Afore = ReadString(payload, "Afore"),
        Fonacot = ReadString(payload, "Fonacot"),
        Infonavit = ReadString(payload, "Infonavit"),
        PaymentForm = ReadString(payload, "PaymentForm"),
        BankCode = ReadString(payload, "BankCode"),
        BankAccount = ReadString(payload, "BankAccount"),
        Clabe = ReadString(payload, "Clabe"),
        BankBranch = ReadString(payload, "BankBranch"),
        ImmediateSupervisor = ReadString(payload, "ImmediateSupervisor"),
        Category = ReadString(payload, "Category"),
        Notes = ReadString(payload, "Notes"),
        PrintReceipt = ReadBool(payload, "PrintReceipt", true),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private EmployeeIncidentRequest MapIncidentRequest(JsonElement payload) => new()
    {
        TenantId = _appState.CurrentTenantId ?? _authState.TenantId ?? ReadGuid(payload, "TenantId"),
        CompanyId = _appState.CurrentCompanyId ?? _authState.CompanyId ?? ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        PayrollIncidentTypeId = ReadGuid(payload, "PayrollIncidentTypeId") ?? ReadGuid(payload, "payrollIncidentTypeId") ?? ReadGuid(payload, "NomPayrollIncidentTypeId") ?? ReadGuid(payload, "nomPayrollIncidentTypeId") ?? ReadGuid(payload, "payroll_incident_type_id"),
        NomPayrollIncidentTypeId = ReadGuid(payload, "PayrollIncidentTypeId") ?? ReadGuid(payload, "payrollIncidentTypeId") ?? ReadGuid(payload, "NomPayrollIncidentTypeId") ?? ReadGuid(payload, "nomPayrollIncidentTypeId") ?? ReadGuid(payload, "payroll_incident_type_id"),
        IncidentDate = ReadDate(payload, "IncidentDate"),
        IncidentType = string.Empty,
        Quantity = ReadDecimal(payload, "Quantity"),
        Amount = ReadDecimal(payload, "Amount"),
        Notes = ReadString(payload, "Notes"),
        Status = ReadString(payload, "Status"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private HrRecurringIncidentRuleRequest MapRecurringIncidentRuleRequest(JsonElement payload) => new()
    {
        TenantId = _appState.CurrentTenantId ?? _authState.TenantId ?? ReadGuid(payload, "TenantId"),
        CompanyId = _appState.CurrentCompanyId ?? _authState.CompanyId ?? ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        NomPayrollIncidentTypeId = ReadGuid(payload, "NomPayrollIncidentTypeId"),
        Amount = ReadDecimal(payload, "Amount"),
        Quantity = ReadDecimal(payload, "Quantity", 1m),
        StartDate = ReadDate(payload, "StartDate"),
        EndDate = ReadDate(payload, "EndDate"),
        Frequency = ReadString(payload, "Frequency", "cada_periodo"),
        Notes = ReadString(payload, "Notes"),
        RequiresAuthorization = ReadBool(payload, "RequiresAuthorization"),
        AuthorizedBy = ReadString(payload, "AuthorizedBy"),
        AuthorizedAt = ReadDate(payload, "AuthorizedAt"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private NomPayrollIncidentTypeRequest MapIncidentTypeRequest(JsonElement payload)
    {
        var code = ReadString(payload, "Code");
        var category = ReadString(payload, "IncidentCategory", InferIncidentCategory(code));
        var affectType = ReadString(payload, "AffectType", category switch
        {
            "DEDUCCION" => "RESTA",
            "PERCEPCION" => "SUMA",
            _ => "NO_AFECTA"
        });
        var payrollConceptType = ReadString(payload, "PayrollConceptType", InferPayrollConceptType(code));

        return new NomPayrollIncidentTypeRequest
        {
            TenantId = _appState.CurrentTenantId ?? _authState.TenantId ?? ReadGuid(payload, "TenantId"),
            CompanyId = ReadGuid(payload, "CompanyId") ?? _appState.CurrentCompanyId ?? _authState.CompanyId,
            BranchId = ReadGuid(payload, "BranchId"),
            Code = code,
            Name = ReadString(payload, "Name"),
            Description = ReadString(payload, "Description"),
            IncidentCategory = category,
            AffectType = affectType,
            PayrollConceptType = payrollConceptType,
            SatCode = ReadString(payload, "SatCode"),
            Color = ReadString(payload, "Color", CategoryColor(category)),
            Icon = ReadString(payload, "Icon", CategoryIcon(category, payrollConceptType)),
            SortOrder = ReadInt(payload, "SortOrder"),
            IsDiscount = ReadBool(payload, "IsDiscount", category == "DEDUCCION"),
            IsPerception = ReadBool(payload, "IsPerception", category == "PERCEPCION"),
            IsInformative = ReadBool(payload, "IsInformative", category == "INFORMATIVA"),
            RequiresAmount = ReadBool(payload, "RequiresAmount", category != "INFORMATIVA"),
            RequiresQuantity = ReadBool(payload, "RequiresQuantity"),
            RequiresAuthorization = ReadBool(payload, "RequiresAuthorization"),
            AppliesToPayroll = ReadBool(payload, "AppliesToPayroll", category != "INFORMATIVA"),
            IsSystem = ReadBool(payload, "IsSystem"),
            IsActive = ReadBool(payload, "IsActive", true)
        };
    }

    private static EmployeeContractRequest MapContractRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        ContractNumber = ReadString(payload, "ContractNumber"),
        ContractType = ReadString(payload, "ContractType"),
        StartDate = ReadDate(payload, "StartDate"),
        EndDate = ReadDate(payload, "EndDate"),
        PaymentFrequency = ReadString(payload, "PaymentFrequency"),
        BaseSalary = ReadDecimal(payload, "BaseSalary"),
        IntegratedSalary = ReadDecimal(payload, "IntegratedSalary"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static HrBankRequest MapHrBankRequest(JsonElement payload) => new()
    {
        Code = ReadString(payload, "Code"),
        ShortName = ReadString(payload, "ShortName"),
        Name = ReadString(payload, "Name"),
        SatCode = ReadString(payload, "SatCode"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static HrTerminationReasonRequest MapTerminationReasonRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Description = ReadString(payload, "Description"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static HrEmployerRegistrationRequest MapEmployerRegistrationRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        RegistrationNumber = ReadString(payload, "RegistrationNumber"),
        RiskClass = ReadString(payload, "RiskClass"),
        State = ReadString(payload, "State"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollPeriodTypeRequest MapPayrollPeriodTypeRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        DaysPerPeriod = ReadInt(payload, "DaysPerPeriod"),
        PeriodsPerYear = ReadInt(payload, "PeriodsPerYear"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private PayrollPeriodRequest MapPayrollPeriodRequest(JsonElement payload) => new()
    {
        TenantId = _appState.CurrentTenantId ?? _authState.TenantId ?? ReadGuid(payload, "TenantId"),
        CompanyId = ReadGuid(payload, "CompanyId") ?? _appState.CurrentCompanyId ?? _authState.CompanyId,
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        PeriodType = ReadString(payload, "PeriodType"),
        StartDate = ReadDate(payload, "StartDate"),
        EndDate = ReadDate(payload, "EndDate"),
        PaymentDate = ReadDate(payload, "PaymentDate"),
        Status = ReadString(payload, "Status"),
        IsImssInsured = ReadBool(payload, "IsImssInsured", true),
        IsClosed = ReadBool(payload, "IsClosed"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollConceptRequest MapPayrollConceptRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        ConceptType = ReadString(payload, "ConceptType"),
        CalculationType = ReadString(payload, "CalculationType"),
        SatCode = ReadString(payload, "SatCode"),
        SatAgrupador = ReadString(payload, "SatAgrupador"),
        TaxableType = ReadString(payload, "TaxableType"),
        IsRecurring = ReadBool(payload, "IsRecurring"),
        IsAutomatic = ReadBool(payload, "IsAutomatic", true),
        PrintOnReceipt = ReadBool(payload, "PrintOnReceipt", true),
        TaxablePercent = ReadDecimal(payload, "TaxablePercent", 100m),
        ExemptPercent = ReadDecimal(payload, "ExemptPercent", 0m),
        SortOrder = ReadInt(payload, "SortOrder"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollRunRequest MapPayrollRunRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        Folio = ReadString(payload, "Folio"),
        RunDate = ReadDate(payload, "RunDate"),
        Status = ReadString(payload, "Status"),
        EmployeeCount = ReadInt(payload, "EmployeeCount"),
        GrossAmount = ReadDecimal(payload, "GrossAmount"),
        DeductionsAmount = ReadDecimal(payload, "DeductionsAmount"),
        NetAmount = ReadDecimal(payload, "NetAmount"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollRunLineRequest MapPayrollRunLineRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        DepartmentId = ReadGuid(payload, "DepartmentId"),
        PositionId = ReadGuid(payload, "PositionId"),
        DaysPaid = ReadDecimal(payload, "DaysPaid"),
        GrossAmount = ReadDecimal(payload, "GrossAmount"),
        DeductionsAmount = ReadDecimal(payload, "DeductionsAmount"),
        NetAmount = ReadDecimal(payload, "NetAmount"),
        IncidentsAmount = ReadDecimal(payload, "IncidentsAmount"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollRunLineDetailRequest MapPayrollRunLineDetailRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PayrollRunLineId = ReadGuid(payload, "PayrollRunLineId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        PayrollConceptId = ReadGuid(payload, "PayrollConceptId"),
        ConceptCode = ReadString(payload, "ConceptCode"),
        ConceptName = ReadString(payload, "ConceptName"),
        ConceptType = ReadString(payload, "ConceptType"),
        SatCode = ReadString(payload, "SatCode"),
        TaxableType = ReadString(payload, "TaxableType"),
        Quantity = ReadDecimal(payload, "Quantity"),
        Amount = ReadDecimal(payload, "Amount"),
        TaxableAmount = ReadDecimal(payload, "TaxableAmount"),
        ExemptAmount = ReadDecimal(payload, "ExemptAmount"),
        SortOrder = ReadInt(payload, "SortOrder"),
        IsGenerated = ReadBool(payload, "IsGenerated", true),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static CatalogViewDefinition BuildView(
        string catalogKey,
        string title,
        string subtitle,
        string keyExpr,
        List<CatalogColumnDefinition> columns,
        List<Dictionary<string, object?>> rows,
        bool allowImport = false)
        => new()
        {
            CatalogKey = catalogKey,
            Title = title,
            Subtitle = subtitle,
            KeyExpr = keyExpr,
            AllowCreate = true,
            AllowUpdate = true,
            AllowDelete = true,
            AllowImport = allowImport,
            TotalCount = rows.Count,
            ActiveCount = rows.Count(x => Convert.ToBoolean(x.GetValueOrDefault("IsActive") ?? false)),
            InactiveCount = rows.Count(x => !Convert.ToBoolean(x.GetValueOrDefault("IsActive") ?? false)),
            Columns = columns,
            Rows = rows
        };

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
            row[key] = value;
        return row;
    }

    private static CatalogColumnDefinition TextColumn(string field, string caption, bool required = false, bool allowEditing = true, int width = 160, bool visible = true)
        => new() { DataField = field, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Visible = visible, Width = width };

    private static CatalogColumnDefinition NumberColumn(string field, string caption, bool required = false, int width = 120)
        => new() { DataField = field, Caption = caption, DataType = "number", Required = required, Width = width };

    private static CatalogColumnDefinition BoolColumn(string field, string caption, int width = 90)
        => new() { DataField = field, Caption = caption, DataType = "boolean", Width = width };

    private static CatalogColumnDefinition DateColumn(string field, string caption, bool required = false, int width = 120)
        => new() { DataField = field, Caption = caption, DataType = "date", Required = required, Width = width };

    private static CatalogColumnDefinition LookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, bool required = false, int width = 180, string? quickCreateKey = null)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "string",
            Required = required,
            Width = width,
            UseLookup = true,
            LookupItems = lookupItems,
            QuickCreateKey = quickCreateKey
        };

    private static CatalogColumnDefinition ReadOnlyLookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, int width = 180)
        => new()
        {
            DataField = field,
            Caption = caption,
            DataType = "string",
            AllowEditing = false,
            Width = width,
            UseLookup = true,
            LookupItems = lookupItems
        };

    private static CatalogColumnDefinition SmartLookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, bool required = false, int width = 180)
        => lookupItems.Count <= 1
            ? new() { DataField = field, Caption = caption, DataType = "string", Visible = false, ShowInGrid = false, AllowEditing = false, Width = width, UseLookup = true, LookupItems = lookupItems }
            : LookupColumn(field, caption, lookupItems, required, width);

    private static string ReadString(JsonElement payload, string name, string fallback = "")
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => fallback,
            JsonValueKind.String => value.GetString() ?? fallback,
            _ => value.ToString()
        };
    }

    private static Guid? ReadGuid(JsonElement payload, string name)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String when Guid.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static DateTime? ReadDate(JsonElement payload, string name)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String when DateTime.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static bool ReadBool(JsonElement payload, string name, bool fallback = false)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static int ReadInt(JsonElement payload, string name, int fallback = 0)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var parsed) => parsed,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static decimal ReadDecimal(JsonElement payload, string name, decimal fallback = 0m)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var parsed) => parsed,
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static bool TryGetPropertyInsensitive(JsonElement payload, string name, out JsonElement value)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (payload.TryGetProperty(name, out value))
            return true;

        var camel = char.ToLowerInvariant(name[0]) + name[1..];
        if (payload.TryGetProperty(camel, out value))
            return true;

        foreach (var property in payload.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string InferIncidentCategory(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Contains("FALTA") || normalized.Contains("RETARDO") || normalized.Contains("DESCUENTO") || normalized.Contains("DANO") || normalized.Contains("PRESTAMO"))
            return "DEDUCCION";
        if (normalized.Contains("HORA") || normalized.Contains("BONO") || normalized.Contains("COMISION"))
            return "PERCEPCION";
        return "INFORMATIVA";
    }

    private static string InferPayrollConceptType(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Contains("FALTA")) return "FALTA";
        if (normalized.Contains("RETARDO")) return "RETARDO";
        if (normalized.Contains("HORA")) return "HORAS_EXTRA";
        if (normalized.Contains("BONO")) return "BONO";
        if (normalized.Contains("COMISION")) return "COMISION";
        if (normalized.Contains("VACACION")) return "VACACIONES";
        if (normalized.Contains("INCAPAC")) return "INCAPACIDAD";
        if (normalized.Contains("PRESTAMO")) return "PRESTAMO";
        if (normalized.Contains("DESCUENTO") || normalized.Contains("DANO")) return "DESCUENTO_DANOS";
        return "OTRO";
    }

    private static string CategoryColor(string? category) => (category ?? string.Empty).Trim().ToUpperInvariant() switch
    {
        "DEDUCCION" => "#DC2626",
        "PERCEPCION" => "#16A34A",
        "INFORMATIVA" => "#2563EB",
        _ => "#64748B"
    };

    private static bool IsIncidentCategory(string? value, string expected)
        => string.Equals((value ?? string.Empty).Trim(), expected, StringComparison.OrdinalIgnoreCase);

    private static string CategoryIcon(string? category, string? payrollConceptType) => (payrollConceptType ?? string.Empty).Trim().ToUpperInvariant() switch
    {
        "FALTA" => "calendar-x",
        "RETARDO" => "clock-alert",
        "HORAS_EXTRA" => "clock-plus",
        "BONO" => "badge-dollar-sign",
        "COMISION" => "percent",
        "VACACIONES" => "plane",
        "INCAPACIDAD" => "shield-alert",
        "PRESTAMO" => "hand-coins",
        "DESCUENTO_DANOS" => "triangle-alert",
        _ => (category ?? string.Empty).Trim().ToUpperInvariant() == "INFORMATIVA" ? "info" : "circle-dot"
    };

    // ── Attendance Policies ─────────────────────────────────────────────────────

    public async Task<List<WorkShiftLookup>> GetWorkShiftLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<WorkShiftSimpleDto>>("/api/hr/shifts") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new WorkShiftLookup { WorkShiftId = x.WorkShiftId, Code = x.Code, Name = x.Name }).ToList();
    }

    public async Task<List<AttendancePolicyItem>> GetAttendancePoliciesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<AttendancePolicyItem>>("/api/hr/attendance-policies") ?? [];
    }

    public async Task<(bool Ok, string? Error)> CreateAttendancePolicyAsync(AttendancePolicyItem item)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsJsonAsync("/api/hr/attendance-policies", new
        {
            item.Code, item.Name, item.Description,
            item.WorkShiftId, item.ToleranceMinutes, item.MinOvertimeMinutes,
            item.RequiresPunchIn, item.RequiresPunchOut,
            item.IsDefault, item.Notes, item.IsActive
        });
        if (!response.IsSuccessStatusCode)
            return (false, await response.Content.ReadAsStringAsync());
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAttendancePolicyAsync(Guid id, AttendancePolicyItem item)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PutAsJsonAsync($"/api/hr/attendance-policies/{id:D}", new
        {
            item.Code, item.Name, item.Description,
            item.WorkShiftId, item.ToleranceMinutes, item.MinOvertimeMinutes,
            item.RequiresPunchIn, item.RequiresPunchOut,
            item.IsDefault, item.Notes, item.IsActive
        });
        if (!response.IsSuccessStatusCode)
            return (false, await response.Content.ReadAsStringAsync());
        return (true, null);
    }

    public async Task<List<AttendancePolicyRuleItem>> GetAttendancePolicyRulesAsync(Guid policyId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<AttendancePolicyRuleItem>>($"/api/hr/attendance-policy-rules/by-policy/{policyId:D}") ?? [];
    }

    public async Task<(bool Ok, string? Error)> CreateAttendancePolicyRuleAsync(AttendancePolicyRuleItem item)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsJsonAsync("/api/hr/attendance-policy-rules", new
        {
            item.AttendancePolicyId, item.Code, item.Name,
            item.RuleType, item.ConditionType,
            item.ThresholdMinutes, item.ThresholdDays,
            item.ActionType, item.ActionValue, item.IncidentTypeCode,
            item.SortOrder, item.Notes, item.IsActive
        });
        if (!response.IsSuccessStatusCode)
            return (false, await response.Content.ReadAsStringAsync());
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAttendancePolicyRuleAsync(Guid id, AttendancePolicyRuleItem item)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PutAsJsonAsync($"/api/hr/attendance-policy-rules/{id:D}", new
        {
            item.AttendancePolicyId, item.Code, item.Name,
            item.RuleType, item.ConditionType,
            item.ThresholdMinutes, item.ThresholdDays,
            item.ActionType, item.ActionValue, item.IncidentTypeCode,
            item.SortOrder, item.Notes, item.IsActive
        });
        if (!response.IsSuccessStatusCode)
            return (false, await response.Content.ReadAsStringAsync());
        return (true, null);
    }

    private sealed class WorkShiftSimpleDto
    {
        public Guid WorkShiftId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    // ── Manual Incidents ────────────────────────────────────────────────────

    public async Task<List<EmployeeSimpleLookup>> GetEmployeeSimpleListAsync()
    {
        var c = CreateScopedClient();
        var full = await c.GetFromJsonAsync<List<EmployeeDto>>("/api/hr/employees") ?? [];
        return full.Where(e => e.IsActive).Select(e => new EmployeeSimpleLookup
        {
            EmployeeId = e.EmployeeId,
            EmployeeNumber = e.EmployeeNumber,
            FullName = string.IsNullOrWhiteSpace(e.FullName)
                ? string.Join(" ", new[] { e.FirstName, e.MiddleName, e.LastName, e.SecondLastName }.Where(s => !string.IsNullOrWhiteSpace(s)))
                : e.FullName
        }).OrderBy(e => e.FullName).ToList();
    }

    public async Task<List<DepartmentDto>> GetDepartmentSimpleListAsync()
    {
        var c = CreateScopedClient();
        var rows = await c.GetFromJsonAsync<List<DepartmentDto>>("/api/hr/departments") ?? [];
        return rows.Where(d => d.IsActive).OrderBy(d => d.Name).ToList();
    }

    public async Task<List<NomPayrollIncidentTypeDto>> GetIncidentTypeLookupsAsync()
    {
        var c = CreateScopedClient();
        return await c.GetFromJsonAsync<List<NomPayrollIncidentTypeDto>>("/api/payroll/incident-types") ?? [];
    }

    public async Task<List<PayrollPeriodDto>> GetPeriodsForSelectionAsync()
    {
        var c = CreateScopedClient();
        var rows = await c.GetFromJsonAsync<List<PayrollPeriodDto>>("/api/payroll/periods") ?? [];
        return rows.Where(x => x.IsActive).OrderByDescending(x => x.StartDate).ToList();
    }

    public async Task<(bool Ok, string? Error)> CreatePeriodAsync(PayrollPeriodRequest req)
    {
        var c = CreateScopedClient();
        var response = await c.PostAsJsonAsync("/api/payroll/periods", req);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await TryReadErrorAsync(response));
    }

    public async Task<(bool Ok, string? Error)> UpdatePeriodAsync(Guid id, PayrollPeriodRequest req)
    {
        var c = CreateScopedClient();
        var response = await c.PutAsJsonAsync($"/api/payroll/periods/{id:D}", req);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await TryReadErrorAsync(response));
    }

    public async Task<List<PayrollPeriodDto>> GetAllPeriodsAsync()
    {
        var c = CreateScopedClient();
        return await c.GetFromJsonAsync<List<PayrollPeriodDto>>("/api/payroll/periods") ?? [];
    }

    public async Task<(bool Ok, string? Error)> ClosePeriodAsync(Guid id)
    {
        var c = CreateScopedClient();
        var response = await c.PostAsync($"/api/payroll/periods/{id:D}/close", null);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await TryReadErrorAsync(response));
    }

    public async Task<(bool Ok, string? Error)> ReopenPeriodAsync(Guid id)
    {
        var c = CreateScopedClient();
        var response = await c.PostAsync($"/api/payroll/periods/{id:D}/reopen", null);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await TryReadErrorAsync(response));
    }

    public async Task<List<EmployeeIncidentDto>> GetManualIncidentsAsync(Guid? periodId = null, Guid? employeeId = null)
    {
        var c = CreateScopedClient();
        var qs = "";
        if (periodId.HasValue) qs += $"periodId={periodId:D}";
        if (employeeId.HasValue) qs += (qs.Length > 0 ? "&" : "") + $"employeeId={employeeId:D}";
        var url = "/api/hr/incidents" + (qs.Length > 0 ? "?" + qs : "");
        return await c.GetFromJsonAsync<List<EmployeeIncidentDto>>(url) ?? [];
    }

    public async Task<(bool Ok, string? Error)> CreateManualIncidentAsync(ManualIncidentRequest req)
    {
        var c = CreateScopedClient();
        var response = await c.PostAsJsonAsync("/api/hr/incidents", req);
        if (response.IsSuccessStatusCode) return (true, null);
        var msg = await TryReadErrorAsync(response);
        return (false, msg);
    }

    public async Task<(bool Ok, string? Error)> UpdateManualIncidentAsync(Guid id, ManualIncidentRequest req)
    {
        var c = CreateScopedClient();
        var response = await c.PutAsJsonAsync($"/api/hr/incidents/{id:D}", req);
        if (response.IsSuccessStatusCode) return (true, null);
        var msg = await TryReadErrorAsync(response);
        return (false, msg);
    }

    public async Task<(bool Ok, string? Error)> DeleteManualIncidentAsync(Guid id)
    {
        var c = CreateScopedClient();
        var response = await c.DeleteAsync($"/api/hr/incidents/{id:D}");
        if (response.IsSuccessStatusCode) return (true, null);
        var msg = await TryReadErrorAsync(response);
        return (false, msg);
    }

    public async Task<(bool Ok, int Created, string? Error)> BulkCreateIncidentsAsync(BulkIncidentItem req)
    {
        var c = CreateScopedClient();
        var response = await c.PostAsJsonAsync("/api/hr/incidents/bulk", req);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<BulkResult>();
            return (true, result?.Created ?? 0, null);
        }
        var msg = await TryReadErrorAsync(response);
        return (false, 0, msg);
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content)) return "Error desconocido.";
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString();
            return content;
        }
        catch { return "Error al procesar respuesta del servidor."; }
    }

    private sealed class BulkResult { public int Created { get; set; } }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("La API devolvió un error sin detalle.");

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var message))
                throw new InvalidOperationException(message.GetString() ?? content);
        }
        catch (JsonException)
        {
        }

        throw new InvalidOperationException(content);
    }
}

public sealed class CompanyLookupDto
{
    public Guid CompanyId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class BranchLookupDto
{
    public Guid BranchId { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class DepartmentDto
{
    public Guid DepartmentId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PositionDto
{
    public Guid PositionId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PayrollGroup { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class EmployeeDto
{
    public Guid EmployeeId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public Guid? WorkScheduleId { get; set; }
    public string WorkScheduleName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string? ClockKey { get; set; }
    public string? NoiKey { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? SecondLastName { get; set; }
    public string MiddleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EmergencyPhone { get; set; }
    public string TaxId { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? BloodType { get; set; }
    public string? MaritalStatus { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressColony { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressZipCode { get; set; }
    public decimal PeriodSalary { get; set; }
    public decimal DailySalary { get; set; }
    public decimal IntegratedDailySalary { get; set; }
    public decimal SbcFija { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public DateTime? ReentryDate { get; set; }
    // IMSS / SAT
    public string Curp { get; set; } = string.Empty;
    public string Nss { get; set; } = string.Empty;
    public string ImssRegId { get; set; } = string.Empty;
    public bool IsImssRegistered { get; set; }
    public DateTime? ImssRegistrationDate { get; set; }
    public DateTime? ImssTerminationDate { get; set; }
    public string? Umf { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string CotizationBase { get; set; } = string.Empty;
    public string TaxRegime { get; set; } = string.Empty;
    public string EmployeeType { get; set; } = string.Empty;
    public string SalaryZone { get; set; } = string.Empty;
    public string PayrollPeriodType { get; set; } = string.Empty;
    // Fondos
    public string? Afore { get; set; }
    public string? Fonacot { get; set; }
    public string? Infonavit { get; set; }
    // Banco
    public string PaymentForm { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Clabe { get; set; } = string.Empty;
    public string? BankBranch { get; set; }
    // Otros
    public string? ImmediateSupervisor { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public bool PrintReceipt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class WorkScheduleLookupDto
{
    public Guid WorkScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeIncidentDto
{
    public Guid EmployeeIncidentId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public Guid? PayrollIncidentTypeId { get; set; }
    public Guid? NomPayrollIncidentTypeId { get; set; }
    public string NomPayrollIncidentTypeCode { get; set; } = string.Empty;
    public string NomPayrollIncidentTypeName { get; set; } = string.Empty;
    public string IncidentCategory { get; set; } = string.Empty;
    public string AffectType { get; set; } = string.Empty;
    public string PayrollConceptType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime? IncidentDate { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Origin { get; set; } = "manual";
    public bool ManuallyEdited { get; set; }
    public bool IsActive { get; set; }
}

public class HrRecurringIncidentRuleRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? NomPayrollIncidentTypeId { get; set; }
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Frequency { get; set; } = "cada_periodo";
    public string Notes { get; set; } = string.Empty;
    public bool RequiresAuthorization { get; set; }
    public string AuthorizedBy { get; set; } = string.Empty;
    public DateTime? AuthorizedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class HrRecurringIncidentRuleDto : HrRecurringIncidentRuleRequest
{
    public Guid HrRecurringIncidentRuleId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string NomPayrollIncidentTypeName { get; set; } = string.Empty;
    public string IncidentCategory { get; set; } = string.Empty;
    public string AffectType { get; set; } = string.Empty;
}

public class NomPayrollIncidentTypeRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IncidentCategory { get; set; } = string.Empty;
    public string AffectType { get; set; } = string.Empty;
    public string PayrollConceptType { get; set; } = string.Empty;
    public string SatCode { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsDiscount { get; set; }
    public bool IsPerception { get; set; }
    public bool IsInformative { get; set; }
    public bool RequiresAmount { get; set; }
    public bool RequiresQuantity { get; set; }
    public bool RequiresAuthorization { get; set; }
    public bool AppliesToPayroll { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class NomPayrollIncidentTypeDto : NomPayrollIncidentTypeRequest
{
    public Guid NomPayrollIncidentTypeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

public sealed class EmployeeContractDto
{
    public Guid EmployeeContractId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string PaymentFrequency { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal IntegratedSalary { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PayrollPeriodTypeDto
{
    public Guid PayrollPeriodTypeId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DaysPerPeriod { get; set; }
    public int PeriodsPerYear { get; set; }
    public int PaymentDays { get; set; }
    public int WorkingDays { get; set; }
    public bool AdjustToCalendarMonth { get; set; }
    public string QuinceaAdjustType { get; set; } = "LaborDays";
    public int? SeventhDayPosition { get; set; }
    public int PaymentDayPosition { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public sealed class PayrollPeriodTypeRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int DaysPerPeriod { get; set; }
    public int PeriodsPerYear { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PayrollPeriodDto
{
    public Guid PayrollPeriodId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PeriodType { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsImssInsured { get; set; } = true;
    public bool IsClosed { get; set; }
    public bool IsActive { get; set; }
}

public sealed class PayrollConceptDto
{
    public Guid PayrollConceptId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public string SatCode { get; set; } = string.Empty;
    public string SatAgrupador { get; set; } = string.Empty;
    public string TaxableType { get; set; } = string.Empty;
    public decimal TaxablePercent { get; set; }
    public decimal ExemptPercent { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsAutomatic { get; set; }
    public bool PrintOnReceipt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class PayrollRunDto
{
    public Guid PayrollRunId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid? PayrollPeriodId { get; set; }
    public string PayrollPeriodName { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime? RunDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PayrollRunLineDto
{
    public Guid PayrollRunLineId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? PayrollRunId { get; set; }
    public string PayrollRunFolio { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public decimal DaysPaid { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal IncidentsAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}


public sealed class PayrollRunLineDetailDto
{
    public Guid PayrollRunLineDetailId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? PayrollRunId { get; set; }
    public string PayrollRunFolio { get; set; } = string.Empty;
    public Guid? PayrollRunLineId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? PayrollConceptId { get; set; }
    public string PayrollConceptName { get; set; } = string.Empty;
    public string ConceptCode { get; set; } = string.Empty;
    public string ConceptName { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty;
    public string SatCode { get; set; } = string.Empty;
    public string TaxableType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public int SortOrder { get; set; }
    public bool IsGenerated { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class HrBankDto
{
    public Guid BankId { get; set; }
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SatCode { get; set; }
    public bool IsActive { get; set; }
}

public sealed class HrTerminationReasonDto
{
    public Guid TerminationReasonId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public sealed class HrEmployerRegistrationDto
{
    public Guid EmployerRegistrationId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? RiskClass { get; set; }
    public string? State { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public sealed class HrBankRequest
{
    public string? Code { get; set; }
    public string? ShortName { get; set; }
    public string? Name { get; set; }
    public string? SatCode { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class HrTerminationReasonRequest
{
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class HrEmployerRegistrationRequest
{
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RiskClass { get; set; }
    public string? State { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class WorkShiftLookup
{
    public Guid WorkShiftId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class AttendancePolicyItem
{
    public Guid AttendancePolicyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? WorkShiftId { get; set; }
    public string WorkShiftName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string Scope { get; set; } = "company";
    public int Priority { get; set; } = 100;
    public int ToleranceMinutes { get; set; }
    public int MinOvertimeMinutes { get; set; } = 15;
    public bool RequiresPunchIn { get; set; } = true;
    public bool RequiresPunchOut { get; set; } = true;
    public bool IsDefault { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class AttendancePolicyRuleItem
{
    public Guid AttendancePolicyRuleId { get; set; }
    public Guid AttendancePolicyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string ConditionType { get; set; } = "GreaterThan";
    public int? ThresholdMinutes { get; set; }
    public decimal? ThresholdDays { get; set; }
    public string ActionType { get; set; } = "CreateIncident";
    public decimal ActionValue { get; set; }
    public string? IncidentTypeCode { get; set; }
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class ManualIncidentRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? NomPayrollIncidentTypeId { get; set; }
    public DateTime? IncidentDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string Origin { get; set; } = "manual";
    public bool ManuallyEdited { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class BulkIncidentItem
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public Guid? NomPayrollIncidentTypeId { get; set; }
    public DateTime? IncidentDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<Guid> EmployeeIds { get; set; } = [];
}

public sealed class EmployeeSimpleLookup
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
