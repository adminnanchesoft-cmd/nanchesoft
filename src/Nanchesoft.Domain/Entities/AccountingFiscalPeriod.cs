namespace Nanchesoft.Domain.Entities;

public class AccountingFiscalPeriod
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "open";
    public bool IsActive { get; set; } = true;
}
