namespace Identity.Application.DTOs;

public class MenuDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; }
    public string? RequiredPermission { get; set; }
    public List<MenuDto> Children { get; set; } = new();
}
