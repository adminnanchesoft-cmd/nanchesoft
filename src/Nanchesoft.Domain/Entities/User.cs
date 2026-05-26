using Nanchesoft.Domain.Common;
using Nanchesoft.Domain.Enums;

namespace Nanchesoft.Domain.Entities;

public sealed class User : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool MustChangePassword { get; set; } = true;
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public Guid? EmployeeId { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public string GetDisplayName() => $"{FirstName} {LastName}".Trim();
}