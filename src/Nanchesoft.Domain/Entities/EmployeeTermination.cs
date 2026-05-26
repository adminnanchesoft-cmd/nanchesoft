using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class EmployeeTermination : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    // "voluntary" | "justified" | "unjustified" | "restructuring"
    public string TerminationType { get; set; } = "voluntary";
    public DateTime TerminationDate { get; set; }

    // Salarios al momento del cálculo
    public decimal DailySalary { get; set; }
    public decimal IntegratedDailySalary { get; set; }

    // Antigüedad
    public decimal YearsOfService { get; set; }
    public decimal DaysOfService { get; set; }

    // Vacaciones
    public decimal AnnualVacationDays { get; set; }    // días por tabla LFT según antigüedad
    public decimal VacationDaysTaken { get; set; }
    public decimal ProportionalVacationDays { get; set; }
    public decimal VacationPremiumPercent { get; set; } = 25;

    // Aguinaldo proporcional
    public decimal ProportionalChristmasBonusDays { get; set; }

    // Prima de antigüedad (12 días/año, aplica siempre en despido o renuncia ≥15 años)
    public decimal SeniorityPremiumDays { get; set; }
    public decimal SeniorityPremiumDailyCap { get; set; }

    // Indemnización constitucional (solo despido injustificado o reestructura)
    public decimal IndemnizationDays { get; set; }     // 90 días
    public decimal SeniorityBonusDays { get; set; }    // 20 días × años

    // Montos calculados
    public decimal VacationAmount { get; set; }
    public decimal VacationPremiumAmount { get; set; }
    public decimal ChristmasBonusAmount { get; set; }
    public decimal SeniorityPremiumAmount { get; set; }
    public decimal IndemnizationAmount { get; set; }
    public decimal SeniorityBonusAmount { get; set; }
    public decimal TotalGross { get; set; }

    public string Notes { get; set; } = string.Empty;
    // "draft" | "approved" | "paid"
    public string Status { get; set; } = "draft";
    public bool IsDeleted { get; set; }
}
