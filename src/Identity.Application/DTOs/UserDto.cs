using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public UserStatus Status { get; set; }
    public AuthProvider PrimaryProvider { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
