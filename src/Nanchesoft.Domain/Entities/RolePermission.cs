using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedBy { get; set; }
}