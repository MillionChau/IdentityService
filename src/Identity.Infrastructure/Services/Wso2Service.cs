using System.Net.Http.Headers;
using System.Text.Json;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Services;

public class Wso2Service : IWso2Service
{
    private readonly Wso2Settings _wso2Settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<Wso2Service> _logger;

    public Wso2Service(
        IOptions<Wso2Settings> wso2Settings,
        HttpClient httpClient,
        ILogger<Wso2Service> logger)
    {
        _wso2Settings = wso2Settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsWso2Enabled() => _wso2Settings.Enabled && !string.IsNullOrWhiteSpace(_wso2Settings.BaseUrl);

    public string GetAuthorizeUrl(string? state = null)
    {
        var stateParam = string.IsNullOrWhiteSpace(state) ? Guid.NewGuid().ToString() : state;
        var baseUrl = _wso2Settings.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/oauth2/authorize?response_type=code&client_id={Uri.EscapeDataString(_wso2Settings.ClientId)}&redirect_uri={Uri.EscapeDataString(_wso2Settings.RedirectUri)}&scope={Uri.EscapeDataString(_wso2Settings.Scope)}&state={Uri.EscapeDataString(stateParam)}";
    }

    public async Task<Wso2TokenDto> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!IsWso2Enabled())
        {
            _logger.LogInformation("WSO2 is disabled. Returning simulated token for code: {Code}", code);
            return new Wso2TokenDto
            {
                AccessToken = $"mock_wso2_access_{Guid.NewGuid():N}",
                RefreshToken = $"mock_wso2_refresh_{Guid.NewGuid():N}",
                IdToken = $"mock_wso2_id_{Guid.NewGuid():N}",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = _wso2Settings.Scope
            };
        }

        try
        {
            var tokenUrl = $"{_wso2Settings.BaseUrl.TrimEnd('/')}/oauth2/token";
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            var authBytes = System.Text.Encoding.UTF8.GetBytes($"{_wso2Settings.ClientId}:{_wso2Settings.ClientSecret}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var formParams = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _wso2Settings.RedirectUri }
            };

            request.Content = new FormUrlEncodedContent(formParams);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("WSO2 Token Exchange failed with status {Status}: {Content}", response.StatusCode, content);
                throw new BadRequestException($"Lỗi xác thực với WSO2: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            return new Wso2TokenDto
            {
                AccessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() ?? string.Empty : string.Empty,
                RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                IdToken = root.TryGetProperty("id_token", out var it) ? it.GetString() : null,
                TokenType = root.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "Bearer" : "Bearer",
                ExpiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600,
                Scope = root.TryGetProperty("scope", out var sc) ? sc.GetString() : null
            };
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            _logger.LogError(ex, "Error communicating with WSO2");
            throw new BadRequestException("Không thể kết nối đến máy chủ xác thực WSO2.");
        }
    }

    public async Task<Wso2TokenDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!IsWso2Enabled())
        {
            return new Wso2TokenDto
            {
                AccessToken = $"mock_renewed_access_{Guid.NewGuid():N}",
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = 3600
            };
        }

        try
        {
            var tokenUrl = $"{_wso2Settings.BaseUrl.TrimEnd('/')}/oauth2/token";
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            var authBytes = System.Text.Encoding.UTF8.GetBytes($"{_wso2Settings.ClientId}:{_wso2Settings.ClientSecret}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var formParams = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            request.Content = new FormUrlEncodedContent(formParams);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new BadRequestException($"Không thể gia hạn phiên WSO2: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            return new Wso2TokenDto
            {
                AccessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() ?? string.Empty : string.Empty,
                RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : refreshToken,
                IdToken = root.TryGetProperty("id_token", out var it) ? it.GetString() : null,
                TokenType = "Bearer",
                ExpiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600
            };
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            _logger.LogError(ex, "Error refreshing WSO2 token");
            throw new BadRequestException("Không thể gia hạn phiên đăng nhập WSO2.");
        }
    }

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!IsWso2Enabled()) return;

        try
        {
            var revokeUrl = $"{_wso2Settings.BaseUrl.TrimEnd('/')}/oauth2/revoke";
            var request = new HttpRequestMessage(HttpMethod.Post, revokeUrl);

            var authBytes = System.Text.Encoding.UTF8.GetBytes($"{_wso2Settings.ClientId}:{_wso2Settings.ClientSecret}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var formParams = new Dictionary<string, string>
            {
                { "token", token }
            };

            request.Content = new FormUrlEncodedContent(formParams);
            await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke token on WSO2");
        }
    }
}
