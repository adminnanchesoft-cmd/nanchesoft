using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionCellEmployee : BaseEntity
{
    public Guid ProductionCellId { get; set; }
    public ProductionCell? ProductionCell { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string Role { get; set; } = "operator";
    public DateOnly AssignedDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
