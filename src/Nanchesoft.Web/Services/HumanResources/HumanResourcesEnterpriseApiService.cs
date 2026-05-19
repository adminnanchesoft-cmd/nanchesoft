using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;
using Nanchesoft.Web.State;

namespace Nanchesoft.Web.Services.HumanResources;

public sealed class HumanResourcesEnterpriseApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppState _appState;
    private readonly AuthState _authState;

    public HumanResourcesEnterpriseApiService(IHttpClientFactory httpClientFactory, AppState appState, AuthState authState)
    {
        _httpClientFactory = httpClientFactory;
        _appState = appState;
        _authState = authState;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey.ToLowerInvariant() switch
        {
            "hr-shifts" => GetShiftsAsync(),
            "hr-work-schedules" => GetWorkSchedulesAsync(),
            "hr-time-clock-devices" => GetTimeClockDevicesAsync(),
            "hr-leave-types" => GetLeaveTypesAsync(),
            "hr-vacation-requests" => GetVacationRequestsAsync(),
            "hr-employee-documents" => GetEmployeeDocumentsAsync(),
            "hr-employee-movements" => GetEmployeeMovementsAsync(),
            "hr-employee-certifications" => GetEmployeeCertificationsAsync(),
            "hr-recruitment-vacancies" => GetRecruitmentVacanciesAsync(),
            "hr-candidate-applications" => GetCandidateApplicationsAsync(),
            "hr-onboarding-checklists" => GetOnboardingChecklistsAsync(),
            "hr-performance-reviews" => GetPerformanceReviewsAsync(),
            "hr-competency-assessments" => GetCompetencyAssessmentsAsync(),
            "hr-succession-plans" => GetSuccessionPlansAsync(),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = catalogKey.ToLowerInvariant() switch
        {
            "hr-shifts" => await client.PostAsJsonAsync("/api/hr/shifts", MapShiftRequest(payload)),
            "hr-work-schedules" => await client.PostAsJsonAsync("/api/hr/work-schedules", MapWorkScheduleRequest(payload)),
            "hr-time-clock-devices" => await client.PostAsJsonAsync("/api/hr/time-clock-devices", MapTimeClockDeviceRequest(payload)),
            "hr-leave-types" => await client.PostAsJsonAsync("/api/hr/leave-types", MapLeaveTypeRequest(payload)),
            "hr-vacation-requests" => await client.PostAsJsonAsync("/api/hr/vacation-requests", MapVacationRequest(payload)),
            "hr-employee-documents" => await client.PostAsJsonAsync("/api/hr/employee-documents", MapEmployeeDocumentRequest(payload)),
            "hr-employee-movements" => await client.PostAsJsonAsync("/api/hr/employee-movements", MapEmployeeMovementRequest(payload)),
            "hr-employee-certifications" => await client.PostAsJsonAsync("/api/hr/employee-certifications", MapEmployeeCertificationRequest(payload)),
            "hr-recruitment-vacancies" => await client.PostAsJsonAsync("/api/hr/recruitment-vacancies", MapRecruitmentVacancyRequest(payload)),
            "hr-candidate-applications" => await client.PostAsJsonAsync("/api/hr/candidate-applications", MapCandidateApplicationRequest(payload)),
            "hr-onboarding-checklists" => await client.PostAsJsonAsync("/api/hr/onboarding-checklists", MapOnboardingChecklistRequest(payload)),
            "hr-performance-reviews" => await client.PostAsJsonAsync("/api/hr/performance-reviews", MapPerformanceReviewRequest(payload)),
            "hr-competency-assessments" => await client.PostAsJsonAsync("/api/hr/competency-assessments", MapCompetencyAssessmentRequest(payload)),
            "hr-succession-plans" => await client.PostAsJsonAsync("/api/hr/succession-plans", MapSuccessionPlanRequest(payload)),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> UpdateAsync(string catalogKey, string key, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = catalogKey.ToLowerInvariant() switch
        {
            "hr-shifts" => await client.PutAsJsonAsync($"/api/hr/shifts/{key}", MapShiftRequest(payload)),
            "hr-work-schedules" => await client.PutAsJsonAsync($"/api/hr/work-schedules/{key}", MapWorkScheduleRequest(payload)),
            "hr-time-clock-devices" => await client.PutAsJsonAsync($"/api/hr/time-clock-devices/{key}", MapTimeClockDeviceRequest(payload)),
            "hr-leave-types" => await client.PutAsJsonAsync($"/api/hr/leave-types/{key}", MapLeaveTypeRequest(payload)),
            "hr-vacation-requests" => await client.PutAsJsonAsync($"/api/hr/vacation-requests/{key}", MapVacationRequest(payload)),
            "hr-employee-documents" => await client.PutAsJsonAsync($"/api/hr/employee-documents/{key}", MapEmployeeDocumentRequest(payload)),
            "hr-employee-movements" => await client.PutAsJsonAsync($"/api/hr/employee-movements/{key}", MapEmployeeMovementRequest(payload)),
            "hr-employee-certifications" => await client.PutAsJsonAsync($"/api/hr/employee-certifications/{key}", MapEmployeeCertificationRequest(payload)),
            "hr-recruitment-vacancies" => await client.PutAsJsonAsync($"/api/hr/recruitment-vacancies/{key}", MapRecruitmentVacancyRequest(payload)),
            "hr-candidate-applications" => await client.PutAsJsonAsync($"/api/hr/candidate-applications/{key}", MapCandidateApplicationRequest(payload)),
            "hr-onboarding-checklists" => await client.PutAsJsonAsync($"/api/hr/onboarding-checklists/{key}", MapOnboardingChecklistRequest(payload)),
            "hr-performance-reviews" => await client.PutAsJsonAsync($"/api/hr/performance-reviews/{key}", MapPerformanceReviewRequest(payload)),
            "hr-competency-assessments" => await client.PutAsJsonAsync($"/api/hr/competency-assessments/{key}", MapCompetencyAssessmentRequest(payload)),
            "hr-succession-plans" => await client.PutAsJsonAsync($"/api/hr/succession-plans/{key}", MapSuccessionPlanRequest(payload)),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task<CatalogViewDefinition> DeleteAsync(string catalogKey, string key)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var endpoint = catalogKey.ToLowerInvariant() switch
        {
            "hr-shifts" => $"/api/hr/shifts/{key}",
            "hr-work-schedules" => $"/api/hr/work-schedules/{key}",
            "hr-time-clock-devices" => $"/api/hr/time-clock-devices/{key}",
            "hr-leave-types" => $"/api/hr/leave-types/{key}",
            "hr-vacation-requests" => $"/api/hr/vacation-requests/{key}",
            "hr-employee-documents" => $"/api/hr/employee-documents/{key}",
            "hr-employee-movements" => $"/api/hr/employee-movements/{key}",
            "hr-employee-certifications" => $"/api/hr/employee-certifications/{key}",
            "hr-recruitment-vacancies" => $"/api/hr/recruitment-vacancies/{key}",
            "hr-candidate-applications" => $"/api/hr/candidate-applications/{key}",
            "hr-onboarding-checklists" => $"/api/hr/onboarding-checklists/{key}",
            "hr-performance-reviews" => $"/api/hr/performance-reviews/{key}",
            "hr-competency-assessments" => $"/api/hr/competency-assessments/{key}",
            "hr-succession-plans" => $"/api/hr/succession-plans/{key}",
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

        var response = await client.DeleteAsync(endpoint);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    private async Task<CatalogViewDefinition> GetShiftsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<WorkShiftDto>>("/api/hr/shifts") ?? [];
        var companies = await GetCompanyLookupsAsync();

        return BuildView(
            "hr-shifts",
            "Turnos",
            "Catálogo enterprise de turnos operativos y administrativos.",
            "WorkShiftId",
            [
                TextColumn("WorkShiftId", "Shift ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                TextColumn("Code", "Código", required: true, width: 120),
                TextColumn("Name", "Turno", required: true, width: 220),
                TextColumn("StartTime", "Entrada", required: true, width: 100),
                TextColumn("EndTime", "Salida", required: true, width: 100),
                NumberColumn("BreakMinutes", "Descanso min", width: 110),
                NumberColumn("ToleranceMinutes", "Tolerancia min", width: 110),
                BoolColumn("IsOvernight", "Nocturno", width: 90),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("WorkShiftId", x.WorkShiftId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("StartTime", x.StartTime),
                ("EndTime", x.EndTime),
                ("BreakMinutes", x.BreakMinutes),
                ("ToleranceMinutes", x.ToleranceMinutes),
                ("IsOvernight", x.IsOvernight),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetWorkSchedulesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<WorkScheduleDto>>("/api/hr/work-schedules") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var shifts = await GetShiftLookupsAsync();

        return BuildView(
            "hr-work-schedules",
            "Horarios laborales",
            "Jornadas semanales con turno base y días laborables por esquema.",
            "WorkScheduleId",
            [
                TextColumn("WorkScheduleId", "Schedule ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("WorkShiftId", "Turno base", shifts, width: 220, quickCreateKey: "hr-shifts"),
                TextColumn("Code", "Código", required: true, width: 120),
                TextColumn("Name", "Horario", required: true, width: 220),
                BoolColumn("Monday", "Lun", width: 60),
                BoolColumn("Tuesday", "Mar", width: 60),
                BoolColumn("Wednesday", "Mié", width: 60),
                BoolColumn("Thursday", "Jue", width: 60),
                BoolColumn("Friday", "Vie", width: 60),
                BoolColumn("Saturday", "Sáb", width: 60),
                BoolColumn("Sunday", "Dom", width: 60),
                // Per-day fields — visible en popup, ocultos en la tabla grid
                TimeColumn("MonEntryTime", "Lun - Entrada", showInGrid: false),
                NumberColumn("MonToleranceMinutes", "Lun - Tolerancia (min)", width: 130, showInGrid: false),
                TimeColumn("MonLunchStartTime", "Lun - Sale a comer", showInGrid: false),
                TimeColumn("MonLunchEndTime", "Lun - Regresa comida", showInGrid: false),
                TimeColumn("MonExitTime", "Lun - Salida", showInGrid: false),
                TimeColumn("TueEntryTime", "Mar - Entrada", showInGrid: false),
                NumberColumn("TueToleranceMinutes", "Mar - Tolerancia (min)", width: 130, showInGrid: false),
                TimeColumn("TueLunchStartTime", "Mar - Sale a comer", showInGrid: false),
                TimeColumn("TueLunchEndTime", "Mar - Regresa comida", showInGrid: false),
                TimeColumn("TueExitTime", "Mar - Salida", showInGrid: false),
                TimeColumn("WedEntryTime", "Mié - Entrada", showInGrid: false),
                NumberColumn("WedToleranceMinutes", "Mié - Tolerancia (min)", width: 130, showInGrid: false),
                TimeColumn("WedLunchStartTime", "Mié - Sale a comer", showInGrid: false),
                TimeColumn("WedLunchEndTime", "Mié - Regresa comida", showInGrid: false),
                TimeColumn("WedExitTime", "Mié - Salida", showInGrid: false),
                TimeColumn("ThuEntryTime", "Jue - Entrada", showInGrid: false),
                NumberColumn("ThuToleranceMinutes", "Jue - Tolerancia (min)", width: 130, showInGrid: false),
                TimeColumn("ThuLunchStartTime", "Jue - Sale a comer", showInGrid: false),
                TimeColumn("ThuLunchEndTime", "Jue - Regresa comida", showInGrid: false),
                TimeColumn("ThuExitTime", "Jue - Salida", showInGrid: false),
                TimeColumn("FriEntryTime", "Vie - Entrada", showInGrid: false),
                NumberColumn("FriToleranceMinutes", "Vie - Tolerancia (min)", width: 130, showInGrid: false),
                TimeColumn("FriLunchStartTime", "Vie - Sale a comer", showInGrid: false),
                TimeColumn("FriLunchEndTime", "Vie - Regresa comida", showInGrid: false),
                TimeColumn("FriExitTime", "Vie - Salida", showInGrid: false),
                TimeColumn("SatEntryTime", "Sáb - Entrada", showInGrid: false),
                NumberColumn("SatToleranceMinutes", "Sáb - Tolerancia (min)", width: 130, showInGrid: false),
                TimeColumn("SatLunchStartTime", "Sáb - Sale a comer", showInGrid: false),
                TimeColumn("SatLunchEndTime", "Sáb - Regresa comida", showInGrid: false),
                TimeColumn("SatExitTime", "Sáb - Salida", showInGrid: false),
                TimeColumn("SunEntryTime", "Dom - Entrada", showInGrid: false),
                NumberColumn("SunToleranceMinutes", "Dom - Tolerancia (min)", width: 130, showInGrid: false),
                TimeColumn("SunLunchStartTime", "Dom - Sale a comer", showInGrid: false),
                TimeColumn("SunLunchEndTime", "Dom - Regresa comida", showInGrid: false),
                TimeColumn("SunExitTime", "Dom - Salida", showInGrid: false),
                NumberColumn("WeeklyHours", "Horas sem", width: 110),
                BoolColumn("IsFlexible", "Flexible", width: 90),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("WorkScheduleId", x.WorkScheduleId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("WorkShiftId", x.WorkShiftId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Monday", x.Monday),
                ("Tuesday", x.Tuesday),
                ("Wednesday", x.Wednesday),
                ("Thursday", x.Thursday),
                ("Friday", x.Friday),
                ("Saturday", x.Saturday),
                ("Sunday", x.Sunday),
                ("MonEntryTime", x.MonEntryTime),
                ("MonToleranceMinutes", x.MonToleranceMinutes),
                ("MonLunchStartTime", x.MonLunchStartTime),
                ("MonLunchEndTime", x.MonLunchEndTime),
                ("MonExitTime", x.MonExitTime),
                ("TueEntryTime", x.TueEntryTime),
                ("TueToleranceMinutes", x.TueToleranceMinutes),
                ("TueLunchStartTime", x.TueLunchStartTime),
                ("TueLunchEndTime", x.TueLunchEndTime),
                ("TueExitTime", x.TueExitTime),
                ("WedEntryTime", x.WedEntryTime),
                ("WedToleranceMinutes", x.WedToleranceMinutes),
                ("WedLunchStartTime", x.WedLunchStartTime),
                ("WedLunchEndTime", x.WedLunchEndTime),
                ("WedExitTime", x.WedExitTime),
                ("ThuEntryTime", x.ThuEntryTime),
                ("ThuToleranceMinutes", x.ThuToleranceMinutes),
                ("ThuLunchStartTime", x.ThuLunchStartTime),
                ("ThuLunchEndTime", x.ThuLunchEndTime),
                ("ThuExitTime", x.ThuExitTime),
                ("FriEntryTime", x.FriEntryTime),
                ("FriToleranceMinutes", x.FriToleranceMinutes),
                ("FriLunchStartTime", x.FriLunchStartTime),
                ("FriLunchEndTime", x.FriLunchEndTime),
                ("FriExitTime", x.FriExitTime),
                ("SatEntryTime", x.SatEntryTime),
                ("SatToleranceMinutes", x.SatToleranceMinutes),
                ("SatLunchStartTime", x.SatLunchStartTime),
                ("SatLunchEndTime", x.SatLunchEndTime),
                ("SatExitTime", x.SatExitTime),
                ("SunEntryTime", x.SunEntryTime),
                ("SunToleranceMinutes", x.SunToleranceMinutes),
                ("SunLunchStartTime", x.SunLunchStartTime),
                ("SunLunchEndTime", x.SunLunchEndTime),
                ("SunExitTime", x.SunExitTime),
                ("WeeklyHours", x.WeeklyHours),
                ("IsFlexible", x.IsFlexible),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetTimeClockDevicesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<TimeClockDeviceDto>>("/api/hr/time-clock-devices") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();

        return BuildView(
            "hr-time-clock-devices",
            "Dispositivos reloj checador",
            "Inventario de relojes, biométricos y endpoints de integración por sucursal.",
            "TimeClockDeviceId",
            [
                TextColumn("TimeClockDeviceId", "Device ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                SmartLookupColumn("BranchId", "Sucursal", branches, width: 220),
                TextColumn("Code", "Código", required: true, width: 120),
                TextColumn("Name", "Dispositivo", required: true, width: 220),
                TextColumn("Brand", "Marca", width: 120),
                TextColumn("Model", "Modelo", width: 120),
                TextColumn("SerialNumber", "Serie", width: 150),
                TextColumn("IpAddress", "IP", width: 120),
                TextColumn("ApiUrl", "API / URL", width: 220),
                TextColumn("Location", "Ubicación", width: 180),
                TextColumn("Status", "Estatus", width: 100),
                DateColumn("LastSyncAt", "Última sync", width: 130),
                TextColumn("Notes", "Notas", width: 220),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("TimeClockDeviceId", x.TimeClockDeviceId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Brand", x.Brand),
                ("Model", x.Model),
                ("SerialNumber", x.SerialNumber),
                ("IpAddress", x.IpAddress),
                ("ApiUrl", x.ApiUrl),
                ("Location", x.Location),
                ("Status", x.Status),
                ("LastSyncAt", x.LastSyncAt),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetLeaveTypesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<LeaveTypeDto>>("/api/hr/leave-types") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var payrollConcepts = await GetPayrollConceptLookupsAsync();

        return BuildView(
            "hr-leave-types",
            "Tipos de ausencia",
            "Parámetros de vacaciones, permisos, incapacidad y ausencias con impacto a nómina.",
            "LeaveTypeId",
            [
                TextColumn("LeaveTypeId", "Leave Type ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollConceptId", "Concepto nómina", payrollConcepts, width: 220, quickCreateKey: "payroll-concepts"),
                TextColumn("Code", "Código", required: true, width: 120),
                TextColumn("Name", "Tipo ausencia", required: true, width: 220),
                TextColumn("Category", "Categoría", width: 120),
                BoolColumn("WithPay", "Con goce", width: 90),
                BoolColumn("ImpactsPayroll", "Impacta nómina", width: 110),
                NumberColumn("DefaultDays", "Días default", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("LeaveTypeId", x.LeaveTypeId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollConceptId", x.PayrollConceptId?.ToString("D")),
                ("Code", x.Code),
                ("Name", x.Name),
                ("Category", x.Category),
                ("WithPay", x.WithPay),
                ("ImpactsPayroll", x.ImpactsPayroll),
                ("DefaultDays", x.DefaultDays),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList(),
            allowImport: true);
    }

    private async Task<CatalogViewDefinition> GetVacationRequestsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<VacationRequestDto>>("/api/hr/vacation-requests") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var leaveTypes = await GetLeaveTypeLookupsAsync();

        return BuildView(
            "hr-vacation-requests",
            "Vacaciones y permisos",
            "Solicitudes enterprise para vacaciones, permisos y ausencias con flujo RH.",
            "VacationRequestId",
            [
                TextColumn("VacationRequestId", "Vacation Request ID", allowEditing: false, width: 220),
                SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 220),
                LookupColumn("LeaveTypeId", "Tipo ausencia", leaveTypes, width: 220, quickCreateKey: "hr-leave-types"),
                TextColumn("Folio", "Folio", width: 140),
                DateColumn("RequestDate", "Solicitud", required: true, width: 120),
                DateColumn("StartDate", "Inicio", required: true, width: 120),
                DateColumn("EndDate", "Fin", required: true, width: 120),
                DateColumn("ReturnDate", "Regreso", width: 120),
                NumberColumn("RequestedDays", "Días solicitados", width: 120),
                NumberColumn("ApprovedDays", "Días aprobados", width: 120),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("ApprovedBy", "Autorizó", width: 160),
                DateColumn("ApprovedAt", "Fecha autorización", width: 130),
                TextColumn("Notes", "Notas", width: 240),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("VacationRequestId", x.VacationRequestId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("LeaveTypeId", x.LeaveTypeId?.ToString("D")),
                ("Folio", x.Folio),
                ("RequestDate", x.RequestDate),
                ("StartDate", x.StartDate),
                ("EndDate", x.EndDate),
                ("ReturnDate", x.ReturnDate),
                ("RequestedDays", x.RequestedDays),
                ("ApprovedDays", x.ApprovedDays),
                ("Status", x.Status),
                ("ApprovedBy", x.ApprovedBy),
                ("ApprovedAt", x.ApprovedAt),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }


private async Task<CatalogViewDefinition> GetEmployeeDocumentsAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<EmployeeDocumentRecordDto>>("/api/hr/employee-documents") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var employees = await GetEmployeeLookupsAsync();

    return BuildView(
        "hr-employee-documents",
        "Documentos de colaboradores",
        "Expediente digital enterprise para documentos laborales, legales y de cumplimiento.",
        "EmployeeDocumentRecordId",
        [
            TextColumn("EmployeeDocumentRecordId", "Document ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
            LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 220),
            TextColumn("DocumentCode", "Código", required: true, width: 120),
            TextColumn("DocumentName", "Documento", required: true, width: 220),
            TextColumn("DocumentType", "Tipo", width: 130),
            TextColumn("DocumentNumber", "Número", width: 140),
            DateColumn("IssueDate", "Emisión", width: 120),
            DateColumn("ExpirationDate", "Vencimiento", width: 120),
            DateColumn("UploadedAt", "Carga", width: 120),
            TextColumn("FileName", "Archivo", width: 180),
            TextColumn("FilePath", "Ruta", width: 220),
            TextColumn("Status", "Estatus", width: 110),
            BoolColumn("IsRequired", "Obligatorio", width: 100),
            BoolColumn("IsVerified", "Verificado", width: 100),
            TextColumn("VerifiedBy", "Verificó", width: 150),
            TextColumn("Notes", "Notas", width: 240),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("EmployeeDocumentRecordId", x.EmployeeDocumentRecordId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("EmployeeId", x.EmployeeId?.ToString("D")),
            ("DocumentCode", x.DocumentCode),
            ("DocumentName", x.DocumentName),
            ("DocumentType", x.DocumentType),
            ("DocumentNumber", x.DocumentNumber),
            ("IssueDate", x.IssueDate),
            ("ExpirationDate", x.ExpirationDate),
            ("UploadedAt", x.UploadedAt),
            ("FileName", x.FileName),
            ("FilePath", x.FilePath),
            ("Status", x.Status),
            ("IsRequired", x.IsRequired),
            ("IsVerified", x.IsVerified),
            ("VerifiedBy", x.VerifiedBy),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}

private async Task<CatalogViewDefinition> GetEmployeeMovementsAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<EmployeeLaborMovementDto>>("/api/hr/employee-movements") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var employees = await GetEmployeeLookupsAsync();
    var departments = await GetDepartmentLookupsAsync();
    var positions = await GetPositionLookupsAsync();

    return BuildView(
        "hr-employee-movements",
        "Movimientos laborales",
        "Histórico enterprise de altas, bajas, reingresos, promociones y cambios con impacto laboral.",
        "EmployeeLaborMovementId",
        [
            TextColumn("EmployeeLaborMovementId", "Movement ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
            LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 220),
            LookupColumn("DepartmentId", "Departamento", departments, width: 180, quickCreateKey: "hr-departments"),
            LookupColumn("PositionId", "Puesto", positions, width: 180, quickCreateKey: "hr-positions"),
            TextColumn("MovementCode", "Código", required: true, width: 120),
            TextColumn("MovementType", "Tipo", required: true, width: 140),
            DateColumn("EffectiveDate", "Efectivo", required: true, width: 120),
            DateColumn("AppliedAt", "Aplicado", width: 120),
            TextColumn("PreviousValue", "Anterior", width: 160),
            TextColumn("NewValue", "Nuevo", width: 160),
            NumberColumn("SalaryBefore", "Salario ant", width: 120),
            NumberColumn("SalaryAfter", "Salario nuevo", width: 120),
            TextColumn("AuthorizedBy", "Autorizó", width: 150),
            TextColumn("Status", "Estatus", width: 110),
            BoolColumn("ImpactsPayroll", "Impacta nómina", width: 110),
            TextColumn("Notes", "Notas", width: 220),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("EmployeeLaborMovementId", x.EmployeeLaborMovementId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("EmployeeId", x.EmployeeId?.ToString("D")),
            ("DepartmentId", x.DepartmentId?.ToString("D")),
            ("PositionId", x.PositionId?.ToString("D")),
            ("MovementCode", x.MovementCode),
            ("MovementType", x.MovementType),
            ("EffectiveDate", x.EffectiveDate),
            ("AppliedAt", x.AppliedAt),
            ("PreviousValue", x.PreviousValue),
            ("NewValue", x.NewValue),
            ("SalaryBefore", x.SalaryBefore),
            ("SalaryAfter", x.SalaryAfter),
            ("AuthorizedBy", x.AuthorizedBy),
            ("Status", x.Status),
            ("ImpactsPayroll", x.ImpactsPayroll),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}

private async Task<CatalogViewDefinition> GetEmployeeCertificationsAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<EmployeeCertificationRecordDto>>("/api/hr/employee-certifications") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var employees = await GetEmployeeLookupsAsync();

    return BuildView(
        "hr-employee-certifications",
        "Certificaciones y formación",
        "Control enterprise de certificaciones, entrenamientos y vencimientos por colaborador.",
        "EmployeeCertificationRecordId",
        [
            TextColumn("EmployeeCertificationRecordId", "Certification ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 180),
            LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 220),
            TextColumn("CertificationCode", "Código", required: true, width: 120),
            TextColumn("CertificationName", "Certificación", required: true, width: 220),
            TextColumn("Category", "Categoría", width: 130),
            TextColumn("IssuedBy", "Emitida por", width: 160),
            DateColumn("IssueDate", "Emisión", required: true, width: 120),
            DateColumn("ExpirationDate", "Vencimiento", width: 120),
            NumberColumn("Score", "Calificación", width: 110),
            TextColumn("Status", "Estatus", width: 110),
            BoolColumn("IsMandatory", "Obligatoria", width: 100),
            BoolColumn("RenewalRequired", "Renovable", width: 100),
            TextColumn("Notes", "Notas", width: 240),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("EmployeeCertificationRecordId", x.EmployeeCertificationRecordId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("EmployeeId", x.EmployeeId?.ToString("D")),
            ("CertificationCode", x.CertificationCode),
            ("CertificationName", x.CertificationName),
            ("Category", x.Category),
            ("IssuedBy", x.IssuedBy),
            ("IssueDate", x.IssueDate),
            ("ExpirationDate", x.ExpirationDate),
            ("Score", x.Score),
            ("Status", x.Status),
            ("IsMandatory", x.IsMandatory),
            ("RenewalRequired", x.RenewalRequired),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}

private async Task<CatalogViewDefinition> GetRecruitmentVacanciesAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<RecruitmentVacancyDto>>("/api/hr/recruitment-vacancies") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var departments = await GetDepartmentLookupsAsync();
    var positions = await GetPositionLookupsAsync();

    return BuildView(
        "hr-recruitment-vacancies",
        "Vacantes",
        "Planeación enterprise de vacantes, headcount y pipeline de contratación.",
        "RecruitmentVacancyId",
        [
            TextColumn("RecruitmentVacancyId", "Vacancy ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 220),
            LookupColumn("DepartmentId", "Departamento", departments, width: 220, quickCreateKey: "hr-departments"),
            LookupColumn("PositionId", "Puesto", positions, width: 220, quickCreateKey: "hr-positions"),
            TextColumn("VacancyCode", "Código", required: true, width: 130),
            TextColumn("Title", "Vacante", required: true, width: 240),
            TextColumn("EmploymentType", "Tipo empleo", width: 120),
            DateColumn("OpenDate", "Apertura", width: 130),
            DateColumn("CloseDate", "Cierre", width: 130),
            NumberColumn("Headcount", "Plazas", width: 90),
            NumberColumn("SalaryMin", "Salario min", width: 110),
            NumberColumn("SalaryMax", "Salario max", width: 110),
            TextColumn("HiringManager", "Hiring manager", width: 180),
            TextColumn("Priority", "Prioridad", width: 100),
            TextColumn("Status", "Estatus", width: 100),
            TextColumn("Notes", "Notas", width: 260),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("RecruitmentVacancyId", x.RecruitmentVacancyId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("DepartmentId", x.DepartmentId?.ToString("D")),
            ("PositionId", x.PositionId?.ToString("D")),
            ("VacancyCode", x.VacancyCode),
            ("Title", x.Title),
            ("EmploymentType", x.EmploymentType),
            ("OpenDate", x.OpenDate),
            ("CloseDate", x.CloseDate),
            ("Headcount", x.Headcount),
            ("SalaryMin", x.SalaryMin),
            ("SalaryMax", x.SalaryMax),
            ("HiringManager", x.HiringManager),
            ("Priority", x.Priority),
            ("Status", x.Status),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}

private async Task<CatalogViewDefinition> GetCandidateApplicationsAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<CandidateApplicationDto>>("/api/hr/candidate-applications") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var vacancies = await GetRecruitmentVacancyLookupsAsync();
    var employees = await GetEmployeeLookupsAsync();

    return BuildView(
        "hr-candidate-applications",
        "Candidatos",
        "Pipeline enterprise de candidatos por vacante, fuente y etapa.",
        "CandidateApplicationId",
        [
            TextColumn("CandidateApplicationId", "Candidate App ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 220),
            LookupColumn("RecruitmentVacancyId", "Vacante", vacancies, required: true, width: 240),
            LookupColumn("HiredEmployeeId", "Empleado contratado", employees, width: 240),
            TextColumn("CandidateCode", "Código", required: true, width: 130),
            TextColumn("FullName", "Candidato", required: true, width: 220),
            TextColumn("Email", "Correo", width: 220),
            TextColumn("Phone", "Teléfono", width: 140),
            TextColumn("Source", "Fuente", width: 110),
            DateColumn("AppliedAt", "Aplicó", width: 130),
            TextColumn("Stage", "Etapa", width: 110),
            NumberColumn("Score", "Score", width: 90),
            NumberColumn("OfferAmount", "Oferta", width: 110),
            TextColumn("CvFileName", "Archivo CV", width: 180),
            TextColumn("CvFilePath", "Ruta CV", width: 220),
            TextColumn("Status", "Estatus", width: 100),
            TextColumn("Notes", "Notas", width: 260),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("CandidateApplicationId", x.CandidateApplicationId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("RecruitmentVacancyId", x.RecruitmentVacancyId?.ToString("D")),
            ("HiredEmployeeId", x.HiredEmployeeId?.ToString("D")),
            ("CandidateCode", x.CandidateCode),
            ("FullName", x.FullName),
            ("Email", x.Email),
            ("Phone", x.Phone),
            ("Source", x.Source),
            ("AppliedAt", x.AppliedAt),
            ("Stage", x.Stage),
            ("Score", x.Score),
            ("OfferAmount", x.OfferAmount),
            ("CvFileName", x.CvFileName),
            ("CvFilePath", x.CvFilePath),
            ("Status", x.Status),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}

private async Task<CatalogViewDefinition> GetOnboardingChecklistsAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<OnboardingChecklistRecordDto>>("/api/hr/onboarding-checklists") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var employees = await GetEmployeeLookupsAsync();
    var candidates = await GetCandidateApplicationLookupsAsync();

    return BuildView(
        "hr-onboarding-checklists",
        "Onboarding",
        "Checklist de ingreso, asignación de activos y habilitación del colaborador.",
        "OnboardingChecklistRecordId",
        [
            TextColumn("OnboardingChecklistRecordId", "Onboarding ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 220),
            LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
            LookupColumn("CandidateApplicationId", "Candidato", candidates, width: 240),
            TextColumn("ChecklistCode", "Código", required: true, width: 130),
            TextColumn("ChecklistName", "Checklist", required: true, width: 240),
            DateColumn("PlannedDate", "Planeado", width: 130),
            DateColumn("CompletedAt", "Completado", width: 130),
            TextColumn("ResponsibleArea", "Área responsable", width: 140),
            TextColumn("Status", "Estatus", width: 100),
            NumberColumn("CompletionPercent", "% avance", width: 100),
            BoolColumn("AssetsAssigned", "Activos", width: 90),
            BoolColumn("CredentialsIssued", "Credenciales", width: 100),
            BoolColumn("InductionCompleted", "Inducción", width: 100),
            TextColumn("Notes", "Notas", width: 260),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("OnboardingChecklistRecordId", x.OnboardingChecklistRecordId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("EmployeeId", x.EmployeeId?.ToString("D")),
            ("CandidateApplicationId", x.CandidateApplicationId?.ToString("D")),
            ("ChecklistCode", x.ChecklistCode),
            ("ChecklistName", x.ChecklistName),
            ("PlannedDate", x.PlannedDate),
            ("CompletedAt", x.CompletedAt),
            ("ResponsibleArea", x.ResponsibleArea),
            ("Status", x.Status),
            ("CompletionPercent", x.CompletionPercent),
            ("AssetsAssigned", x.AssetsAssigned),
            ("CredentialsIssued", x.CredentialsIssued),
            ("InductionCompleted", x.InductionCompleted),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}


private async Task<CatalogViewDefinition> GetPerformanceReviewsAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<EmployeePerformanceReviewDto>>("/api/hr/performance-reviews") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var employees = await GetEmployeeLookupsAsync();
    var departments = await GetDepartmentLookupsAsync();
    var positions = await GetPositionLookupsAsync();

    return BuildView(
        "hr-performance-reviews",
        "Evaluaciones de desempeño",
        "Ciclos enterprise de evaluación, calibración y cumplimiento de objetivos.",
        "EmployeePerformanceReviewId",
        [
            TextColumn("EmployeePerformanceReviewId", "Review ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 220),
            LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 260),
            LookupColumn("DepartmentId", "Departamento", departments, width: 220, quickCreateKey: "hr-departments"),
            LookupColumn("PositionId", "Puesto", positions, width: 220, quickCreateKey: "hr-positions"),
            TextColumn("ReviewCode", "Código", required: true, width: 130),
            TextColumn("ReviewCycle", "Ciclo", required: true, width: 150),
            DateColumn("PeriodStart", "Inicio", width: 130),
            DateColumn("PeriodEnd", "Fin", width: 130),
            DateColumn("ReviewDate", "Evaluación", width: 130),
            TextColumn("ReviewerName", "Evaluador", width: 180),
            NumberColumn("Score", "Score", width: 100),
            NumberColumn("CalibrationScore", "Calibrado", width: 110),
            NumberColumn("GoalCompletionPercent", "% metas", width: 110),
            TextColumn("PotentialLevel", "Potencial", width: 110),
            TextColumn("Status", "Estatus", width: 110),
            TextColumn("Notes", "Notas", width: 280),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("EmployeePerformanceReviewId", x.EmployeePerformanceReviewId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("EmployeeId", x.EmployeeId?.ToString("D")),
            ("DepartmentId", x.DepartmentId?.ToString("D")),
            ("PositionId", x.PositionId?.ToString("D")),
            ("ReviewCode", x.ReviewCode),
            ("ReviewCycle", x.ReviewCycle),
            ("PeriodStart", x.PeriodStart),
            ("PeriodEnd", x.PeriodEnd),
            ("ReviewDate", x.ReviewDate),
            ("ReviewerName", x.ReviewerName),
            ("Score", x.Score),
            ("CalibrationScore", x.CalibrationScore),
            ("GoalCompletionPercent", x.GoalCompletionPercent),
            ("PotentialLevel", x.PotentialLevel),
            ("Status", x.Status),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}

private async Task<CatalogViewDefinition> GetCompetencyAssessmentsAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<EmployeeCompetencyAssessmentDto>>("/api/hr/competency-assessments") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var employees = await GetEmployeeLookupsAsync();
    var departments = await GetDepartmentLookupsAsync();
    var positions = await GetPositionLookupsAsync();

    return BuildView(
        "hr-competency-assessments",
        "Evaluaciones de competencias",
        "Matriz enterprise de competencias esperadas, nivel logrado y brechas de desarrollo.",
        "EmployeeCompetencyAssessmentId",
        [
            TextColumn("EmployeeCompetencyAssessmentId", "Assessment ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 220),
            LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 260),
            LookupColumn("DepartmentId", "Departamento", departments, width: 220, quickCreateKey: "hr-departments"),
            LookupColumn("PositionId", "Puesto", positions, width: 220, quickCreateKey: "hr-positions"),
            TextColumn("AssessmentCode", "Código", required: true, width: 130),
            TextColumn("CompetencyCode", "Comp. código", required: true, width: 130),
            TextColumn("CompetencyName", "Competencia", required: true, width: 220),
            NumberColumn("ExpectedLevel", "Esperado", width: 100),
            NumberColumn("AchievedLevel", "Logrado", width: 100),
            NumberColumn("GapLevel", "Brecha", width: 90),
            DateColumn("AssessedAt", "Evaluado", width: 130),
            TextColumn("AssessorName", "Evaluador", width: 180),
            TextColumn("DevelopmentAction", "Acción desarrollo", width: 240),
            TextColumn("Status", "Estatus", width: 110),
            TextColumn("Notes", "Notas", width: 280),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("EmployeeCompetencyAssessmentId", x.EmployeeCompetencyAssessmentId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("EmployeeId", x.EmployeeId?.ToString("D")),
            ("DepartmentId", x.DepartmentId?.ToString("D")),
            ("PositionId", x.PositionId?.ToString("D")),
            ("AssessmentCode", x.AssessmentCode),
            ("CompetencyCode", x.CompetencyCode),
            ("CompetencyName", x.CompetencyName),
            ("ExpectedLevel", x.ExpectedLevel),
            ("AchievedLevel", x.AchievedLevel),
            ("GapLevel", x.GapLevel),
            ("AssessedAt", x.AssessedAt),
            ("AssessorName", x.AssessorName),
            ("DevelopmentAction", x.DevelopmentAction),
            ("Status", x.Status),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}

private async Task<CatalogViewDefinition> GetSuccessionPlansAsync()
{
    var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
    var rows = await client.GetFromJsonAsync<List<SuccessionPlanRecordDto>>("/api/hr/succession-plans") ?? [];
    var companies = await GetCompanyLookupsAsync();
    var branches = await GetBranchLookupsAsync();
    var employees = await GetEmployeeLookupsAsync();
    var positions = await GetPositionLookupsAsync();

    return BuildView(
        "hr-succession-plans",
        "Planes de sucesión",
        "Mapa enterprise de sucesores, criticidad, riesgo y preparación del talento clave.",
        "SuccessionPlanRecordId",
        [
            TextColumn("SuccessionPlanRecordId", "Plan ID", allowEditing: false, width: 220),
            SmartLookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
            SmartLookupColumn("BranchId", "Sucursal", branches, width: 220),
            LookupColumn("PositionId", "Puesto objetivo", positions, required: true, width: 220, quickCreateKey: "hr-positions"),
            LookupColumn("IncumbentEmployeeId", "Titular actual", employees, width: 260),
            LookupColumn("SuccessorEmployeeId", "Sucesor", employees, required: true, width: 260),
            TextColumn("PlanCode", "Código", required: true, width: 130),
            TextColumn("Criticality", "Criticidad", width: 110),
            TextColumn("ReadinessLevel", "Readiness", width: 110),
            TextColumn("RiskOfLoss", "Riesgo salida", width: 120),
            DateColumn("ReviewDate", "Revisión", width: 130),
            DateColumn("TargetReadyDate", "Meta listo", width: 130),
            BoolColumn("IsNominationApproved", "Aprobado", width: 90),
            TextColumn("DevelopmentPlan", "Plan desarrollo", width: 240),
            TextColumn("Status", "Estatus", width: 110),
            TextColumn("Notes", "Notas", width: 280),
            BoolColumn("IsActive", "Activo", width: 90)
        ],
        rows.Select(x => Row(
            ("SuccessionPlanRecordId", x.SuccessionPlanRecordId.ToString("D")),
            ("CompanyId", x.CompanyId?.ToString("D")),
            ("BranchId", x.BranchId?.ToString("D")),
            ("PositionId", x.PositionId?.ToString("D")),
            ("IncumbentEmployeeId", x.IncumbentEmployeeId?.ToString("D")),
            ("SuccessorEmployeeId", x.SuccessorEmployeeId?.ToString("D")),
            ("PlanCode", x.PlanCode),
            ("Criticality", x.Criticality),
            ("ReadinessLevel", x.ReadinessLevel),
            ("RiskOfLoss", x.RiskOfLoss),
            ("ReviewDate", x.ReviewDate),
            ("TargetReadyDate", x.TargetReadyDate),
            ("IsNominationApproved", x.IsNominationApproved),
            ("DevelopmentPlan", x.DevelopmentPlan),
            ("Status", x.Status),
            ("Notes", x.Notes),
            ("IsActive", x.IsActive)))
        .ToList());
}

    private async Task<List<CatalogLookupItem>> GetCompanyLookupsAsync()
    {
        var tenantId = _appState.CurrentTenantId ?? _authState.TenantId;
        var companyId = _appState.CurrentCompanyId ?? _authState.CompanyId;

        if (!tenantId.HasValue && !companyId.HasValue)
            return [];

        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CompanyLookupDto>>("/api/organization/companies") ?? [];

        if (tenantId.HasValue)
            rows = rows.Where(x => x.TenantId == tenantId.Value).ToList();
        else if (companyId.HasValue)
            rows = rows.Where(x => x.CompanyId == companyId.Value).ToList();

        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem { Id = x.CompanyId.ToString("D"), Name = x.Name }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetBranchLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BranchLookupDto>>("/api/organization/branches") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.BranchId.ToString("D"), Name = x.Name }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetEmployeeLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<EmployeeLookupDto>>("/api/hr/employees") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.EmployeeId.ToString("D"), Name = string.IsNullOrWhiteSpace(x.FullName) ? x.EmployeeNumber : $"{x.EmployeeNumber} · {x.FullName}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPayrollConceptLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollConceptLookupDto>>("/api/payroll/concepts") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.PayrollConceptId.ToString("D"), Name = string.IsNullOrWhiteSpace(x.Name) ? x.Code : $"{x.Code} · {x.Name}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetShiftLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<WorkShiftDto>>("/api/hr/shifts") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.WorkShiftId.ToString("D"), Name = $"{x.Code} · {x.Name}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetLeaveTypeLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<LeaveTypeDto>>("/api/hr/leave-types") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.LeaveTypeId.ToString("D"), Name = $"{x.Code} · {x.Name}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetDepartmentLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<DepartmentLookupDto>>("/api/hr/departments") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.DepartmentId.ToString("D"), Name = x.Name }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPositionLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PositionLookupDto>>("/api/hr/positions") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.PositionId.ToString("D"), Name = x.Name }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetRecruitmentVacancyLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<RecruitmentVacancyDto>>("/api/hr/recruitment-vacancies") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.RecruitmentVacancyId.ToString("D"), Name = $"{x.VacancyCode} · {x.Title}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetCandidateApplicationLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CandidateApplicationDto>>("/api/hr/candidate-applications") ?? [];
        return rows.Where(x => x.IsActive).Select(x => new CatalogLookupItem { Id = x.CandidateApplicationId.ToString("D"), Name = $"{x.CandidateCode} · {x.FullName}" }).ToList();
    }

    private static WorkShiftRequest MapShiftRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        StartTime = ReadString(payload, "StartTime"),
        EndTime = ReadString(payload, "EndTime"),
        BreakMinutes = ReadInt(payload, "BreakMinutes"),
        ToleranceMinutes = ReadInt(payload, "ToleranceMinutes"),
        IsOvernight = ReadBool(payload, "IsOvernight"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static WorkScheduleRequest MapWorkScheduleRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        WorkShiftId = ReadGuid(payload, "WorkShiftId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Monday = ReadBool(payload, "Monday"),
        Tuesday = ReadBool(payload, "Tuesday"),
        Wednesday = ReadBool(payload, "Wednesday"),
        Thursday = ReadBool(payload, "Thursday"),
        Friday = ReadBool(payload, "Friday"),
        Saturday = ReadBool(payload, "Saturday"),
        Sunday = ReadBool(payload, "Sunday"),
        MonEntryTime = ReadString(payload, "MonEntryTime"),
        MonToleranceMinutes = ReadInt(payload, "MonToleranceMinutes"),
        MonLunchStartTime = ReadString(payload, "MonLunchStartTime"),
        MonLunchEndTime = ReadString(payload, "MonLunchEndTime"),
        MonExitTime = ReadString(payload, "MonExitTime"),
        TueEntryTime = ReadString(payload, "TueEntryTime"),
        TueToleranceMinutes = ReadInt(payload, "TueToleranceMinutes"),
        TueLunchStartTime = ReadString(payload, "TueLunchStartTime"),
        TueLunchEndTime = ReadString(payload, "TueLunchEndTime"),
        TueExitTime = ReadString(payload, "TueExitTime"),
        WedEntryTime = ReadString(payload, "WedEntryTime"),
        WedToleranceMinutes = ReadInt(payload, "WedToleranceMinutes"),
        WedLunchStartTime = ReadString(payload, "WedLunchStartTime"),
        WedLunchEndTime = ReadString(payload, "WedLunchEndTime"),
        WedExitTime = ReadString(payload, "WedExitTime"),
        ThuEntryTime = ReadString(payload, "ThuEntryTime"),
        ThuToleranceMinutes = ReadInt(payload, "ThuToleranceMinutes"),
        ThuLunchStartTime = ReadString(payload, "ThuLunchStartTime"),
        ThuLunchEndTime = ReadString(payload, "ThuLunchEndTime"),
        ThuExitTime = ReadString(payload, "ThuExitTime"),
        FriEntryTime = ReadString(payload, "FriEntryTime"),
        FriToleranceMinutes = ReadInt(payload, "FriToleranceMinutes"),
        FriLunchStartTime = ReadString(payload, "FriLunchStartTime"),
        FriLunchEndTime = ReadString(payload, "FriLunchEndTime"),
        FriExitTime = ReadString(payload, "FriExitTime"),
        SatEntryTime = ReadString(payload, "SatEntryTime"),
        SatToleranceMinutes = ReadInt(payload, "SatToleranceMinutes"),
        SatLunchStartTime = ReadString(payload, "SatLunchStartTime"),
        SatLunchEndTime = ReadString(payload, "SatLunchEndTime"),
        SatExitTime = ReadString(payload, "SatExitTime"),
        SunEntryTime = ReadString(payload, "SunEntryTime"),
        SunToleranceMinutes = ReadInt(payload, "SunToleranceMinutes"),
        SunLunchStartTime = ReadString(payload, "SunLunchStartTime"),
        SunLunchEndTime = ReadString(payload, "SunLunchEndTime"),
        SunExitTime = ReadString(payload, "SunExitTime"),
        WeeklyHours = ReadDecimal(payload, "WeeklyHours"),
        IsFlexible = ReadBool(payload, "IsFlexible"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static TimeClockDeviceRequest MapTimeClockDeviceRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Brand = ReadString(payload, "Brand"),
        Model = ReadString(payload, "Model"),
        SerialNumber = ReadString(payload, "SerialNumber"),
        IpAddress = ReadString(payload, "IpAddress"),
        ApiUrl = ReadString(payload, "ApiUrl"),
        Location = ReadString(payload, "Location"),
        Status = ReadString(payload, "Status"),
        LastSyncAt = ReadDate(payload, "LastSyncAt"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static LeaveTypeRequest MapLeaveTypeRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollConceptId = ReadGuid(payload, "PayrollConceptId"),
        Code = ReadString(payload, "Code"),
        Name = ReadString(payload, "Name"),
        Category = ReadString(payload, "Category"),
        WithPay = ReadBool(payload, "WithPay"),
        ImpactsPayroll = ReadBool(payload, "ImpactsPayroll"),
        DefaultDays = ReadDecimal(payload, "DefaultDays"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static VacationRequestRequest MapVacationRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        LeaveTypeId = ReadGuid(payload, "LeaveTypeId"),
        Folio = ReadString(payload, "Folio"),
        RequestDate = ReadDate(payload, "RequestDate"),
        StartDate = ReadDate(payload, "StartDate"),
        EndDate = ReadDate(payload, "EndDate"),
        ReturnDate = ReadDate(payload, "ReturnDate"),
        RequestedDays = ReadDecimal(payload, "RequestedDays"),
        ApprovedDays = ReadDecimal(payload, "ApprovedDays"),
        Status = ReadString(payload, "Status"),
        ApprovedBy = ReadString(payload, "ApprovedBy"),
        ApprovedAt = ReadDate(payload, "ApprovedAt"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };


private static EmployeeDocumentRecordRequest MapEmployeeDocumentRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    EmployeeId = ReadGuid(payload, "EmployeeId"),
    DocumentCode = ReadString(payload, "DocumentCode"),
    DocumentName = ReadString(payload, "DocumentName"),
    DocumentType = ReadString(payload, "DocumentType"),
    DocumentNumber = ReadString(payload, "DocumentNumber"),
    IssueDate = ReadDate(payload, "IssueDate"),
    ExpirationDate = ReadDate(payload, "ExpirationDate"),
    UploadedAt = ReadDate(payload, "UploadedAt"),
    VerifiedAt = ReadDate(payload, "VerifiedAt"),
    FileName = ReadString(payload, "FileName"),
    FilePath = ReadString(payload, "FilePath"),
    Status = ReadString(payload, "Status"),
    IsRequired = ReadBool(payload, "IsRequired"),
    IsVerified = ReadBool(payload, "IsVerified"),
    VerifiedBy = ReadString(payload, "VerifiedBy"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

private static EmployeeLaborMovementRequest MapEmployeeMovementRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    EmployeeId = ReadGuid(payload, "EmployeeId"),
    DepartmentId = ReadGuid(payload, "DepartmentId"),
    PositionId = ReadGuid(payload, "PositionId"),
    MovementCode = ReadString(payload, "MovementCode"),
    MovementType = ReadString(payload, "MovementType"),
    EffectiveDate = ReadDate(payload, "EffectiveDate"),
    AppliedAt = ReadDate(payload, "AppliedAt"),
    PreviousValue = ReadString(payload, "PreviousValue"),
    NewValue = ReadString(payload, "NewValue"),
    SalaryBefore = ReadDecimal(payload, "SalaryBefore"),
    SalaryAfter = ReadDecimal(payload, "SalaryAfter"),
    AuthorizedBy = ReadString(payload, "AuthorizedBy"),
    Status = ReadString(payload, "Status"),
    ImpactsPayroll = ReadBool(payload, "ImpactsPayroll"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

private static EmployeeCertificationRecordRequest MapEmployeeCertificationRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    EmployeeId = ReadGuid(payload, "EmployeeId"),
    CertificationCode = ReadString(payload, "CertificationCode"),
    CertificationName = ReadString(payload, "CertificationName"),
    Category = ReadString(payload, "Category"),
    IssuedBy = ReadString(payload, "IssuedBy"),
    IssueDate = ReadDate(payload, "IssueDate"),
    ExpirationDate = ReadDate(payload, "ExpirationDate"),
    Score = ReadDecimal(payload, "Score"),
    Status = ReadString(payload, "Status"),
    IsMandatory = ReadBool(payload, "IsMandatory"),
    RenewalRequired = ReadBool(payload, "RenewalRequired"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

private static RecruitmentVacancyRequest MapRecruitmentVacancyRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    DepartmentId = ReadGuid(payload, "DepartmentId"),
    PositionId = ReadGuid(payload, "PositionId"),
    VacancyCode = ReadString(payload, "VacancyCode"),
    Title = ReadString(payload, "Title"),
    EmploymentType = ReadString(payload, "EmploymentType"),
    OpenDate = ReadDate(payload, "OpenDate"),
    CloseDate = ReadDate(payload, "CloseDate"),
    Headcount = ReadInt(payload, "Headcount"),
    SalaryMin = ReadDecimal(payload, "SalaryMin"),
    SalaryMax = ReadDecimal(payload, "SalaryMax"),
    HiringManager = ReadString(payload, "HiringManager"),
    Priority = ReadString(payload, "Priority"),
    Status = ReadString(payload, "Status"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

private static CandidateApplicationRequest MapCandidateApplicationRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    RecruitmentVacancyId = ReadGuid(payload, "RecruitmentVacancyId"),
    HiredEmployeeId = ReadGuid(payload, "HiredEmployeeId"),
    CandidateCode = ReadString(payload, "CandidateCode"),
    FullName = ReadString(payload, "FullName"),
    Email = ReadString(payload, "Email"),
    Phone = ReadString(payload, "Phone"),
    Source = ReadString(payload, "Source"),
    AppliedAt = ReadDate(payload, "AppliedAt"),
    Stage = ReadString(payload, "Stage"),
    Score = ReadDecimal(payload, "Score"),
    OfferAmount = ReadDecimal(payload, "OfferAmount"),
    CvFileName = ReadString(payload, "CvFileName"),
    CvFilePath = ReadString(payload, "CvFilePath"),
    Status = ReadString(payload, "Status"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

private static OnboardingChecklistRecordRequest MapOnboardingChecklistRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    EmployeeId = ReadGuid(payload, "EmployeeId"),
    CandidateApplicationId = ReadGuid(payload, "CandidateApplicationId"),
    ChecklistCode = ReadString(payload, "ChecklistCode"),
    ChecklistName = ReadString(payload, "ChecklistName"),
    PlannedDate = ReadDate(payload, "PlannedDate"),
    CompletedAt = ReadDate(payload, "CompletedAt"),
    ResponsibleArea = ReadString(payload, "ResponsibleArea"),
    Status = ReadString(payload, "Status"),
    CompletionPercent = ReadDecimal(payload, "CompletionPercent"),
    AssetsAssigned = ReadBool(payload, "AssetsAssigned"),
    CredentialsIssued = ReadBool(payload, "CredentialsIssued"),
    InductionCompleted = ReadBool(payload, "InductionCompleted"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

    private static CatalogViewDefinition BuildView(string catalogKey, string title, string subtitle, string keyExpr, List<CatalogColumnDefinition> columns, List<Dictionary<string, object?>> rows, bool allowImport = false)
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

    private static CatalogColumnDefinition TextColumn(string field, string caption, bool required = false, bool allowEditing = true, int width = 160, bool visible = true, bool showInGrid = true)
        => new() { DataField = field, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Visible = visible, ShowInGrid = showInGrid, Width = width };

    private static CatalogColumnDefinition TimeColumn(string field, string caption, int width = 100, bool showInGrid = true)
        => new() { DataField = field, Caption = caption, DataType = "time", Width = width, ShowInGrid = showInGrid };

    private static CatalogColumnDefinition NumberColumn(string field, string caption, bool required = false, int width = 120, bool showInGrid = true)
        => new() { DataField = field, Caption = caption, DataType = "number", Required = required, Width = width, ShowInGrid = showInGrid };

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

    private static CatalogColumnDefinition SmartLookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, bool required = false, int width = 180)
        => lookupItems.Count <= 1
            ? new() { DataField = field, Caption = caption, DataType = "string", Visible = false, ShowInGrid = false, AllowEditing = false, Width = width, UseLookup = true, LookupItems = lookupItems }
            : LookupColumn(field, caption, lookupItems, required, width);


private static EmployeePerformanceReviewRequest MapPerformanceReviewRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    EmployeeId = ReadGuid(payload, "EmployeeId"),
    DepartmentId = ReadGuid(payload, "DepartmentId"),
    PositionId = ReadGuid(payload, "PositionId"),
    ReviewCode = ReadString(payload, "ReviewCode"),
    ReviewCycle = ReadString(payload, "ReviewCycle"),
    PeriodStart = ReadDateTime(payload, "PeriodStart"),
    PeriodEnd = ReadDateTime(payload, "PeriodEnd"),
    ReviewDate = ReadDateTime(payload, "ReviewDate"),
    ReviewerName = ReadString(payload, "ReviewerName"),
    Score = ReadDecimal(payload, "Score"),
    CalibrationScore = ReadDecimal(payload, "CalibrationScore"),
    GoalCompletionPercent = ReadDecimal(payload, "GoalCompletionPercent"),
    PotentialLevel = ReadString(payload, "PotentialLevel"),
    Status = ReadString(payload, "Status"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

private static EmployeeCompetencyAssessmentRequest MapCompetencyAssessmentRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    EmployeeId = ReadGuid(payload, "EmployeeId"),
    DepartmentId = ReadGuid(payload, "DepartmentId"),
    PositionId = ReadGuid(payload, "PositionId"),
    AssessmentCode = ReadString(payload, "AssessmentCode"),
    CompetencyCode = ReadString(payload, "CompetencyCode"),
    CompetencyName = ReadString(payload, "CompetencyName"),
    ExpectedLevel = ReadInt(payload, "ExpectedLevel"),
    AchievedLevel = ReadInt(payload, "AchievedLevel"),
    GapLevel = ReadInt(payload, "GapLevel"),
    AssessedAt = ReadDateTime(payload, "AssessedAt"),
    AssessorName = ReadString(payload, "AssessorName"),
    DevelopmentAction = ReadString(payload, "DevelopmentAction"),
    Status = ReadString(payload, "Status"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

private static SuccessionPlanRecordRequest MapSuccessionPlanRequest(JsonElement payload) => new()
{
    CompanyId = ReadGuid(payload, "CompanyId"),
    BranchId = ReadGuid(payload, "BranchId"),
    PositionId = ReadGuid(payload, "PositionId"),
    IncumbentEmployeeId = ReadGuid(payload, "IncumbentEmployeeId"),
    SuccessorEmployeeId = ReadGuid(payload, "SuccessorEmployeeId"),
    PlanCode = ReadString(payload, "PlanCode"),
    Criticality = ReadString(payload, "Criticality"),
    ReadinessLevel = ReadString(payload, "ReadinessLevel"),
    RiskOfLoss = ReadString(payload, "RiskOfLoss"),
    ReviewDate = ReadDateTime(payload, "ReviewDate"),
    TargetReadyDate = ReadDateTime(payload, "TargetReadyDate"),
    IsNominationApproved = ReadBool(payload, "IsNominationApproved"),
    DevelopmentPlan = ReadString(payload, "DevelopmentPlan"),
    Status = ReadString(payload, "Status"),
    Notes = ReadString(payload, "Notes"),
    IsActive = ReadBool(payload, "IsActive", true)
};

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

    private static DateTime? ReadDateTime(JsonElement payload, string name)
    {
        return ReadDate(payload, name);
    }

    private static DateTime? ReadDate(JsonElement payload, string name)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String when DateTime.TryParse(value.GetString(), out var parsed) => parsed,
            _ when value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var epoch) => DateTimeOffset.FromUnixTimeMilliseconds(epoch).UtcDateTime,
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

    private sealed class CompanyLookupDto
    {
        public Guid CompanyId { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class BranchLookupDto
    {
        public Guid BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class EmployeeLookupDto
    {
        public Guid EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PayrollConceptLookupDto
    {
        public Guid PayrollConceptId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
private sealed class DepartmentLookupDto
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

private sealed class PositionLookupDto
{
    public Guid PositionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

}


public sealed class WorkShiftDto
{
    public Guid WorkShiftId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int BreakMinutes { get; set; }
    public int ToleranceMinutes { get; set; }
    public bool IsOvernight { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class WorkScheduleDto
{
    public Guid WorkScheduleId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? WorkShiftId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public string MonEntryTime { get; set; } = string.Empty;
    public int MonToleranceMinutes { get; set; }
    public string MonLunchStartTime { get; set; } = string.Empty;
    public string MonLunchEndTime { get; set; } = string.Empty;
    public string MonExitTime { get; set; } = string.Empty;
    public string TueEntryTime { get; set; } = string.Empty;
    public int TueToleranceMinutes { get; set; }
    public string TueLunchStartTime { get; set; } = string.Empty;
    public string TueLunchEndTime { get; set; } = string.Empty;
    public string TueExitTime { get; set; } = string.Empty;
    public string WedEntryTime { get; set; } = string.Empty;
    public int WedToleranceMinutes { get; set; }
    public string WedLunchStartTime { get; set; } = string.Empty;
    public string WedLunchEndTime { get; set; } = string.Empty;
    public string WedExitTime { get; set; } = string.Empty;
    public string ThuEntryTime { get; set; } = string.Empty;
    public int ThuToleranceMinutes { get; set; }
    public string ThuLunchStartTime { get; set; } = string.Empty;
    public string ThuLunchEndTime { get; set; } = string.Empty;
    public string ThuExitTime { get; set; } = string.Empty;
    public string FriEntryTime { get; set; } = string.Empty;
    public int FriToleranceMinutes { get; set; }
    public string FriLunchStartTime { get; set; } = string.Empty;
    public string FriLunchEndTime { get; set; } = string.Empty;
    public string FriExitTime { get; set; } = string.Empty;
    public string SatEntryTime { get; set; } = string.Empty;
    public int SatToleranceMinutes { get; set; }
    public string SatLunchStartTime { get; set; } = string.Empty;
    public string SatLunchEndTime { get; set; } = string.Empty;
    public string SatExitTime { get; set; } = string.Empty;
    public string SunEntryTime { get; set; } = string.Empty;
    public int SunToleranceMinutes { get; set; }
    public string SunLunchStartTime { get; set; } = string.Empty;
    public string SunLunchEndTime { get; set; } = string.Empty;
    public string SunExitTime { get; set; } = string.Empty;
    public decimal WeeklyHours { get; set; }
    public bool IsFlexible { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class TimeClockDeviceDto
{
    public Guid TimeClockDeviceId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class LeaveTypeDto
{
    public Guid LeaveTypeId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool WithPay { get; set; }
    public bool ImpactsPayroll { get; set; }
    public decimal DefaultDays { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class VacationRequestDto
{
    public Guid VacationRequestId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime? RequestDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public decimal RequestedDays { get; set; }
    public decimal ApprovedDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}


public sealed class WorkShiftRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public int ToleranceMinutes { get; set; }
    public bool IsOvernight { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class WorkScheduleRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? WorkShiftId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public string? MonEntryTime { get; set; }
    public int MonToleranceMinutes { get; set; }
    public string? MonLunchStartTime { get; set; }
    public string? MonLunchEndTime { get; set; }
    public string? MonExitTime { get; set; }
    public string? TueEntryTime { get; set; }
    public int TueToleranceMinutes { get; set; }
    public string? TueLunchStartTime { get; set; }
    public string? TueLunchEndTime { get; set; }
    public string? TueExitTime { get; set; }
    public string? WedEntryTime { get; set; }
    public int WedToleranceMinutes { get; set; }
    public string? WedLunchStartTime { get; set; }
    public string? WedLunchEndTime { get; set; }
    public string? WedExitTime { get; set; }
    public string? ThuEntryTime { get; set; }
    public int ThuToleranceMinutes { get; set; }
    public string? ThuLunchStartTime { get; set; }
    public string? ThuLunchEndTime { get; set; }
    public string? ThuExitTime { get; set; }
    public string? FriEntryTime { get; set; }
    public int FriToleranceMinutes { get; set; }
    public string? FriLunchStartTime { get; set; }
    public string? FriLunchEndTime { get; set; }
    public string? FriExitTime { get; set; }
    public string? SatEntryTime { get; set; }
    public int SatToleranceMinutes { get; set; }
    public string? SatLunchStartTime { get; set; }
    public string? SatLunchEndTime { get; set; }
    public string? SatExitTime { get; set; }
    public string? SunEntryTime { get; set; }
    public int SunToleranceMinutes { get; set; }
    public string? SunLunchStartTime { get; set; }
    public string? SunLunchEndTime { get; set; }
    public string? SunExitTime { get; set; }
    public decimal WeeklyHours { get; set; }
    public bool IsFlexible { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TimeClockDeviceRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? IpAddress { get; set; }
    public string? ApiUrl { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class LeaveTypeRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PayrollConceptId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public bool WithPay { get; set; }
    public bool ImpactsPayroll { get; set; }
    public decimal DefaultDays { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VacationRequestRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public string? Folio { get; set; }
    public DateTime? RequestDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public decimal RequestedDays { get; set; }
    public decimal ApprovedDays { get; set; }
    public string? Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeDocumentRecordDto
{
    public Guid EmployeeDocumentRecordId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? UploadedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsVerified { get; set; }
    public string VerifiedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeLaborMovementDto
{
    public Guid EmployeeLaborMovementId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string MovementCode { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public decimal SalaryBefore { get; set; }
    public decimal SalaryAfter { get; set; }
    public string AuthorizedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool ImpactsPayroll { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeCertificationRecordDto
{
    public Guid EmployeeCertificationRecordId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string CertificationCode { get; set; } = string.Empty;
    public string CertificationName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool RenewalRequired { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeDocumentRecordRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? DocumentCode { get; set; }
    public string? DocumentName { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? UploadedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? Status { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVerified { get; set; }
    public string? VerifiedBy { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeLaborMovementRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? MovementCode { get; set; }
    public string? MovementType { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public decimal SalaryBefore { get; set; }
    public decimal SalaryAfter { get; set; }
    public string? AuthorizedBy { get; set; }
    public string? Status { get; set; }
    public bool ImpactsPayroll { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeCertificationRecordRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? CertificationCode { get; set; }
    public string? CertificationName { get; set; }
    public string? Category { get; set; }
    public string? IssuedBy { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal Score { get; set; }
    public string? Status { get; set; }
    public bool IsMandatory { get; set; }
    public bool RenewalRequired { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RecruitmentVacancyDto
{
    public Guid RecruitmentVacancyId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string VacancyCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public int Headcount { get; set; }
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public string HiringManager { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CandidateApplicationDto
{
    public Guid CandidateApplicationId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? RecruitmentVacancyId { get; set; }
    public Guid? HiredEmployeeId { get; set; }
    public string CandidateCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }
    public string Stage { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal OfferAmount { get; set; }
    public string CvFileName { get; set; } = string.Empty;
    public string CvFilePath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class OnboardingChecklistRecordDto
{
    public Guid OnboardingChecklistRecordId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? CandidateApplicationId { get; set; }
    public string ChecklistCode { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public DateTime? PlannedDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ResponsibleArea { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CompletionPercent { get; set; }
    public bool AssetsAssigned { get; set; }
    public bool CredentialsIssued { get; set; }
    public bool InductionCompleted { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class RecruitmentVacancyRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? VacancyCode { get; set; }
    public string? Title { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public int Headcount { get; set; }
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public string? HiringManager { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CandidateApplicationRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? RecruitmentVacancyId { get; set; }
    public Guid? HiredEmployeeId { get; set; }
    public string? CandidateCode { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Source { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string? Stage { get; set; }
    public decimal Score { get; set; }
    public decimal OfferAmount { get; set; }
    public string? CvFileName { get; set; }
    public string? CvFilePath { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class OnboardingChecklistRecordRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? CandidateApplicationId { get; set; }
    public string? ChecklistCode { get; set; }
    public string? ChecklistName { get; set; }
    public DateTime? PlannedDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResponsibleArea { get; set; }
    public string? Status { get; set; }
    public decimal CompletionPercent { get; set; }
    public bool AssetsAssigned { get; set; }
    public bool CredentialsIssued { get; set; }
    public bool InductionCompleted { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeePerformanceReviewDto
{
    public Guid EmployeePerformanceReviewId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string ReviewCode { get; set; } = string.Empty;
    public string ReviewCycle { get; set; } = string.Empty;
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal CalibrationScore { get; set; }
    public decimal GoalCompletionPercent { get; set; }
    public string PotentialLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeeCompetencyAssessmentDto
{
    public Guid EmployeeCompetencyAssessmentId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string AssessmentCode { get; set; } = string.Empty;
    public string CompetencyCode { get; set; } = string.Empty;
    public string CompetencyName { get; set; } = string.Empty;
    public int ExpectedLevel { get; set; }
    public int AchievedLevel { get; set; }
    public int GapLevel { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string AssessorName { get; set; } = string.Empty;
    public string DevelopmentAction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class SuccessionPlanRecordDto
{
    public Guid SuccessionPlanRecordId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? IncumbentEmployeeId { get; set; }
    public Guid? SuccessorEmployeeId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string ReadinessLevel { get; set; } = string.Empty;
    public string RiskOfLoss { get; set; } = string.Empty;
    public DateTime? ReviewDate { get; set; }
    public DateTime? TargetReadyDate { get; set; }
    public bool IsNominationApproved { get; set; }
    public string DevelopmentPlan { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class EmployeePerformanceReviewRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? ReviewCode { get; set; }
    public string? ReviewCycle { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string? ReviewerName { get; set; }
    public decimal Score { get; set; }
    public decimal CalibrationScore { get; set; }
    public decimal GoalCompletionPercent { get; set; }
    public string? PotentialLevel { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeCompetencyAssessmentRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? AssessmentCode { get; set; }
    public string? CompetencyCode { get; set; }
    public string? CompetencyName { get; set; }
    public int ExpectedLevel { get; set; }
    public int AchievedLevel { get; set; }
    public int GapLevel { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string? AssessorName { get; set; }
    public string? DevelopmentAction { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SuccessionPlanRecordRequest
{
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? IncumbentEmployeeId { get; set; }
    public Guid? SuccessorEmployeeId { get; set; }
    public string? PlanCode { get; set; }
    public string? Criticality { get; set; }
    public string? ReadinessLevel { get; set; }
    public string? RiskOfLoss { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? TargetReadyDate { get; set; }
    public bool IsNominationApproved { get; set; }
    public string? DevelopmentPlan { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
