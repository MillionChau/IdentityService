using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class Menu : AuditableEntity, IAggregateRoot
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public Guid? ParentId { get; set; }
    public Menu? Parent { get; set; }
    public int Order { get; set; } = 0;
    public string? RequiredPermission { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Menu> Children { get; set; } = new List<Menu>();
}
