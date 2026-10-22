namespace Identity.Infrastructure.Settings;

public class MinioStorageSettings
{
    public const string SectionName = "MinioSettings";

    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin2026";
    public bool UseSsl { get; set; } = false;
    public string AvatarBucket { get; set; } = "devradar-avatars";
    public string CoverBucket { get; set; } = "devradar-covers";
    public string PublicUrl { get; set; } = "http://localhost:9000";
}
