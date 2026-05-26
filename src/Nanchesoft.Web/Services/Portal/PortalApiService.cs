using System.Net.Http.Json;

namespace Nanchesoft.Web.Services.Portal;

public sealed class PortalApiService(IHttpClientFactory factory)
{
    private HttpClient Client => factory.CreateClient("Nanchesoft.Api");

    public async Task<PortalEmployeeDto?> GetMeAsync()
        => await Client.GetFromJsonAsync<PortalEmployeeDto>("/api/portal/me");

    public async Task<bool> LinkEmployeeAsync(Guid employeeId)
    {
        var r = await Client.PutAsync($"/api/portal/link-employee/{employeeId}", null);
        return r.IsSuccessStatusCode;
    }

    public async Task<List<PortalPayslipSummaryDto>> GetPayslipsAsync()
        => await Client.GetFromJsonAsync<List<PortalPayslipSummaryDto>>("/api/portal/payslips") ?? [];

    public async Task<PortalPayslipDetailDto?> GetPayslipDetailAsync(Guid runLineId)
        => await Client.GetFromJsonAsync<PortalPayslipDetailDto>($"/api/portal/payslips/{runLineId}");

    public async Task<List<PortalIncidentDto>> GetIncidentsAsync(Guid? periodId = null)
    {
        var url = "/api/portal/incidents";
        if (periodId.HasValue) url += $"?periodId={periodId.Value}";
        return await Client.GetFromJsonAsync<List<PortalIncidentDto>>(url) ?? [];
    }

    public async Task<PortalVacationBalanceDto?> GetVacationBalanceAsync()
        => await Client.GetFromJsonAsync<PortalVacationBalanceDto>("/api/portal/vacation-balance");

    public async Task<List<PortalVacationRequestDto>> GetVacationRequestsAsync()
        => await Client.GetFromJsonAsync<List<PortalVacationRequestDto>>("/api/portal/vacation-requests") ?? [];

    public async Task<bool> CreateVacationRequestAsync(DateTime start, DateTime end, string? notes)
    {
        var r = await Client.PostAsJsonAsync("/api/portal/vacation-requests",
            new { StartDate = start, EndDate = end, Notes = notes });
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> CancelVacationRequestAsync(Guid id)
    {
        var r = await Client.DeleteAsync($"/api/portal/vacation-requests/{id}");
        return r.IsSuccessStatusCode;
    }
}

public sealed class PortalEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
    public decimal DailySalary { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class PortalPayslipSummaryDto
{
    public Guid PayrollRunLineId { get; set; }
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? RunDate { get; set; }
    public decimal DaysPaid { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class PortalPayslipDetailDto
{
    public Guid PayrollRunLineId { get; set; }
    public string PayrollRunFolio { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal DaysPaid { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public List<PortalPayslipLineDto> Details { get; set; } = [];
}

public sealed class PortalPayslipLineDto
{
    public string ConceptCode { get; set; } = string.Empty;
    public string ConceptName { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PortalIncidentDto
{
    public Guid IncidentId { get; set; }
    public DateTime IncidentDate { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class PortalVacationBalanceDto
{
    public int FullYears { get; set; }
    public decimal AnnualDays { get; set; }
    public decimal ProportionalEarned { get; set; }
    public decimal DaysUsedThisYear { get; set; }
    public decimal DaysAvailable { get; set; }
    public DateTime HireDate { get; set; }
}

public sealed class PortalVacationRequestDto
{
    public Guid VacationRequestId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RequestedDays { get; set; }
    public decimal ApprovedDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
}
