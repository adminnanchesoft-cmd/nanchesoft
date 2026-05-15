using System.Net.Http.Json;
using System.Text.Json;
using Nanchesoft.Web.Services.Catalogs;

namespace Nanchesoft.Web.Services.HumanResources;

public sealed class PayrollOperationsApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PayrollOperationsApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CatalogViewDefinition> GetCatalogAsync(string catalogKey)
        => catalogKey.ToLowerInvariant() switch
        {
            "time-clock" => GetTimeClockAsync(),
            "attendance-daily-summaries" => GetAttendanceDailySummariesAsync(),
            "payroll-recurring-movements" => GetRecurringMovementsAsync(),
            "prepayroll-adjustments" => GetPrePayrollAdjustmentsAsync(),
            "prepayroll-cutoffs" => GetPrePayrollCutoffsAsync(),
            "employee-loans" => GetLoansAsync(),
            "employee-loan-deductions" => GetLoanDeductionsAsync(),
            "payroll-source-applications" => GetPayrollSourceApplicationsAsync(),
            "payroll-receipt-control" => GetPayrollReceiptControlAsync(),
            "payroll-run-closings" => GetPayrollRunClosingsAsync(),
            "payroll-dispersion-batches" => GetPayrollDispersionBatchesAsync(),
            "payroll-dispersion-lines" => GetPayrollDispersionLinesAsync(),
            "payroll-accounting-postings" => GetPayrollAccountingPostingsAsync(),
            "payroll-tax-accumulators" => GetPayrollTaxAccumulatorsAsync(),
            "payroll-employer-obligations" => GetPayrollEmployerObligationsAsync(),
            "payroll-fiscal-reconciliations" => GetPayrollFiscalReconciliationsAsync(),
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };

    public async Task<CatalogViewDefinition> InsertAsync(string catalogKey, JsonElement payload)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = catalogKey.ToLowerInvariant() switch
        {
            "time-clock" => await client.PostAsJsonAsync("/api/hr/time-clock", MapAttendancePunchRequest(payload)),
            "attendance-daily-summaries" => await client.PostAsJsonAsync("/api/payroll/attendance-daily-summaries", MapAttendanceDailySummaryRequest(payload)),
            "payroll-recurring-movements" => await client.PostAsJsonAsync("/api/payroll/recurring-movements", MapRecurringMovementRequest(payload)),
            "prepayroll-adjustments" => await client.PostAsJsonAsync("/api/payroll/prepayroll-adjustments", MapPrePayrollAdjustmentRequest(payload)),
            "prepayroll-cutoffs" => await client.PostAsJsonAsync("/api/payroll/prepayroll-cutoffs", MapPrePayrollCutoffRequest(payload)),
            "employee-loans" => await client.PostAsJsonAsync("/api/payroll/loans", MapEmployeeLoanRequest(payload)),
            "employee-loan-deductions" => await client.PostAsJsonAsync("/api/payroll/loan-deductions", MapEmployeeLoanDeductionRequest(payload)),
            "payroll-source-applications" => await client.PostAsJsonAsync("/api/payroll/source-applications", MapPayrollSourceApplicationRequest(payload)),
            "payroll-receipt-control" => await client.PostAsJsonAsync("/api/payroll/receipt-control", MapPayrollReceiptControlRequest(payload)),
            "payroll-run-closings" => await client.PostAsJsonAsync("/api/payroll/run-closings", MapPayrollRunClosingRequest(payload)),
            "payroll-dispersion-batches" => await client.PostAsJsonAsync("/api/payroll/dispersion-batches", MapPayrollDispersionBatchRequest(payload)),
            "payroll-dispersion-lines" => await client.PostAsJsonAsync("/api/payroll/dispersion-lines", MapPayrollDispersionLineRequest(payload)),
            "payroll-accounting-postings" => await client.PostAsJsonAsync("/api/payroll/accounting-postings", MapPayrollAccountingPostingRequest(payload)),
            "payroll-tax-accumulators" => await client.PostAsJsonAsync("/api/payroll/tax-accumulators", MapPayrollTaxAccumulatorRequest(payload)),
            "payroll-employer-obligations" => await client.PostAsJsonAsync("/api/payroll/employer-obligations", MapPayrollEmployerObligationRequest(payload)),
            "payroll-fiscal-reconciliations" => await client.PostAsJsonAsync("/api/payroll/fiscal-reconciliations", MapPayrollFiscalReconciliationRequest(payload)),
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
            "time-clock" => await client.PutAsJsonAsync($"/api/hr/time-clock/{key}", MapAttendancePunchRequest(payload)),
            "attendance-daily-summaries" => await client.PutAsJsonAsync($"/api/payroll/attendance-daily-summaries/{key}", MapAttendanceDailySummaryRequest(payload)),
            "payroll-recurring-movements" => await client.PutAsJsonAsync($"/api/payroll/recurring-movements/{key}", MapRecurringMovementRequest(payload)),
            "prepayroll-adjustments" => await client.PutAsJsonAsync($"/api/payroll/prepayroll-adjustments/{key}", MapPrePayrollAdjustmentRequest(payload)),
            "prepayroll-cutoffs" => await client.PutAsJsonAsync($"/api/payroll/prepayroll-cutoffs/{key}", MapPrePayrollCutoffRequest(payload)),
            "employee-loans" => await client.PutAsJsonAsync($"/api/payroll/loans/{key}", MapEmployeeLoanRequest(payload)),
            "employee-loan-deductions" => await client.PutAsJsonAsync($"/api/payroll/loan-deductions/{key}", MapEmployeeLoanDeductionRequest(payload)),
            "payroll-source-applications" => await client.PutAsJsonAsync($"/api/payroll/source-applications/{key}", MapPayrollSourceApplicationRequest(payload)),
            "payroll-receipt-control" => await client.PutAsJsonAsync($"/api/payroll/receipt-control/{key}", MapPayrollReceiptControlRequest(payload)),
            "payroll-run-closings" => await client.PutAsJsonAsync($"/api/payroll/run-closings/{key}", MapPayrollRunClosingRequest(payload)),
            "payroll-dispersion-batches" => await client.PutAsJsonAsync($"/api/payroll/dispersion-batches/{key}", MapPayrollDispersionBatchRequest(payload)),
            "payroll-dispersion-lines" => await client.PutAsJsonAsync($"/api/payroll/dispersion-lines/{key}", MapPayrollDispersionLineRequest(payload)),
            "payroll-accounting-postings" => await client.PutAsJsonAsync($"/api/payroll/accounting-postings/{key}", MapPayrollAccountingPostingRequest(payload)),
            "payroll-tax-accumulators" => await client.PutAsJsonAsync($"/api/payroll/tax-accumulators/{key}", MapPayrollTaxAccumulatorRequest(payload)),
            "payroll-employer-obligations" => await client.PutAsJsonAsync($"/api/payroll/employer-obligations/{key}", MapPayrollEmployerObligationRequest(payload)),
            "payroll-fiscal-reconciliations" => await client.PutAsJsonAsync($"/api/payroll/fiscal-reconciliations/{key}", MapPayrollFiscalReconciliationRequest(payload)),
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
            "time-clock" => $"/api/hr/time-clock/{key}",
            "attendance-daily-summaries" => $"/api/payroll/attendance-daily-summaries/{key}",
            "payroll-recurring-movements" => $"/api/payroll/recurring-movements/{key}",
            "prepayroll-adjustments" => $"/api/payroll/prepayroll-adjustments/{key}",
            "prepayroll-cutoffs" => $"/api/payroll/prepayroll-cutoffs/{key}",
            "employee-loans" => $"/api/payroll/loans/{key}",
            "employee-loan-deductions" => $"/api/payroll/loan-deductions/{key}",
            "payroll-source-applications" => $"/api/payroll/source-applications/{key}",
            "payroll-receipt-control" => $"/api/payroll/receipt-control/{key}",
            "payroll-run-closings" => $"/api/payroll/run-closings/{key}",
            "payroll-dispersion-batches" => $"/api/payroll/dispersion-batches/{key}",
            "payroll-dispersion-lines" => $"/api/payroll/dispersion-lines/{key}",
            "payroll-accounting-postings" => $"/api/payroll/accounting-postings/{key}",
            "payroll-tax-accumulators" => $"/api/payroll/tax-accumulators/{key}",
            "payroll-employer-obligations" => $"/api/payroll/employer-obligations/{key}",
            "payroll-fiscal-reconciliations" => $"/api/payroll/fiscal-reconciliations/{key}",
            _ => throw new InvalidOperationException($"No se encontró el catálogo '{catalogKey}'.")
        };
        var response = await client.DeleteAsync(endpoint);
        await EnsureSuccessAsync(response);
        return await GetCatalogAsync(catalogKey);
    }

    public async Task ApplyAdvancedSourcesAsync(Guid runId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var response = await client.PostAsync($"/api/payroll/runs/{runId}/apply-advanced-sources", null);
        await EnsureSuccessAsync(response);
    }

    public async Task<List<PayrollReceiptLineRow>> GetPayrollReceiptLinesAsync(Guid runId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetFromJsonAsync<List<PayrollReceiptLineRow>>($"/api/payroll/runs/{runId}/receipt-lines") ?? [];
    }

    public async Task<string> GetPayrollRunPrintHtmlAsync(Guid runId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetStringAsync($"/api/payroll/runs/{runId}/print-html");
    }

    public async Task<string> GetPayrollReceiptPrintHtmlAsync(Guid lineId)
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        return await client.GetStringAsync($"/api/payroll/run-lines/{lineId}/receipt-html");
    }

    private async Task<CatalogViewDefinition> GetTimeClockAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<AttendancePunchDto>>("/api/hr/time-clock") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();

        return BuildView(
            "time-clock",
            "Reloj checador",
            "Marcaciones de entrada y salida por colaborador, listas para análisis e incidencias.",
            "AttendancePunchId",
            [
                TextColumn("AttendancePunchId", "AttendancePunch ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                DateColumn("WorkDate", "Fecha laboral", required: true, width: 120),
                DateColumn("PunchDateTime", "Marcación", required: true, width: 160),
                TextColumn("PunchType", "Tipo", required: true, width: 100),
                TextColumn("Source", "Origen", width: 120),
                TextColumn("DeviceName", "Dispositivo", width: 160),
                TextColumn("DeviceSerial", "Serie", width: 140),
                TextColumn("ExternalReference", "Ref. externa", width: 160),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 220),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("AttendancePunchId", x.AttendancePunchId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("WorkDate", x.WorkDate),
                ("PunchDateTime", x.PunchDateTime),
                ("PunchType", x.PunchType),
                ("Source", x.Source),
                ("DeviceName", x.DeviceName),
                ("DeviceSerial", x.DeviceSerial),
                ("ExternalReference", x.ExternalReference),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetAttendanceDailySummariesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<AttendanceDailySummaryDto>>("/api/payroll/attendance-daily-summaries") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();

        return BuildView(
            "attendance-daily-summaries",
            "Resumen diario de asistencia",
            "Corte diario consolidado de reloj checador, retardos, horas extra y ausencias para prenómina.",
            "AttendanceDailySummaryId",
            [
                TextColumn("AttendanceDailySummaryId", "AttendanceSummary ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("PayrollPeriodId", "Periodo", periods, width: 220),
                DateColumn("WorkDate", "Fecha laboral", required: true, width: 120),
                DateColumn("ScheduledEntryTime", "Entrada prog.", width: 150),
                DateColumn("ScheduledExitTime", "Salida prog.", width: 150),
                DateColumn("FirstPunchDateTime", "Primera marca", width: 150),
                DateColumn("LastPunchDateTime", "Última marca", width: 150),
                NumberColumn("WorkedHours", "Horas", width: 90),
                NumberColumn("DelayMinutes", "Retardo min", width: 95),
                NumberColumn("EarlyLeaveMinutes", "Salida ant. min", width: 110),
                NumberColumn("OvertimeHours", "Horas extra", width: 95),
                NumberColumn("AbsenceUnits", "Ausencia", width: 90),
                TextColumn("DayType", "Tipo día", width: 100),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Source", "Origen", width: 110),
                TextColumn("Notes", "Notas", width: 240),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("AttendanceDailySummaryId", x.AttendanceDailySummaryId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("WorkDate", x.WorkDate),
                ("ScheduledEntryTime", x.ScheduledEntryTime),
                ("ScheduledExitTime", x.ScheduledExitTime),
                ("FirstPunchDateTime", x.FirstPunchDateTime),
                ("LastPunchDateTime", x.LastPunchDateTime),
                ("WorkedHours", x.WorkedHours),
                ("DelayMinutes", x.DelayMinutes),
                ("EarlyLeaveMinutes", x.EarlyLeaveMinutes),
                ("OvertimeHours", x.OvertimeHours),
                ("AbsenceUnits", x.AbsenceUnits),
                ("DayType", x.DayType),
                ("Status", x.Status),
                ("Source", x.Source),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetRecurringMovementsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollRecurringMovementDto>>("/api/payroll/recurring-movements") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var concepts = await GetPayrollConceptLookupsAsync();

        return BuildView(
            "payroll-recurring-movements",
            "Movimientos periódicos programados",
            "Percepciones y deducciones automáticas por empleado para cada corrida de nómina.",
            "PayrollRecurringMovementId",
            [
                TextColumn("PayrollRecurringMovementId", "RecurringMovement ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("PayrollConceptId", "Concepto nómina", concepts, required: true, width: 220),
                TextColumn("MovementCode", "Código", required: true, width: 130),
                TextColumn("MovementName", "Movimiento", required: true, width: 220),
                TextColumn("MovementType", "Tipo", required: true, width: 100),
                TextColumn("CalculationMode", "Cálculo", required: true, width: 120),
                NumberColumn("Quantity", "Cantidad", width: 100),
                NumberColumn("Amount", "Importe", width: 110),
                NumberColumn("Percentage", "%", width: 90),
                DateColumn("EffectiveStartDate", "Desde", required: true, width: 120),
                DateColumn("EffectiveEndDate", "Hasta", width: 120),
                BoolColumn("ApplyEveryRun", "Cada corrida", width: 100),
                NumberColumn("DayOfPeriod", "Día periodo", width: 100),
                BoolColumn("IsProrated", "Prorratea", width: 100),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 220),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollRecurringMovementId", x.PayrollRecurringMovementId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("PayrollConceptId", x.PayrollConceptId?.ToString("D")),
                ("MovementCode", x.MovementCode),
                ("MovementName", x.MovementName),
                ("MovementType", x.MovementType),
                ("CalculationMode", x.CalculationMode),
                ("Quantity", x.Quantity),
                ("Amount", x.Amount),
                ("Percentage", x.Percentage),
                ("EffectiveStartDate", x.EffectiveStartDate),
                ("EffectiveEndDate", x.EffectiveEndDate),
                ("ApplyEveryRun", x.ApplyEveryRun),
                ("DayOfPeriod", x.DayOfPeriod),
                ("IsProrated", x.IsProrated),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPrePayrollAdjustmentsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PrePayrollAdjustmentDto>>("/api/payroll/prepayroll-adjustments") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();
        var concepts = await GetPayrollConceptLookupsAsync();

        return BuildView(
            "prepayroll-adjustments",
            "Ajustes de prenómina",
            "Movimientos variables capturados antes del cálculo final de nómina por periodo y colaborador.",
            "PrePayrollAdjustmentId",
            [
                TextColumn("PrePayrollAdjustmentId", "PrePayrollAdjustment ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("PayrollPeriodId", "Periodo", periods, required: true, width: 220),
                LookupColumn("PayrollConceptId", "Concepto", concepts, width: 220),
                TextColumn("AdjustmentCode", "Código", required: true, width: 130),
                TextColumn("AdjustmentName", "Ajuste", required: true, width: 220),
                TextColumn("AdjustmentType", "Tipo", required: true, width: 100),
                TextColumn("CaptureSource", "Origen", width: 100),
                DateColumn("ReferenceDate", "Fecha ref.", required: true, width: 120),
                NumberColumn("Quantity", "Cantidad", width: 90),
                NumberColumn("Amount", "Importe", width: 110),
                NumberColumn("TaxableAmount", "Gravado", width: 110),
                NumberColumn("ExemptAmount", "Exento", width: 110),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 240),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PrePayrollAdjustmentId", x.PrePayrollAdjustmentId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("PayrollConceptId", x.PayrollConceptId?.ToString("D")),
                ("AdjustmentCode", x.AdjustmentCode),
                ("AdjustmentName", x.AdjustmentName),
                ("AdjustmentType", x.AdjustmentType),
                ("CaptureSource", x.CaptureSource),
                ("ReferenceDate", x.ReferenceDate),
                ("Quantity", x.Quantity),
                ("Amount", x.Amount),
                ("TaxableAmount", x.TaxableAmount),
                ("ExemptAmount", x.ExemptAmount),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPrePayrollCutoffsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PrePayrollCutoffDto>>("/api/payroll/prepayroll-cutoffs") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var branches = await GetBranchLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();

        return BuildView(
            "prepayroll-cutoffs",
            "Cortes de prenómina",
            "Control del cierre operativo de asistencia e incidencias antes de generar la corrida final de nómina.",
            "PrePayrollCutoffId",
            [
                TextColumn("PrePayrollCutoffId", "PrePayrollCutoff ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("BranchId", "Sucursal", branches, width: 180),
                LookupColumn("PayrollPeriodId", "Periodo", periods, required: true, width: 220),
                TextColumn("CutoffCode", "Código", required: true, width: 130),
                TextColumn("CutoffName", "Corte", required: true, width: 220),
                DateColumn("StartDate", "Desde", required: true, width: 120),
                DateColumn("EndDate", "Hasta", required: true, width: 120),
                NumberColumn("EmployeesReviewed", "Revisados", width: 95),
                NumberColumn("IncidentsDetected", "Incidencias", width: 95),
                NumberColumn("WorkedDaysTotal", "Días trabajados", width: 110),
                NumberColumn("OvertimeHoursTotal", "Hrs extra", width: 95),
                TextColumn("Status", "Estatus", width: 110),
                BoolColumn("IsClosed", "Cerrado", width: 90),
                DateColumn("ClosedAt", "Fecha cierre", width: 150),
                TextColumn("Notes", "Notas", width: 240),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PrePayrollCutoffId", x.PrePayrollCutoffId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("BranchId", x.BranchId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("CutoffCode", x.CutoffCode),
                ("CutoffName", x.CutoffName),
                ("StartDate", x.StartDate),
                ("EndDate", x.EndDate),
                ("EmployeesReviewed", x.EmployeesReviewed),
                ("IncidentsDetected", x.IncidentsDetected),
                ("WorkedDaysTotal", x.WorkedDaysTotal),
                ("OvertimeHoursTotal", x.OvertimeHoursTotal),
                ("Status", x.Status),
                ("IsClosed", x.IsClosed),
                ("ClosedAt", x.ClosedAt),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetLoansAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<EmployeeLoanDto>>("/api/payroll/loans") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var concepts = await GetPayrollConceptLookupsAsync();

        return BuildView(
            "employee-loans",
            "Préstamos",
            "Préstamos al colaborador con saldo y descuento periódico listo para aplicarse en nómina.",
            "EmployeeLoanId",
            [
                TextColumn("EmployeeLoanId", "EmployeeLoan ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("PayrollConceptId", "Concepto descuento", concepts, required: true, width: 220),
                TextColumn("LoanNumber", "Préstamo", required: true, width: 130),
                DateColumn("LoanDate", "Fecha préstamo", required: true, width: 120),
                DateColumn("StartDate", "Inicio desc.", required: true, width: 120),
                DateColumn("EndDate", "Fin", width: 120),
                NumberColumn("PrincipalAmount", "Monto", width: 110),
                NumberColumn("BalanceAmount", "Saldo", width: 110),
                NumberColumn("InstallmentAmount", "Descuento", width: 110),
                NumberColumn("Installments", "Cuotas", width: 90),
                NumberColumn("InstallmentsPaid", "Aplicadas", width: 90),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 220),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("EmployeeLoanId", x.EmployeeLoanId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("PayrollConceptId", x.PayrollConceptId?.ToString("D")),
                ("LoanNumber", x.LoanNumber),
                ("LoanDate", x.LoanDate),
                ("StartDate", x.StartDate),
                ("EndDate", x.EndDate),
                ("PrincipalAmount", x.PrincipalAmount),
                ("BalanceAmount", x.BalanceAmount),
                ("InstallmentAmount", x.InstallmentAmount),
                ("Installments", x.Installments),
                ("InstallmentsPaid", x.InstallmentsPaid),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetLoanDeductionsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<EmployeeLoanDeductionDto>>("/api/payroll/loan-deductions") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var loans = await GetLoanLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();

        return BuildView(
            "employee-loan-deductions",
            "Descuentos de préstamos",
            "Aplicaciones históricas del préstamo por periodo y corrida de nómina.",
            "EmployeeLoanDeductionId",
            [
                TextColumn("EmployeeLoanDeductionId", "LoanDeduction ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("EmployeeLoanId", "Préstamo", loans, required: true, width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("PayrollPeriodId", "Periodo", periods, width: 220),
                LookupColumn("PayrollRunId", "Corrida", runs, width: 220),
                DateColumn("DeductionDate", "Fecha", required: true, width: 120),
                NumberColumn("InstallmentNumber", "No. cuota", width: 90),
                NumberColumn("Amount", "Importe", width: 110),
                NumberColumn("PrincipalApplied", "Principal", width: 110),
                NumberColumn("InterestApplied", "Interés", width: 110),
                NumberColumn("RemainingBalance", "Saldo rem.", width: 110),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 220),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("EmployeeLoanDeductionId", x.EmployeeLoanDeductionId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("EmployeeLoanId", x.EmployeeLoanId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("DeductionDate", x.DeductionDate),
                ("InstallmentNumber", x.InstallmentNumber),
                ("Amount", x.Amount),
                ("PrincipalApplied", x.PrincipalApplied),
                ("InterestApplied", x.InterestApplied),
                ("RemainingBalance", x.RemainingBalance),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<List<CatalogLookupItem>> GetCompanyLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<CompanyLookupDto>>("/api/organization/companies") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem { Id = x.CompanyId.ToString("D"), Name = x.Name }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetBranchLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<BranchLookupDto>>("/api/organization/branches") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem { Id = x.BranchId.ToString("D"), Name = $"{x.Name} · {x.CompanyName}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetEmployeeLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<EmployeeLookupDto>>("/api/hr/employees") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.FullName).Select(x => new CatalogLookupItem { Id = x.EmployeeId.ToString("D"), Name = $"{x.EmployeeNumber} · {x.FullName}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPayrollConceptLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollConceptLookupDto>>("/api/payroll/concepts") ?? [];
        return rows.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogLookupItem { Id = x.PayrollConceptId.ToString("D"), Name = $"{x.Code} · {x.Name}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPayrollPeriodLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollPeriodLookupDto>>("/api/payroll/periods") ?? [];
        return rows.Where(x => x.IsActive).OrderByDescending(x => x.StartDate).Select(x => new CatalogLookupItem { Id = x.PayrollPeriodId.ToString("D"), Name = $"{x.Code} · {x.Name}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetPayrollRunLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollRunLookupDto>>("/api/payroll/runs") ?? [];
        return rows.Where(x => x.IsActive).OrderByDescending(x => x.RunDate).Select(x => new CatalogLookupItem { Id = x.PayrollRunId.ToString("D"), Name = $"{x.Folio} · {x.PayrollPeriodName}" }).ToList();
    }

    private async Task<List<CatalogLookupItem>> GetLoanLookupsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<EmployeeLoanDto>>("/api/payroll/loans") ?? [];
        return rows.Where(x => x.IsActive).OrderByDescending(x => x.LoanDate).Select(x => new CatalogLookupItem { Id = x.EmployeeLoanId.ToString("D"), Name = $"{x.LoanNumber} · {x.EmployeeName}" }).ToList();
    }



    private async Task<CatalogViewDefinition> GetPayrollSourceApplicationsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollSourceApplicationDto>>("/api/payroll/source-applications") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();
        var concepts = await GetPayrollConceptLookupsAsync();

        return BuildView(
            "payroll-source-applications",
            "Aplicaciones calculadas",
            "Trazabilidad enterprise de lo aplicado al recibo: ajustes, incidencias, préstamos y conceptos generados.",
            "PayrollSourceApplicationId",
            [
                TextColumn("PayrollSourceApplicationId", "SourceApplication ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                TextColumn("PayrollRunLineId", "Recibo / línea", width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                LookupColumn("PayrollPeriodId", "Periodo", periods, width: 220),
                LookupColumn("PayrollConceptId", "Concepto nómina", concepts, width: 220),
                TextColumn("SourceId", "Fuente ID", width: 220),
                TextColumn("SourceType", "Tipo fuente", required: true, width: 130),
                TextColumn("ApplicationCode", "Código", required: true, width: 110),
                TextColumn("ApplicationName", "Aplicación", required: true, width: 220),
                TextColumn("MovementType", "Movimiento", required: true, width: 110),
                NumberColumn("Quantity", "Cantidad", width: 100),
                NumberColumn("Amount", "Importe", width: 110),
                NumberColumn("TaxableAmount", "Gravado", width: 110),
                NumberColumn("ExemptAmount", "Exento", width: 110),
                DateColumn("AppliedAt", "Aplicado", required: true, width: 150),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollSourceApplicationId", x.PayrollSourceApplicationId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("PayrollRunLineId", x.PayrollRunLineId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("PayrollConceptId", x.PayrollConceptId?.ToString("D")),
                ("SourceId", x.SourceId?.ToString("D")),
                ("SourceType", x.SourceType),
                ("ApplicationCode", x.ApplicationCode),
                ("ApplicationName", x.ApplicationName),
                ("MovementType", x.MovementType),
                ("Quantity", x.Quantity),
                ("Amount", x.Amount),
                ("TaxableAmount", x.TaxableAmount),
                ("ExemptAmount", x.ExemptAmount),
                ("AppliedAt", x.AppliedAt),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollReceiptControlAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollReceiptControlDto>>("/api/payroll/receipt-control") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();

        return BuildView(
            "payroll-receipt-control",
            "Control de recibos",
            "Seguimiento operativo y fiscal del recibo por colaborador: generado, revisado, entregado y timbrado.",
            "PayrollReceiptControlId",
            [
                TextColumn("PayrollReceiptControlId", "ReceiptControl ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                TextColumn("PayrollRunLineId", "Recibo / línea", required: true, width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                TextColumn("ReceiptNumber", "Recibo", required: true, width: 160),
                TextColumn("ReceiptStatus", "Estatus", required: true, width: 110),
                DateColumn("GeneratedAt", "Generado", required: true, width: 150),
                DateColumn("ReviewedAt", "Revisado", width: 150),
                DateColumn("DeliveredAt", "Entregado", width: 150),
                DateColumn("StampedAt", "Timbrado", width: 150),
                TextColumn("DeliveryChannel", "Canal", width: 120),
                TextColumn("DeliveryReference", "Referencia", width: 180),
                TextColumn("AckBy", "Acuse", width: 160),
                NumberColumn("NetAmount", "Neto", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollReceiptControlId", x.PayrollReceiptControlId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("PayrollRunLineId", x.PayrollRunLineId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("ReceiptNumber", x.ReceiptNumber),
                ("ReceiptStatus", x.ReceiptStatus),
                ("GeneratedAt", x.GeneratedAt),
                ("ReviewedAt", x.ReviewedAt),
                ("DeliveredAt", x.DeliveredAt),
                ("StampedAt", x.StampedAt),
                ("DeliveryChannel", x.DeliveryChannel),
                ("DeliveryReference", x.DeliveryReference),
                ("AckBy", x.AckBy),
                ("NetAmount", x.NetAmount),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollRunClosingsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollRunClosingDto>>("/api/payroll/run-closings") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();

        return BuildView(
            "payroll-run-closings",
            "Cierres de nómina",
            "Control ejecutivo del cierre: recibos, aplicaciones, incidencias detectadas y bloqueo final del proceso.",
            "PayrollRunClosingId",
            [
                TextColumn("PayrollRunClosingId", "RunClosing ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                TextColumn("ClosingCode", "Cierre", required: true, width: 150),
                DateColumn("ClosingDate", "Fecha cierre", required: true, width: 150),
                NumberColumn("EmployeesIncluded", "Empleados", width: 100),
                NumberColumn("GrossAmount", "Bruto", width: 110),
                NumberColumn("DeductionsAmount", "Deducciones", width: 120),
                NumberColumn("NetAmount", "Neto", width: 110),
                NumberColumn("SourceApplicationsCount", "Aplicaciones", width: 110),
                NumberColumn("ReceiptsGeneratedCount", "Recibos", width: 100),
                NumberColumn("IssuesDetected", "Hallazgos", width: 100),
                TextColumn("Status", "Estatus", width: 110),
                BoolColumn("IsLocked", "Bloqueado", width: 95),
                DateColumn("LockedAt", "Bloqueado el", width: 150),
                TextColumn("ClosedBy", "Cerró", width: 160),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollRunClosingId", x.PayrollRunClosingId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("ClosingCode", x.ClosingCode),
                ("ClosingDate", x.ClosingDate),
                ("EmployeesIncluded", x.EmployeesIncluded),
                ("GrossAmount", x.GrossAmount),
                ("DeductionsAmount", x.DeductionsAmount),
                ("NetAmount", x.NetAmount),
                ("SourceApplicationsCount", x.SourceApplicationsCount),
                ("ReceiptsGeneratedCount", x.ReceiptsGeneratedCount),
                ("IssuesDetected", x.IssuesDetected),
                ("Status", x.Status),
                ("IsLocked", x.IsLocked),
                ("LockedAt", x.LockedAt),
                ("ClosedBy", x.ClosedBy),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }


    private async Task<CatalogViewDefinition> GetPayrollDispersionBatchesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollDispersionBatchDto>>("/api/payroll/dispersion-batches") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();

        return BuildView(
            "payroll-dispersion-batches",
            "Dispersiones bancarias",
            "Lotes bancarios por corrida: layout, cuenta fondeadora, estatus de exportación y confirmación.",
            "PayrollDispersionBatchId",
            [
                TextColumn("PayrollDispersionBatchId", "DispersionBatch ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                TextColumn("BatchCode", "Lote", required: true, width: 150),
                DateColumn("DispersionDate", "Fecha dispersión", required: true, width: 150),
                TextColumn("LayoutFormat", "Formato", required: true, width: 110),
                TextColumn("BankName", "Banco", width: 160),
                TextColumn("FundingAccount", "Cuenta fondeo", width: 170),
                NumberColumn("BeneficiariesCount", "Beneficiarios", width: 110),
                NumberColumn("TotalAmount", "Importe total", width: 120),
                TextColumn("Status", "Estatus", width: 110),
                DateColumn("ApprovedAt", "Aprobado", width: 150),
                DateColumn("ExportedAt", "Exportado", width: 150),
                DateColumn("ConfirmedAt", "Confirmado", width: 150),
                TextColumn("FileReference", "Archivo", width: 180),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollDispersionBatchId", x.PayrollDispersionBatchId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("BatchCode", x.BatchCode),
                ("DispersionDate", x.DispersionDate),
                ("LayoutFormat", x.LayoutFormat),
                ("BankName", x.BankName),
                ("FundingAccount", x.FundingAccount),
                ("BeneficiariesCount", x.BeneficiariesCount),
                ("TotalAmount", x.TotalAmount),
                ("Status", x.Status),
                ("ApprovedAt", x.ApprovedAt),
                ("ExportedAt", x.ExportedAt),
                ("ConfirmedAt", x.ConfirmedAt),
                ("FileReference", x.FileReference),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollDispersionLinesAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollDispersionLineDto>>("/api/payroll/dispersion-lines") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();
        var batches = rows
            .Select(x => new CatalogLookupItem { Id = x.PayrollDispersionBatchId?.ToString("D") ?? string.Empty, Name = string.IsNullOrWhiteSpace(x.BatchCode) ? (x.PayrollDispersionBatchId?.ToString("D") ?? string.Empty) : x.BatchCode })
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();

        return BuildView(
            "payroll-dispersion-lines",
            "Líneas de dispersión",
            "Detalle por colaborador para pago bancario: banco destino, referencia y validación previa a la dispersión.",
            "PayrollDispersionLineId",
            [
                TextColumn("PayrollDispersionLineId", "DispersionLine ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollDispersionBatchId", "Lote", batches, required: true, width: 180),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                TextColumn("PayrollRunLineId", "Recibo / línea", required: true, width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                NumberColumn("Sequence", "Secuencia", width: 90),
                TextColumn("EmployeeNumber", "Número", width: 110),
                TextColumn("BeneficiaryName", "Beneficiario", required: true, width: 220),
                TextColumn("BankName", "Banco", width: 160),
                TextColumn("BankAccount", "Cuenta", width: 170),
                TextColumn("Clabe", "CLABE", width: 170),
                NumberColumn("NetAmount", "Neto", width: 110),
                TextColumn("PaymentReference", "Referencia", width: 160),
                TextColumn("ValidationStatus", "Validación", width: 110),
                BoolColumn("IsRejected", "Rechazado", width: 95),
                DateColumn("PaidAt", "Pagado", width: 150),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollDispersionLineId", x.PayrollDispersionLineId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollDispersionBatchId", x.PayrollDispersionBatchId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("PayrollRunLineId", x.PayrollRunLineId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("Sequence", x.Sequence),
                ("EmployeeNumber", x.EmployeeNumber),
                ("BeneficiaryName", x.BeneficiaryName),
                ("BankName", x.BankName),
                ("BankAccount", x.BankAccount),
                ("Clabe", x.Clabe),
                ("NetAmount", x.NetAmount),
                ("PaymentReference", x.PaymentReference),
                ("ValidationStatus", x.ValidationStatus),
                ("IsRejected", x.IsRejected),
                ("PaidAt", x.PaidAt),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollAccountingPostingsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollAccountingPostingDto>>("/api/payroll/accounting-postings") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();

        return BuildView(
            "payroll-accounting-postings",
            "Pólizas de nómina",
            "Interfaz contable enterprise de la nómina: póliza, libro, referencia de exportación y control de posteo.",
            "PayrollAccountingPostingId",
            [
                TextColumn("PayrollAccountingPostingId", "AccountingPosting ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                TextColumn("PostingCode", "Póliza", required: true, width: 150),
                DateColumn("PostingDate", "Fecha póliza", required: true, width: 150),
                TextColumn("LedgerBook", "Libro", required: true, width: 120),
                TextColumn("JournalNumber", "Diario", width: 120),
                NumberColumn("DebitAmount", "Debe", width: 110),
                NumberColumn("CreditAmount", "Haber", width: 110),
                NumberColumn("LinesCount", "Renglones", width: 100),
                TextColumn("Status", "Estatus", width: 110),
                DateColumn("ExportedAt", "Exportado", width: 150),
                DateColumn("PostedAt", "Posteado", width: 150),
                DateColumn("LockedAt", "Bloqueado", width: 150),
                TextColumn("ExportReference", "Referencia export", width: 200),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollAccountingPostingId", x.PayrollAccountingPostingId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("PostingCode", x.PostingCode),
                ("PostingDate", x.PostingDate),
                ("LedgerBook", x.LedgerBook),
                ("JournalNumber", x.JournalNumber),
                ("DebitAmount", x.DebitAmount),
                ("CreditAmount", x.CreditAmount),
                ("LinesCount", x.LinesCount),
                ("Status", x.Status),
                ("ExportedAt", x.ExportedAt),
                ("PostedAt", x.PostedAt),
                ("LockedAt", x.LockedAt),
                ("ExportReference", x.ExportReference),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }


    private async Task<CatalogViewDefinition> GetPayrollTaxAccumulatorsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollTaxAccumulatorDto>>("/api/payroll/tax-accumulators") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();
        var employees = await GetEmployeeLookupsAsync();

        return BuildView(
            "payroll-tax-accumulators",
            "Acumulados fiscales",
            "Acumulados ISR / CFDI por colaborador y corrida, con base gravable, exenta y seguimiento fiscal de nómina.",
            "PayrollTaxAccumulatorId",
            [
                TextColumn("PayrollTaxAccumulatorId", "TaxAccumulator ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                TextColumn("PayrollRunLineId", "Línea nómina", required: true, width: 220),
                LookupColumn("PayrollPeriodId", "Periodo", periods, required: true, width: 220),
                LookupColumn("EmployeeId", "Colaborador", employees, required: true, width: 240),
                TextColumn("AccumulatorCode", "Código", required: true, width: 140),
                TextColumn("AccumulatorName", "Nombre", required: true, width: 220),
                NumberColumn("FiscalYear", "Ejercicio", width: 100),
                NumberColumn("FiscalMonth", "Mes", width: 90),
                NumberColumn("TaxableAmount", "Gravado", width: 110),
                NumberColumn("ExemptAmount", "Exento", width: 110),
                NumberColumn("WithheldIsr", "ISR retenido", width: 110),
                NumberColumn("SubsidyApplied", "Subsidio", width: 100),
                NumberColumn("SocialSecurityBase", "Base IMSS", width: 110),
                NumberColumn("NetAmount", "Neto", width: 110),
                DateColumn("LastCalculatedAt", "Calculado", width: 150),
                TextColumn("Status", "Estatus", width: 110),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollTaxAccumulatorId", x.PayrollTaxAccumulatorId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("PayrollRunLineId", x.PayrollRunLineId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("EmployeeId", x.EmployeeId?.ToString("D")),
                ("AccumulatorCode", x.AccumulatorCode),
                ("AccumulatorName", x.AccumulatorName),
                ("FiscalYear", x.FiscalYear),
                ("FiscalMonth", x.FiscalMonth),
                ("TaxableAmount", x.TaxableAmount),
                ("ExemptAmount", x.ExemptAmount),
                ("WithheldIsr", x.WithheldIsr),
                ("SubsidyApplied", x.SubsidyApplied),
                ("SocialSecurityBase", x.SocialSecurityBase),
                ("NetAmount", x.NetAmount),
                ("LastCalculatedAt", x.LastCalculatedAt),
                ("Status", x.Status),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollEmployerObligationsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollEmployerObligationDto>>("/api/payroll/employer-obligations") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();

        return BuildView(
            "payroll-employer-obligations",
            "Obligaciones patronales",
            "Cuotas y aportaciones derivadas de nómina: IMSS, INFONAVIT, ISN y obligaciones patronales del periodo.",
            "PayrollEmployerObligationId",
            [
                TextColumn("PayrollEmployerObligationId", "EmployerObligation ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                LookupColumn("PayrollPeriodId", "Periodo", periods, required: true, width: 220),
                TextColumn("ObligationCode", "Código", required: true, width: 120),
                TextColumn("ObligationName", "Nombre", required: true, width: 220),
                TextColumn("ObligationType", "Tipo", width: 140),
                NumberColumn("FiscalYear", "Ejercicio", width: 100),
                NumberColumn("FiscalMonth", "Mes", width: 90),
                NumberColumn("BaseAmount", "Base", width: 110),
                NumberColumn("Amount", "Importe", width: 110),
                NumberColumn("EmployeesCount", "Colabs.", width: 90),
                DateColumn("DueDate", "Vence", width: 150),
                TextColumn("Status", "Estatus", width: 110),
                DateColumn("PaidAt", "Pagado", width: 150),
                TextColumn("ReferenceNumber", "Referencia", width: 180),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollEmployerObligationId", x.PayrollEmployerObligationId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("ObligationCode", x.ObligationCode),
                ("ObligationName", x.ObligationName),
                ("ObligationType", x.ObligationType),
                ("FiscalYear", x.FiscalYear),
                ("FiscalMonth", x.FiscalMonth),
                ("BaseAmount", x.BaseAmount),
                ("Amount", x.Amount),
                ("EmployeesCount", x.EmployeesCount),
                ("DueDate", x.DueDate),
                ("Status", x.Status),
                ("PaidAt", x.PaidAt),
                ("ReferenceNumber", x.ReferenceNumber),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private async Task<CatalogViewDefinition> GetPayrollFiscalReconciliationsAsync()
    {
        var client = _httpClientFactory.CreateClient("Nanchesoft.Api");
        var rows = await client.GetFromJsonAsync<List<PayrollFiscalReconciliationDto>>("/api/payroll/fiscal-reconciliations") ?? [];
        var companies = await GetCompanyLookupsAsync();
        var runs = await GetPayrollRunLookupsAsync();
        var periods = await GetPayrollPeriodLookupsAsync();
        var batches = rows
            .Select(x => new CatalogLookupItem { Id = x.PayrollDispersionBatchId?.ToString("D") ?? string.Empty, Name = string.IsNullOrWhiteSpace(x.ReconciliationCode) ? (x.PayrollDispersionBatchId?.ToString("D") ?? string.Empty) : $"DSPREF {x.ReconciliationCode}" })
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
        var postings = rows
            .Select(x => new CatalogLookupItem { Id = x.PayrollAccountingPostingId?.ToString("D") ?? string.Empty, Name = string.IsNullOrWhiteSpace(x.ReconciliationCode) ? (x.PayrollAccountingPostingId?.ToString("D") ?? string.Empty) : $"POLREF {x.ReconciliationCode}" })
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();

        return BuildView(
            "payroll-fiscal-reconciliations",
            "Conciliaciones fiscales",
            "Conciliación enterprise entre corrida, acumulados fiscales, recibos, dispersión bancaria y póliza contable.",
            "PayrollFiscalReconciliationId",
            [
                TextColumn("PayrollFiscalReconciliationId", "FiscalReconciliation ID", allowEditing: false, width: 220),
                LookupColumn("CompanyId", "Empresa", companies, required: true, width: 220),
                LookupColumn("PayrollRunId", "Proceso nómina", runs, required: true, width: 220),
                LookupColumn("PayrollPeriodId", "Periodo", periods, required: true, width: 220),
                LookupColumn("PayrollDispersionBatchId", "Disp. bancaria", batches, width: 180),
                LookupColumn("PayrollAccountingPostingId", "Póliza", postings, width: 180),
                TextColumn("ReconciliationCode", "Código", required: true, width: 150),
                NumberColumn("FiscalYear", "Ejercicio", width: 100),
                NumberColumn("FiscalMonth", "Mes", width: 90),
                NumberColumn("ReceiptsStampedCount", "Recibos", width: 90),
                NumberColumn("DispersionValidatedCount", "Dispersión", width: 95),
                NumberColumn("AccountingPostedCount", "Pólizas", width: 90),
                NumberColumn("TaxAccumulatorsCount", "Acumulados", width: 95),
                NumberColumn("GrossAmount", "Bruto", width: 110),
                NumberColumn("WithheldIsrAmount", "ISR", width: 110),
                NumberColumn("EmployerTaxesAmount", "Imp. patronal", width: 120),
                NumberColumn("NetAmount", "Neto", width: 110),
                NumberColumn("DifferenceAmount", "Diferencia", width: 110),
                TextColumn("Status", "Estatus", width: 110),
                DateColumn("ReconciledAt", "Conciliado", width: 150),
                TextColumn("ClosedBy", "Cerró", width: 150),
                TextColumn("Notes", "Notas", width: 260),
                BoolColumn("IsActive", "Activo", width: 90)
            ],
            rows.Select(x => Row(
                ("PayrollFiscalReconciliationId", x.PayrollFiscalReconciliationId.ToString("D")),
                ("CompanyId", x.CompanyId?.ToString("D")),
                ("PayrollRunId", x.PayrollRunId?.ToString("D")),
                ("PayrollPeriodId", x.PayrollPeriodId?.ToString("D")),
                ("PayrollDispersionBatchId", x.PayrollDispersionBatchId?.ToString("D")),
                ("PayrollAccountingPostingId", x.PayrollAccountingPostingId?.ToString("D")),
                ("ReconciliationCode", x.ReconciliationCode),
                ("FiscalYear", x.FiscalYear),
                ("FiscalMonth", x.FiscalMonth),
                ("ReceiptsStampedCount", x.ReceiptsStampedCount),
                ("DispersionValidatedCount", x.DispersionValidatedCount),
                ("AccountingPostedCount", x.AccountingPostedCount),
                ("TaxAccumulatorsCount", x.TaxAccumulatorsCount),
                ("GrossAmount", x.GrossAmount),
                ("WithheldIsrAmount", x.WithheldIsrAmount),
                ("EmployerTaxesAmount", x.EmployerTaxesAmount),
                ("NetAmount", x.NetAmount),
                ("DifferenceAmount", x.DifferenceAmount),
                ("Status", x.Status),
                ("ReconciledAt", x.ReconciledAt),
                ("ClosedBy", x.ClosedBy),
                ("Notes", x.Notes),
                ("IsActive", x.IsActive)))
            .ToList());
    }

    private static PayrollTaxAccumulatorRequest MapPayrollTaxAccumulatorRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PayrollRunLineId = ReadGuid(payload, "PayrollRunLineId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        AccumulatorCode = ReadString(payload, "AccumulatorCode"),
        AccumulatorName = ReadString(payload, "AccumulatorName"),
        FiscalYear = ReadInt(payload, "FiscalYear"),
        FiscalMonth = ReadInt(payload, "FiscalMonth"),
        TaxableAmount = ReadDecimal(payload, "TaxableAmount"),
        ExemptAmount = ReadDecimal(payload, "ExemptAmount"),
        WithheldIsr = ReadDecimal(payload, "WithheldIsr"),
        SubsidyApplied = ReadDecimal(payload, "SubsidyApplied"),
        SocialSecurityBase = ReadDecimal(payload, "SocialSecurityBase"),
        NetAmount = ReadDecimal(payload, "NetAmount"),
        LastCalculatedAt = ReadDate(payload, "LastCalculatedAt"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollEmployerObligationRequest MapPayrollEmployerObligationRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        ObligationCode = ReadString(payload, "ObligationCode"),
        ObligationName = ReadString(payload, "ObligationName"),
        ObligationType = ReadString(payload, "ObligationType"),
        FiscalYear = ReadInt(payload, "FiscalYear"),
        FiscalMonth = ReadInt(payload, "FiscalMonth"),
        BaseAmount = ReadDecimal(payload, "BaseAmount"),
        Amount = ReadDecimal(payload, "Amount"),
        EmployeesCount = ReadInt(payload, "EmployeesCount"),
        DueDate = ReadDate(payload, "DueDate"),
        Status = ReadString(payload, "Status"),
        PaidAt = ReadDate(payload, "PaidAt"),
        ReferenceNumber = ReadString(payload, "ReferenceNumber"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollFiscalReconciliationRequest MapPayrollFiscalReconciliationRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        PayrollDispersionBatchId = ReadGuid(payload, "PayrollDispersionBatchId"),
        PayrollAccountingPostingId = ReadGuid(payload, "PayrollAccountingPostingId"),
        ReconciliationCode = ReadString(payload, "ReconciliationCode"),
        FiscalYear = ReadInt(payload, "FiscalYear"),
        FiscalMonth = ReadInt(payload, "FiscalMonth"),
        ReceiptsStampedCount = ReadInt(payload, "ReceiptsStampedCount"),
        DispersionValidatedCount = ReadInt(payload, "DispersionValidatedCount"),
        AccountingPostedCount = ReadInt(payload, "AccountingPostedCount"),
        TaxAccumulatorsCount = ReadInt(payload, "TaxAccumulatorsCount"),
        GrossAmount = ReadDecimal(payload, "GrossAmount"),
        WithheldIsrAmount = ReadDecimal(payload, "WithheldIsrAmount"),
        EmployerTaxesAmount = ReadDecimal(payload, "EmployerTaxesAmount"),
        NetAmount = ReadDecimal(payload, "NetAmount"),
        DifferenceAmount = ReadDecimal(payload, "DifferenceAmount"),
        Status = ReadString(payload, "Status"),
        ReconciledAt = ReadDate(payload, "ReconciledAt"),
        ClosedBy = ReadString(payload, "ClosedBy"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };


    private static AttendancePunchRequest MapAttendancePunchRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        WorkDate = ReadDate(payload, "WorkDate"),
        PunchDateTime = ReadDate(payload, "PunchDateTime"),
        PunchType = ReadString(payload, "PunchType"),
        Source = ReadString(payload, "Source"),
        DeviceName = ReadString(payload, "DeviceName"),
        DeviceSerial = ReadString(payload, "DeviceSerial"),
        ExternalReference = ReadString(payload, "ExternalReference"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollRecurringMovementRequest MapRecurringMovementRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        PayrollConceptId = ReadGuid(payload, "PayrollConceptId"),
        MovementCode = ReadString(payload, "MovementCode"),
        MovementName = ReadString(payload, "MovementName"),
        MovementType = ReadString(payload, "MovementType"),
        CalculationMode = ReadString(payload, "CalculationMode"),
        Quantity = ReadDecimal(payload, "Quantity"),
        Amount = ReadDecimal(payload, "Amount"),
        Percentage = ReadDecimal(payload, "Percentage"),
        EffectiveStartDate = ReadDate(payload, "EffectiveStartDate"),
        EffectiveEndDate = ReadDate(payload, "EffectiveEndDate"),
        ApplyEveryRun = ReadBool(payload, "ApplyEveryRun", true),
        DayOfPeriod = ReadNullableInt(payload, "DayOfPeriod"),
        IsProrated = ReadBool(payload, "IsProrated"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static EmployeeLoanRequest MapEmployeeLoanRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        PayrollConceptId = ReadGuid(payload, "PayrollConceptId"),
        LoanNumber = ReadString(payload, "LoanNumber"),
        LoanDate = ReadDate(payload, "LoanDate"),
        StartDate = ReadDate(payload, "StartDate"),
        EndDate = ReadDate(payload, "EndDate"),
        PrincipalAmount = ReadDecimal(payload, "PrincipalAmount"),
        BalanceAmount = ReadDecimal(payload, "BalanceAmount"),
        InstallmentAmount = ReadDecimal(payload, "InstallmentAmount"),
        Installments = ReadInt(payload, "Installments"),
        InstallmentsPaid = ReadInt(payload, "InstallmentsPaid"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static EmployeeLoanDeductionRequest MapEmployeeLoanDeductionRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        EmployeeLoanId = ReadGuid(payload, "EmployeeLoanId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PayrollRunLineId = ReadGuid(payload, "PayrollRunLineId"),
        DeductionDate = ReadDate(payload, "DeductionDate"),
        InstallmentNumber = ReadInt(payload, "InstallmentNumber"),
        Amount = ReadDecimal(payload, "Amount"),
        PrincipalApplied = ReadDecimal(payload, "PrincipalApplied"),
        InterestApplied = ReadDecimal(payload, "InterestApplied"),
        RemainingBalance = ReadDecimal(payload, "RemainingBalance"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static AttendanceDailySummaryRequest MapAttendanceDailySummaryRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        WorkDate = ReadDate(payload, "WorkDate"),
        ScheduledEntryTime = ReadDate(payload, "ScheduledEntryTime"),
        ScheduledExitTime = ReadDate(payload, "ScheduledExitTime"),
        FirstPunchDateTime = ReadDate(payload, "FirstPunchDateTime"),
        LastPunchDateTime = ReadDate(payload, "LastPunchDateTime"),
        WorkedHours = ReadDecimal(payload, "WorkedHours"),
        DelayMinutes = ReadInt(payload, "DelayMinutes"),
        EarlyLeaveMinutes = ReadInt(payload, "EarlyLeaveMinutes"),
        OvertimeHours = ReadDecimal(payload, "OvertimeHours"),
        AbsenceUnits = ReadDecimal(payload, "AbsenceUnits"),
        DayType = ReadString(payload, "DayType"),
        Status = ReadString(payload, "Status"),
        Source = ReadString(payload, "Source"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PrePayrollAdjustmentRequest MapPrePayrollAdjustmentRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        PayrollConceptId = ReadGuid(payload, "PayrollConceptId"),
        AdjustmentCode = ReadString(payload, "AdjustmentCode"),
        AdjustmentName = ReadString(payload, "AdjustmentName"),
        AdjustmentType = ReadString(payload, "AdjustmentType"),
        CaptureSource = ReadString(payload, "CaptureSource"),
        ReferenceDate = ReadDate(payload, "ReferenceDate"),
        Quantity = ReadDecimal(payload, "Quantity"),
        Amount = ReadDecimal(payload, "Amount"),
        TaxableAmount = ReadDecimal(payload, "TaxableAmount"),
        ExemptAmount = ReadDecimal(payload, "ExemptAmount"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PrePayrollCutoffRequest MapPrePayrollCutoffRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        BranchId = ReadGuid(payload, "BranchId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        CutoffCode = ReadString(payload, "CutoffCode"),
        CutoffName = ReadString(payload, "CutoffName"),
        StartDate = ReadDate(payload, "StartDate"),
        EndDate = ReadDate(payload, "EndDate"),
        EmployeesReviewed = ReadInt(payload, "EmployeesReviewed"),
        IncidentsDetected = ReadInt(payload, "IncidentsDetected"),
        WorkedDaysTotal = ReadDecimal(payload, "WorkedDaysTotal"),
        OvertimeHoursTotal = ReadDecimal(payload, "OvertimeHoursTotal"),
        Status = ReadString(payload, "Status"),
        IsClosed = ReadBool(payload, "IsClosed"),
        ClosedAt = ReadDate(payload, "ClosedAt"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };



    private static PayrollSourceApplicationRequest MapPayrollSourceApplicationRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PayrollRunLineId = ReadGuid(payload, "PayrollRunLineId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        PayrollPeriodId = ReadGuid(payload, "PayrollPeriodId"),
        PayrollConceptId = ReadGuid(payload, "PayrollConceptId"),
        SourceId = ReadGuid(payload, "SourceId"),
        SourceType = ReadString(payload, "SourceType"),
        ApplicationCode = ReadString(payload, "ApplicationCode"),
        ApplicationName = ReadString(payload, "ApplicationName"),
        MovementType = ReadString(payload, "MovementType"),
        Quantity = ReadDecimal(payload, "Quantity"),
        Amount = ReadDecimal(payload, "Amount"),
        TaxableAmount = ReadDecimal(payload, "TaxableAmount"),
        ExemptAmount = ReadDecimal(payload, "ExemptAmount"),
        AppliedAt = ReadDate(payload, "AppliedAt"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollReceiptControlRequest MapPayrollReceiptControlRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PayrollRunLineId = ReadGuid(payload, "PayrollRunLineId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        ReceiptNumber = ReadString(payload, "ReceiptNumber"),
        ReceiptStatus = ReadString(payload, "ReceiptStatus"),
        GeneratedAt = ReadDate(payload, "GeneratedAt"),
        ReviewedAt = ReadDate(payload, "ReviewedAt"),
        DeliveredAt = ReadDate(payload, "DeliveredAt"),
        StampedAt = ReadDate(payload, "StampedAt"),
        DeliveryChannel = ReadString(payload, "DeliveryChannel"),
        DeliveryReference = ReadString(payload, "DeliveryReference"),
        AckBy = ReadString(payload, "AckBy"),
        NetAmount = ReadDecimal(payload, "NetAmount"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollRunClosingRequest MapPayrollRunClosingRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        ClosingCode = ReadString(payload, "ClosingCode"),
        ClosingDate = ReadDate(payload, "ClosingDate"),
        EmployeesIncluded = ReadInt(payload, "EmployeesIncluded"),
        GrossAmount = ReadDecimal(payload, "GrossAmount"),
        DeductionsAmount = ReadDecimal(payload, "DeductionsAmount"),
        NetAmount = ReadDecimal(payload, "NetAmount"),
        SourceApplicationsCount = ReadInt(payload, "SourceApplicationsCount"),
        ReceiptsGeneratedCount = ReadInt(payload, "ReceiptsGeneratedCount"),
        IssuesDetected = ReadInt(payload, "IssuesDetected"),
        Status = ReadString(payload, "Status"),
        IsLocked = ReadBool(payload, "IsLocked"),
        LockedAt = ReadDate(payload, "LockedAt"),
        ClosedBy = ReadString(payload, "ClosedBy"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };


    private static PayrollDispersionBatchRequest MapPayrollDispersionBatchRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        BatchCode = ReadString(payload, "BatchCode"),
        DispersionDate = ReadDate(payload, "DispersionDate"),
        LayoutFormat = ReadString(payload, "LayoutFormat"),
        BankName = ReadString(payload, "BankName"),
        FundingAccount = ReadString(payload, "FundingAccount"),
        BeneficiariesCount = ReadInt(payload, "BeneficiariesCount"),
        TotalAmount = ReadDecimal(payload, "TotalAmount"),
        Status = ReadString(payload, "Status"),
        ApprovedAt = ReadDate(payload, "ApprovedAt"),
        ExportedAt = ReadDate(payload, "ExportedAt"),
        ConfirmedAt = ReadDate(payload, "ConfirmedAt"),
        FileReference = ReadString(payload, "FileReference"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollDispersionLineRequest MapPayrollDispersionLineRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollDispersionBatchId = ReadGuid(payload, "PayrollDispersionBatchId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PayrollRunLineId = ReadGuid(payload, "PayrollRunLineId"),
        EmployeeId = ReadGuid(payload, "EmployeeId"),
        Sequence = ReadInt(payload, "Sequence"),
        EmployeeNumber = ReadString(payload, "EmployeeNumber"),
        BeneficiaryName = ReadString(payload, "BeneficiaryName"),
        BankName = ReadString(payload, "BankName"),
        BankAccount = ReadString(payload, "BankAccount"),
        Clabe = ReadString(payload, "Clabe"),
        NetAmount = ReadDecimal(payload, "NetAmount"),
        PaymentReference = ReadString(payload, "PaymentReference"),
        ValidationStatus = ReadString(payload, "ValidationStatus"),
        IsRejected = ReadBool(payload, "IsRejected"),
        PaidAt = ReadDate(payload, "PaidAt"),
        Status = ReadString(payload, "Status"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static PayrollAccountingPostingRequest MapPayrollAccountingPostingRequest(JsonElement payload) => new()
    {
        CompanyId = ReadGuid(payload, "CompanyId"),
        PayrollRunId = ReadGuid(payload, "PayrollRunId"),
        PostingCode = ReadString(payload, "PostingCode"),
        PostingDate = ReadDate(payload, "PostingDate"),
        LedgerBook = ReadString(payload, "LedgerBook"),
        JournalNumber = ReadString(payload, "JournalNumber"),
        DebitAmount = ReadDecimal(payload, "DebitAmount"),
        CreditAmount = ReadDecimal(payload, "CreditAmount"),
        LinesCount = ReadInt(payload, "LinesCount"),
        Status = ReadString(payload, "Status"),
        ExportedAt = ReadDate(payload, "ExportedAt"),
        PostedAt = ReadDate(payload, "PostedAt"),
        LockedAt = ReadDate(payload, "LockedAt"),
        ExportReference = ReadString(payload, "ExportReference"),
        Notes = ReadString(payload, "Notes"),
        IsActive = ReadBool(payload, "IsActive", true)
    };

    private static CatalogViewDefinition BuildView(string catalogKey, string title, string subtitle, string keyExpr, List<CatalogColumnDefinition> columns, List<Dictionary<string, object?>> rows)
        => new()
        {
            CatalogKey = catalogKey,
            Title = title,
            Subtitle = subtitle,
            KeyExpr = keyExpr,
            AllowCreate = true,
            AllowUpdate = true,
            AllowDelete = true,
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

    private static CatalogColumnDefinition TextColumn(string field, string caption, bool required = false, bool allowEditing = true, int width = 160)
        => new() { DataField = field, Caption = caption, DataType = "string", Required = required, AllowEditing = allowEditing, Width = width };

    private static CatalogColumnDefinition NumberColumn(string field, string caption, bool required = false, int width = 120)
        => new() { DataField = field, Caption = caption, DataType = "number", Required = required, Width = width };

    private static CatalogColumnDefinition BoolColumn(string field, string caption, int width = 90)
        => new() { DataField = field, Caption = caption, DataType = "boolean", Width = width };

    private static CatalogColumnDefinition DateColumn(string field, string caption, bool required = false, int width = 120)
        => new() { DataField = field, Caption = caption, DataType = "date", Required = required, Width = width };

    private static CatalogColumnDefinition LookupColumn(string field, string caption, List<CatalogLookupItem> lookupItems, bool required = false, int width = 180)
        => new() { DataField = field, Caption = caption, DataType = "string", Required = required, Width = width, UseLookup = true, LookupItems = lookupItems };

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
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            return null;
        if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
            return parsed;
        return Guid.TryParse(value.ToString(), out parsed) ? parsed : null;
    }

    private static DateTime? ReadDate(JsonElement payload, string name)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            return null;
        if (value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var parsed))
            return parsed;
        return DateTime.TryParse(value.ToString(), out parsed) ? parsed : null;
    }

    private static decimal ReadDecimal(JsonElement payload, string name, decimal fallback = 0m)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            return number;
        if (decimal.TryParse(value.ToString(), out number))
            return number;
        return fallback;
    }

    private static int ReadInt(JsonElement payload, string name, int fallback = 0)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;
        return int.TryParse(value.ToString(), out number) ? number : fallback;
    }

    private static int? ReadNullableInt(JsonElement payload, string name)
    {
        if (!TryGetPropertyInsensitive(payload, name, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;
        return int.TryParse(value.ToString(), out number) ? number : null;
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

    private static bool TryGetPropertyInsensitive(JsonElement payload, string name, out JsonElement value)
    {
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
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(content) ? $"Error HTTP {(int)response.StatusCode}." : content);
    }

    private sealed class AttendancePunchDto
    {
        public Guid AttendancePunchId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? EmployeeId { get; set; }
        public DateTime WorkDate { get; set; }
        public DateTime PunchDateTime { get; set; }
        public string PunchType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceSerial { get; set; } = string.Empty;
        public string ExternalReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PayrollRecurringMovementDto
    {
        public Guid PayrollRecurringMovementId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? PayrollConceptId { get; set; }
        public string MovementCode { get; set; } = string.Empty;
        public string MovementName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public string CalculationMode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public DateTime EffectiveStartDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public bool ApplyEveryRun { get; set; }
        public int? DayOfPeriod { get; set; }
        public bool IsProrated { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class EmployeeLoanDto
    {
        public Guid EmployeeLoanId { get; set; }
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public Guid? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public Guid? PayrollConceptId { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public DateTime LoanDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public int Installments { get; set; }
        public int InstallmentsPaid { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class EmployeeLoanDeductionDto
    {
        public Guid EmployeeLoanDeductionId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? EmployeeLoanId { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? PayrollPeriodId { get; set; }
        public Guid? PayrollRunId { get; set; }
        public DateTime DeductionDate { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal PrincipalApplied { get; set; }
        public decimal InterestApplied { get; set; }
        public decimal RemainingBalance { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class AttendanceDailySummaryDto
    {
        public Guid AttendanceDailySummaryId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? PayrollPeriodId { get; set; }
        public DateTime WorkDate { get; set; }
        public DateTime? ScheduledEntryTime { get; set; }
        public DateTime? ScheduledExitTime { get; set; }
        public DateTime? FirstPunchDateTime { get; set; }
        public DateTime? LastPunchDateTime { get; set; }
        public decimal WorkedHours { get; set; }
        public int DelayMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal AbsenceUnits { get; set; }
        public string DayType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PrePayrollAdjustmentDto
    {
        public Guid PrePayrollAdjustmentId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? PayrollPeriodId { get; set; }
        public Guid? PayrollConceptId { get; set; }
        public string AdjustmentCode { get; set; } = string.Empty;
        public string AdjustmentName { get; set; } = string.Empty;
        public string AdjustmentType { get; set; } = string.Empty;
        public string CaptureSource { get; set; } = string.Empty;
        public DateTime ReferenceDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal ExemptAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PrePayrollCutoffDto
    {
        public Guid PrePayrollCutoffId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? PayrollPeriodId { get; set; }
        public string CutoffCode { get; set; } = string.Empty;
        public string CutoffName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int EmployeesReviewed { get; set; }
        public int IncidentsDetected { get; set; }
        public decimal WorkedDaysTotal { get; set; }
        public decimal OvertimeHoursTotal { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsClosed { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PayrollSourceApplicationDto
    {
        public Guid PayrollSourceApplicationId { get; set; }
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public Guid? PayrollRunId { get; set; }
        public string PayrollRunFolio { get; set; } = string.Empty;
        public Guid? PayrollRunLineId { get; set; }
        public Guid? PayrollPeriodId { get; set; }
        public string PayrollPeriodName { get; set; } = string.Empty;
        public Guid? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public Guid? PayrollConceptId { get; set; }
        public string PayrollConceptName { get; set; } = string.Empty;
        public Guid? SourceId { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string ApplicationCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal ExemptAmount { get; set; }
        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PayrollReceiptControlDto
    {
        public Guid PayrollReceiptControlId { get; set; }
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public Guid? PayrollRunId { get; set; }
        public string PayrollRunFolio { get; set; } = string.Empty;
        public Guid? PayrollRunLineId { get; set; }
        public Guid? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string ReceiptStatus { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? StampedAt { get; set; }
        public string DeliveryChannel { get; set; } = string.Empty;
        public string DeliveryReference { get; set; } = string.Empty;
        public string AckBy { get; set; } = string.Empty;
        public decimal NetAmount { get; set; }
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PayrollRunClosingDto
    {
        public Guid PayrollRunClosingId { get; set; }
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public Guid? PayrollRunId { get; set; }
        public string PayrollRunFolio { get; set; } = string.Empty;
        public string ClosingCode { get; set; } = string.Empty;
        public DateTime ClosingDate { get; set; }
        public int EmployeesIncluded { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DeductionsAmount { get; set; }
        public decimal NetAmount { get; set; }
        public int SourceApplicationsCount { get; set; }
        public int ReceiptsGeneratedCount { get; set; }
        public int IssuesDetected { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public DateTime? LockedAt { get; set; }
        public string ClosedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }


    private sealed class PayrollTaxAccumulatorDto
    {
        public Guid PayrollTaxAccumulatorId { get; set; }
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public Guid? PayrollRunId { get; set; }
        public string PayrollRunFolio { get; set; } = string.Empty;
        public Guid? PayrollRunLineId { get; set; }
        public Guid? PayrollPeriodId { get; set; }
        public string PayrollPeriodName { get; set; } = string.Empty;
        public Guid? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string AccumulatorCode { get; set; } = string.Empty;
        public string AccumulatorName { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public int FiscalMonth { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal ExemptAmount { get; set; }
        public decimal WithheldIsr { get; set; }
        public decimal SubsidyApplied { get; set; }
        public decimal SocialSecurityBase { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime LastCalculatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PayrollEmployerObligationDto
    {
        public Guid PayrollEmployerObligationId { get; set; }
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public Guid? PayrollRunId { get; set; }
        public string PayrollRunFolio { get; set; } = string.Empty;
        public Guid? PayrollPeriodId { get; set; }
        public string PayrollPeriodName { get; set; } = string.Empty;
        public string ObligationCode { get; set; } = string.Empty;
        public string ObligationName { get; set; } = string.Empty;
        public string ObligationType { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public int FiscalMonth { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal Amount { get; set; }
        public int EmployeesCount { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class PayrollFiscalReconciliationDto
    {
        public Guid PayrollFiscalReconciliationId { get; set; }
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public Guid? PayrollRunId { get; set; }
        public string PayrollRunFolio { get; set; } = string.Empty;
        public Guid? PayrollPeriodId { get; set; }
        public string PayrollPeriodName { get; set; } = string.Empty;
        public Guid? PayrollDispersionBatchId { get; set; }
        public Guid? PayrollAccountingPostingId { get; set; }
        public string ReconciliationCode { get; set; } = string.Empty;
        public int FiscalYear { get; set; }
        public int FiscalMonth { get; set; }
        public int ReceiptsStampedCount { get; set; }
        public int DispersionValidatedCount { get; set; }
        public int AccountingPostedCount { get; set; }
        public int TaxAccumulatorsCount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal WithheldIsrAmount { get; set; }
        public decimal EmployerTaxesAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal DifferenceAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ReconciledAt { get; set; }
        public string ClosedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class CompanyLookupDto { public Guid CompanyId { get; set; } public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
    private sealed class BranchLookupDto { public Guid BranchId { get; set; } public string Name { get; set; } = string.Empty; public string CompanyName { get; set; } = string.Empty; public bool IsActive { get; set; } }
    private sealed class EmployeeLookupDto { public Guid EmployeeId { get; set; } public string EmployeeNumber { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public bool IsActive { get; set; } }
    private sealed class PayrollConceptLookupDto { public Guid PayrollConceptId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
    private sealed class PayrollPeriodLookupDto { public Guid PayrollPeriodId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public DateTime StartDate { get; set; } public bool IsActive { get; set; } }
    private sealed class PayrollRunLookupDto { public Guid PayrollRunId { get; set; } public string Folio { get; set; } = string.Empty; public string PayrollPeriodName { get; set; } = string.Empty; public DateTime RunDate { get; set; } public bool IsActive { get; set; } }
}
