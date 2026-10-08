using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

public class OAuthUserInfoDto
{
    public string ProviderKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public AuthProvider Provider { get; set; }
    public string? AccessToken { get; set; }
}

