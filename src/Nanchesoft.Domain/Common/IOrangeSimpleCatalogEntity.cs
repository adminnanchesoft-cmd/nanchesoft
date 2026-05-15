namespace Nanchesoft.Domain.Common;

public interface IOrangeSimpleCatalogEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid CompanyId { get; set; }
    string Code { get; set; }
    string Name { get; set; }
    string Description { get; set; }
    int Sequence { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
