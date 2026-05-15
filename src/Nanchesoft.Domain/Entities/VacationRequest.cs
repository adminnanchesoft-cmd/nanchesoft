using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class VacationRequest : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }

    public string Folio { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public decimal RequestedDays { get; set; }
    public decimal ApprovedDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}
