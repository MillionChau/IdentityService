using Identity.Domain.Common;
using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

public class UserExternalLogin : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public AuthProvider Provider { get; set; }
    public string ProviderKey { get; set; } = string.Empty;
    public string? ProviderDisplayName { get; set; }
}
