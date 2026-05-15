using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class NavigationItem : BaseEntity
{
    public string Module { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Route { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public NavigationItem? Parent { get; set; }
    public int SortOrder { get; set; }
    public string? RequiredPermission { get; set; }
    public bool IsVisible { get; set; } = true;
}