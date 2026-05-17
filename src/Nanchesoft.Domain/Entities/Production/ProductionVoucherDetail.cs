using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionVoucherDetail : BaseEntity
{
    public Guid ProductionVoucherId { get; set; }
    public ProductionVoucher? ProductionVoucher { get; set; }

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? SizeRunSizeId { get; set; }
    public ProductSizeRunSize? SizeRunSize { get; set; }

    public int QuantityAssigned { get; set; }
    public int QuantityProduced { get; set; }
    public int QuantityRejected { get; set; }

    public string OperationCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
