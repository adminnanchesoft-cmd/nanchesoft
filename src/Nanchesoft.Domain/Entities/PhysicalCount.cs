namespace Nanchesoft.Domain.Entities;

public class PhysicalCount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public DateTime CountDate { get; set; } = DateTime.UtcNow.Date;
    public string Status { get; set; } = "draft";
    public string Notes { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
    public List<PhysicalCountLine> Lines { get; set; } = [];
}
