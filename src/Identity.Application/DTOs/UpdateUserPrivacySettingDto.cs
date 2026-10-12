namespace Identity.Application.DTOs;

public class UpdateUserPrivacySettingDto
{
    public bool IsProfilePublic { get; set; } = true;
    public bool ShowEmail { get; set; } = false;
    public bool ShowPhoneNumber { get; set; } = false;
    public bool ShowGitHubStats { get; set; } = true;
    public bool ShowActivityStats { get; set; } = true;
}
