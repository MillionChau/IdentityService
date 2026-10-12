namespace Identity.Application.DTOs;

public class UserPrivacySettingDto
{
    public Guid UserId { get; set; }
    public bool IsProfilePublic { get; set; }
    public bool ShowEmail { get; set; }
    public bool ShowPhoneNumber { get; set; }
    public bool ShowGitHubStats { get; set; }
    public bool ShowActivityStats { get; set; }
}
