using System.Security.Claims;
using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces;

public interface IJwtTokenService
{
    Task<AuthResponseDto> GenerateTokensAsync(User user, IEnumerable<string> roles, IEnumerable<string> permissions, CancellationToken cancellationToken = default);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
