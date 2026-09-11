using Identity.Application.DTOs;

namespace Identity.Application.Common.Interfaces;

public interface IWso2Service
{
    string GetAuthorizeUrl(string? state = null);
    Task<Wso2TokenDto> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);
    Task<Wso2TokenDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
    bool IsWso2Enabled();
}
