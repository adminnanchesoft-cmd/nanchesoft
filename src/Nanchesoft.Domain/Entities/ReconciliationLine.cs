using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ReconciliationLine : BaseEntity
{
    public Guid ReconciliationId { get; set; }
    public Reconciliation? Reconciliation { get; set; }
    public Guid BankMovementId { get; set; }
    public BankMovement? BankMovement { get; set; }
    public bool IsChecked { get; set; }
    public decimal MovementAmount { get; set; }
}
