using System.Net.Http.Headers;
using System.Text.Json;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Enums;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Services;

public class OAuthService : IOAuthService
{
    private readonly OAuthSettings _oauthSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OAuthService> _logger;

    public OAuthService(
        IOptions<OAuthSettings> oauthSettings,
        HttpClient httpClient,
        ILogger<OAuthService> logger)
    {
        _oauthSettings = oauthSettings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    private OAuthProviderSettings GetProviderConfig(AuthProvider provider)
    {
        return provider switch
        {
            AuthProvider.Github => _oauthSettings.GitHub,
            AuthProvider.Google => _oauthSettings.Google,
            _ => throw new BadRequestException($"Nhà cung cấp OAuth '{provider}' không được hỗ trợ.")
        };
    }

    public string GetAuthorizationUrl(AuthProvider provider, string? state = null)
    {
        var config = GetProviderConfig(provider);
        var stateParam = string.IsNullOrWhiteSpace(state) ? Guid.NewGuid().ToString("N") : state;
        var redirectUri = !string.IsNullOrWhiteSpace(config.RedirectUri) ? config.RedirectUri : string.Empty;

        return provider switch
        {
            AuthProvider.Github =>
                $"{config.AuthorizationEndpoint}?client_id={Uri.EscapeDataString(config.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(config.Scope)}" +
                $"&state={Uri.EscapeDataString(stateParam)}",

            AuthProvider.Google =>
                $"{config.AuthorizationEndpoint}?response_type=code" +
                $"&client_id={Uri.EscapeDataString(config.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(config.Scope)}" +
                $"&state={Uri.EscapeDataString(stateParam)}" +
                "&access_type=offline&prompt=consent",

            _ => throw new BadRequestException($"Nhà cung cấp OAuth '{provider}' không được hỗ trợ.")
        };
    }

    public async Task<OAuthUserInfoDto> AuthenticateCodeAsync(
        AuthProvider provider,
        string code,
        string? redirectUri = null,
        CancellationToken cancellationToken = default)
    {
        var config = GetProviderConfig(provider);
        var effectiveRedirectUri = !string.IsNullOrWhiteSpace(redirectUri) ? redirectUri : config.RedirectUri;

        // Chế độ Mock dự phòng khi chưa cấu hình ClientId thực hoặc chạy trong môi trường test/dev offline
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.ClientId) || config.ClientId.StartsWith("mock_"))
        {
            _logger.LogInformation("OAuth provider {Provider} is running in simulated mock mode for code {Code}", provider, code);
            var mockKey = $"mock_{provider.ToString().ToLower()}_{Guid.NewGuid():N}".Substring(0, 16);
            return new OAuthUserInfoDto
            {
                Provider = provider,
                ProviderKey = mockKey,
                Username = $"mock_{provider.ToString().ToLower()}_user",
                Email = $"mock_{mockKey}@{provider.ToString().ToLower()}.devradar.test",
                FullName = $"Mock {provider} Developer",
                AvatarUrl = "https://avatars.githubusercontent.com/u/9919?v=4",
                AccessToken = $"mock_token_{Guid.NewGuid():N}"
            };
        }

