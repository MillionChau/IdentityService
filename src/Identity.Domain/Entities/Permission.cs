using Identity.Domain.Common;
using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

public class Permission : AuditableEntity, IAggregateRoot
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
    public PermissionType Type { get; set; } = PermissionType.Role;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
