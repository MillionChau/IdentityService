using Identity.Infrastructure.Services;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Identity.UnitTests.Services;

public class Wso2ServiceTests
{
    [Fact]
    public void GetAuthorizeUrl_ShouldReturnCorrectFormattedUrl()
    {
        var settings = new Wso2Settings
        {
            Enabled = true,
            BaseUrl = "https://identity.devradar.io:9443",
            ClientId = "devradar-app",
            RedirectUri = "http://localhost:5002/api/v1/auth/wso2/callback",
            Scope = "openid profile"
        };

        var service = new Wso2Service(Options.Create(settings), new HttpClient(), NullLogger<Wso2Service>.Instance);
        var url = service.GetAuthorizeUrl("test_state_123");

        Assert.Contains("https://identity.devradar.io:9443/oauth2/authorize", url);
        Assert.Contains("client_id=devradar-app", url);
        Assert.Contains("state=test_state_123", url);
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WhenDisabled_ShouldReturnSimulatedTokens()
    {
        var settings = new Wso2Settings { Enabled = false };
        var service = new Wso2Service(Options.Create(settings), new HttpClient(), NullLogger<Wso2Service>.Instance);

        var result = await service.ExchangeCodeForTokenAsync("any_code_123");

        Assert.NotNull(result);
        Assert.StartsWith("mock_wso2_access_", result.AccessToken);
        Assert.StartsWith("mock_wso2_refresh_", result.RefreshToken!);
    }
}
