using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Employee : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }

    public string Code { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public DateTime? BirthDate { get; set; }
    public decimal DailySalary { get; set; }
    public decimal IntegratedDailySalary { get; set; }
    public string Status { get; set; } = "active";

    public string GetFullName()
    {
        return string.Join(" ", new[] { FirstName, MiddleName, LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()));
    }
}
