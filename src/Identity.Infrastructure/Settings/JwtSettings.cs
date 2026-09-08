namespace Identity.Infrastructure.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = "DevRadarSecretKey_SuperSecureKeyForJWTTokenGeneration_2026";
    public string Issuer { get; set; } = "DevRadarIdentityService";
    public string Audience { get; set; } = "DevRadarClients";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
