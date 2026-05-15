using System.ComponentModel.DataAnnotations;
using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialSupplierCostHistory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid MaterialSupplierAssignmentId { get; set; }
    public Guid? CurrencyId { get; set; }
    public DateTime CostDate { get; set; } = DateTime.UtcNow.Date;
    public decimal Cost { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;

    [MaxLength(40)]
    public string SourceDocumentType { get; set; } = string.Empty;

    public Guid? SourceDocumentId { get; set; }

    [MaxLength(80)]
    public string SourceDocumentNumber { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string Notes { get; set; } = string.Empty;

    public MaterialSupplierAssignment? MaterialSupplierAssignment { get; set; }
    public Currency? Currency { get; set; }
}