        return provider switch
        {
            AuthProvider.Github => await AuthenticateGithubAsync(config, code, effectiveRedirectUri, cancellationToken),
            AuthProvider.Google => await AuthenticateGoogleAsync(config, code, effectiveRedirectUri, cancellationToken),
            _ => throw new BadRequestException($"Nhà cung cấp OAuth '{provider}' không được hỗ trợ.")
        };
    }

    private async Task<OAuthUserInfoDto> AuthenticateGithubAsync(
        OAuthProviderSettings config,
        string code,
        string? redirectUri,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Đổi code lấy Access Token
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint);
            tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            tokenRequest.Headers.UserAgent.ParseAdd("DevRadar-IdentityService");

            var tokenParams = new Dictionary<string, string>
            {
                { "client_id", config.ClientId },
                { "client_secret", config.ClientSecret },
                { "code", code }
            };
            if (!string.IsNullOrWhiteSpace(redirectUri))
            {
                tokenParams.Add("redirect_uri", redirectUri);
            }

            tokenRequest.Content = new FormUrlEncodedContent(tokenParams);
            var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub token exchange failed: {Content}", tokenContent);
                throw new BadRequestException("Không thể xác thực mã code với GitHub.");
            }

            using var tokenDoc = JsonDocument.Parse(tokenContent);
            if (!tokenDoc.RootElement.TryGetProperty("access_token", out var atElement))
            {
                var errorDesc = tokenDoc.RootElement.TryGetProperty("error_description", out var ed)
                    ? ed.GetString() : "Lỗi xác thực GitHub";
                throw new BadRequestException($"Lỗi từ GitHub: {errorDesc}");
            }
            var accessToken = atElement.GetString()!;

            // 2. Lấy thông tin user profile từ GitHub
            var userRequest = new HttpRequestMessage(HttpMethod.Get, config.UserInformationEndpoint);
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            userRequest.Headers.UserAgent.ParseAdd("DevRadar-IdentityService");

            var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
            var userContent = await userResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub user info fetch failed: {Content}", userContent);
                throw new BadRequestException("Không thể lấy thông tin người dùng từ GitHub.");
            }

            using var userDoc = JsonDocument.Parse(userContent);
            var root = userDoc.RootElement;

            var id = root.GetProperty("id").GetInt64().ToString();
            var login = root.GetProperty("login").GetString() ?? $"github_{id}";
            var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
            var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
            var avatarUrl = root.TryGetProperty("avatar_url", out var av) ? av.GetString() : null;

            // 3. Nếu email null (do private), gọi thêm API /user/emails
            if (string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                    emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    emailsRequest.Headers.UserAgent.ParseAdd("DevRadar-IdentityService");

                    var emailsResponse = await _httpClient.SendAsync(emailsRequest, cancellationToken);
                    if (emailsResponse.IsSuccessStatusCode)
                    {
                        var emailsContent = await emailsResponse.Content.ReadAsStringAsync(cancellationToken);
                        using var emailsDoc = JsonDocument.Parse(emailsContent);
                        foreach (var item in emailsDoc.RootElement.EnumerateArray())
                        {
                            var isPrimary = item.TryGetProperty("primary", out var pr) && pr.GetBoolean();
                            var isVerified = item.TryGetProperty("verified", out var vf) && vf.GetBoolean();
                            if (isPrimary && isVerified && item.TryGetProperty("email", out var em))
                            {
                                email = em.GetString();
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch private email from GitHub");
                }
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                email = $"{login}@users.noreply.github.com";
            }

            return new OAuthUserInfoDto
            {
                Provider = AuthProvider.Github,
                ProviderKey = id,
                Username = login,
                FullName = name ?? login,
                Email = email,
                AvatarUrl = avatarUrl,
                AccessToken = accessToken
            };
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            _logger.LogError(ex, "Error during GitHub OAuth authentication");
            throw new BadRequestException("Không thể kết nối đến máy chủ xác thực GitHub.");
        }
    }

    private async Task<OAuthUserInfoDto> AuthenticateGoogleAsync(
        OAuthProviderSettings config,
        string code,
        string? redirectUri,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Đổi code lấy Access Token
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint);
            var tokenParams = new Dictionary<string, string>
            {
                { "client_id", config.ClientId },
                { "client_secret", config.ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", !string.IsNullOrWhiteSpace(redirectUri) ? redirectUri : config.RedirectUri }
            };

            tokenRequest.Content = new FormUrlEncodedContent(tokenParams);
            var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Google token exchange failed: {Content}", tokenContent);
                throw new BadRequestException("Không thể xác thực mã code với Google.");
            }

            using var tokenDoc = JsonDocument.Parse(tokenContent);
            if (!tokenDoc.RootElement.TryGetProperty("access_token", out var atElement))
            {
                throw new BadRequestException("Không tìm thấy access_token từ Google.");
            }
            var accessToken = atElement.GetString()!;

            // 2. Lấy thông tin user profile từ Google
            var userRequest = new HttpRequestMessage(HttpMethod.Get, config.UserInformationEndpoint);
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
            var userContent = await userResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Google user info fetch failed: {Content}", userContent);
                throw new BadRequestException("Không thể lấy thông tin người dùng từ Google.");
            }

            using var userDoc = JsonDocument.Parse(userContent);
            var root = userDoc.RootElement;

            var id = root.GetProperty("id").GetString()!;
            var email = root.GetProperty("email").GetString()!;
            var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
            var givenName = root.TryGetProperty("given_name", out var gn) ? gn.GetString() : null;
            var familyName = root.TryGetProperty("family_name", out var fn) ? fn.GetString() : null;
            var picture = root.TryGetProperty("picture", out var pic) ? pic.GetString() : null;

            var username = email.Split('@')[0];

            return new OAuthUserInfoDto
            {
                Provider = AuthProvider.Google,
                ProviderKey = id,
                Username = username,
                Email = email,
                FullName = name,
                FirstName = givenName,
                LastName = familyName,
                AvatarUrl = picture,
                AccessToken = accessToken
            };
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            _logger.LogError(ex, "Error during Google OAuth authentication");
            throw new BadRequestException("Không thể kết nối đến máy chủ xác thực Google.");
        }
    }
}

