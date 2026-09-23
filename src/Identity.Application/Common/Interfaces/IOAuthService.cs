using Identity.Application.DTOs;
using Identity.Domain.Enums;

namespace Identity.Application.Common.Interfaces;

public interface IOAuthService
{
    string GetAuthorizationUrl(AuthProvider provider, string? state = null);
    Task<OAuthUserInfoDto> AuthenticateCodeAsync(AuthProvider provider, string code, string? redirectUri = null, CancellationToken cancellationToken = default);
}

