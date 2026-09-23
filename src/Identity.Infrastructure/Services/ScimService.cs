using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Services;

public class ScimService : IScimService
{
    private readonly Wso2Settings _wso2Settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ScimService> _logger;

    public ScimService(
        IOptions<Wso2Settings> wso2Settings,
        HttpClient httpClient,
        ILogger<ScimService> logger)
    {
        _wso2Settings = wso2Settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    private string GetScimBaseUrl()
    {
        var scim = string.IsNullOrWhiteSpace(_wso2Settings.ScimUri)
            ? $"{_wso2Settings.BaseUrl.TrimEnd('/')}/scim2"
            : _wso2Settings.ScimUri.TrimEnd('/');
        return scim;
    }

    private void AddScimAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_wso2Settings.AuthToken))
        {
            request.Headers.Add("Authorization", "Basic " + _wso2Settings.AuthToken);
        }
    }

    public async Task<string?> SearchUserAsync(string filter, CancellationToken cancellationToken = default)
    {
        if (!_wso2Settings.Enabled) return null;

        try
        {
            var url = $"{GetScimBaseUrl()}/Users?filter={Uri.EscapeDataString(filter)}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddScimAuthHeader(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SCIM SearchUserAsync failed");
        }

        return null;
    }

    public async Task CreateUserAsync(User user, string? plainPassword = null, CancellationToken cancellationToken = default)
    {
        if (!_wso2Settings.Enabled) return;

        try
        {
            var url = $"{GetScimBaseUrl()}/Users";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            AddScimAuthHeader(request);

            var scimPayload = new
            {
                schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                userName = user.UserName,
                password = plainPassword ?? "DevRadarDefault@123",
                name = new
                {
                    givenName = user.FirstName ?? user.UserName,
                    familyName = user.LastName ?? string.Empty
                },
                emails = new[]
                {
                    new { value = user.Email, primary = true, type = "work" }
                },
                externalId = user.Id.ToString(),
                active = user.Status == UserStatus.Active
            };

            request.Content = new StringContent(JsonSerializer.Serialize(scimPayload), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("SCIM CreateUserAsync returned {Status}: {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SCIM CreateUserAsync failed for user {UserName}", user.UserName);
        }
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!_wso2Settings.Enabled) return;

        try
        {
            // Search user first
            var searchResult = await SearchUserAsync($"userName eq \\\"{user.UserName}\\\"", cancellationToken);
            if (string.IsNullOrEmpty(searchResult)) return;

            using var doc = JsonDocument.Parse(searchResult);
            var root = doc.RootElement;
            if (root.TryGetProperty("Resources", out var resources) && resources.GetArrayLength() > 0)
            {
                var scimUserId = resources[0].GetProperty("id").GetString();
                if (!string.IsNullOrEmpty(scimUserId))
                {
                    var url = $"{GetScimBaseUrl()}/Users/{scimUserId}";
                    var request = new HttpRequestMessage(HttpMethod.Put, url);
                    AddScimAuthHeader(request);

                    var scimPayload = new
                    {
                        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                        userName = user.UserName,
                        name = new
                        {
                            givenName = user.FirstName ?? user.UserName,
                            familyName = user.LastName ?? string.Empty
                        },
                        emails = new[]
                        {
                            new { value = user.Email, primary = true, type = "work" }
                        },
                        phoneNumbers = string.IsNullOrWhiteSpace(user.PhoneNumber)
                            ? Array.Empty<object>()
                            : new object[] { new { value = user.PhoneNumber, type = "mobile" } },
                        externalId = user.Id.ToString(),
                        active = user.Status == UserStatus.Active
                    };

                    request.Content = new StringContent(JsonSerializer.Serialize(scimPayload), Encoding.UTF8, "application/json");
                    await _httpClient.SendAsync(request, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SCIM UpdateUserAsync failed for user {UserName}", user.UserName);
        }
    }

    public async Task DeleteUserAsync(string scimUserId, CancellationToken cancellationToken = default)
    {
        if (!_wso2Settings.Enabled || string.IsNullOrWhiteSpace(scimUserId)) return;

        try
        {
            var url = $"{GetScimBaseUrl()}/Users/{scimUserId}";
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            AddScimAuthHeader(request);

            await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SCIM DeleteUserAsync failed for ID {Id}", scimUserId);
        }
    }
}
