namespace Identity.Infrastructure.Settings;

public class OAuthProviderSettings
{
    public bool Enabled { get; set; } = true;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string UserInformationEndpoint { get; set; } = string.Empty;
}

public class OAuthSettings
{
    public const string SectionName = "OAuth";

    public OAuthProviderSettings GitHub { get; set; } = new()
    {
        Scope = "read:user user:email",
        AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
        TokenEndpoint = "https://github.com/login/oauth/access_token",
        UserInformationEndpoint = "https://api.github.com/user"
    };

    public OAuthProviderSettings Google { get; set; } = new()
    {
        Scope = "openid profile email",
        AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
        TokenEndpoint = "https://oauth2.googleapis.com/token",
        UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo"
    };
}

