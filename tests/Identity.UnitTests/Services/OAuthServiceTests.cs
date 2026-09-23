using Identity.Domain.Enums;
using Identity.Infrastructure.Services;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Identity.UnitTests.Services;

public class OAuthServiceTests
{
    [Fact]
    public void GetAuthorizationUrl_GitHub_ShouldReturnCorrectUrl()
    {
        var settings = new OAuthSettings();
        settings.GitHub.ClientId = "test_gh_client";
        settings.GitHub.RedirectUri = "http://localhost:5002/api/v1/auth/oauth/github/callback";

        var service = new OAuthService(Options.Create(settings), new HttpClient(), NullLogger<OAuthService>.Instance);
        var url = service.GetAuthorizationUrl(AuthProvider.Github, "state_xyz");

        Assert.Contains("https://github.com/login/oauth/authorize", url);
        Assert.Contains("client_id=test_gh_client", url);
        Assert.Contains("state=state_xyz", url);
        Assert.Contains("scope=read%3Auser%20user%3Aemail", url);
    }

    [Fact]
    public void GetAuthorizationUrl_Google_ShouldReturnCorrectUrl()
    {
        var settings = new OAuthSettings();
        settings.Google.ClientId = "test_google_client";
        settings.Google.RedirectUri = "http://localhost:5002/api/v1/auth/oauth/google/callback";

        var service = new OAuthService(Options.Create(settings), new HttpClient(), NullLogger<OAuthService>.Instance);
        var url = service.GetAuthorizationUrl(AuthProvider.Google, "google_state_123");

        Assert.Contains("https://accounts.google.com/o/oauth2/v2/auth", url);
        Assert.Contains("client_id=test_google_client", url);
        Assert.Contains("state=google_state_123", url);
        Assert.Contains("response_type=code", url);
    }

    [Fact]
    public async Task AuthenticateCodeAsync_InMockMode_ShouldReturnSimulatedUserInfo()
    {
        var settings = new OAuthSettings();
        settings.GitHub.ClientId = "mock_client";

        var service = new OAuthService(Options.Create(settings), new HttpClient(), NullLogger<OAuthService>.Instance);
        var userInfo = await service.AuthenticateCodeAsync(AuthProvider.Github, "mock_auth_code_123");

        Assert.NotNull(userInfo);
        Assert.Equal(AuthProvider.Github, userInfo.Provider);
        Assert.NotEmpty(userInfo.ProviderKey);
        Assert.Contains("mock", userInfo.Email);
    }
}

