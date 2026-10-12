namespace Identity.Application.DTOs;

/// <summary>
/// DTO hồ sơ người dùng trả về cho thành viên khác (UC-07), đã qua bộ lọc quyền riêng tư.
/// </summary>
public class PublicUserProfileDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public bool IsProfilePublic { get; set; } = true;
    public bool ShowGitHubStats { get; set; } = true;
    public bool ShowActivityStats { get; set; } = true;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
