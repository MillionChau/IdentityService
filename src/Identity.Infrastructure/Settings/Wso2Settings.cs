namespace Identity.Infrastructure.Settings;

public class Wso2Settings
{
    public const string SectionName = "WSO2";

    public bool Enabled { get; set; } = false;
    public string BaseUrl { get; set; } = "https://localhost:9443";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:5002/api/v1/auth/wso2/callback";
    public string Scope { get; set; } = "openid internal_login";
    public string AuthToken { get; set; } = string.Empty; // Basic auth token for SCIM
    public string ScimUri { get; set; } = "https://localhost:9443/scim2";
}
