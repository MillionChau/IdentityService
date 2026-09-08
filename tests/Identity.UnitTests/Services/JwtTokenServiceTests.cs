using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Services;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Identity.UnitTests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly JwtTokenService _tokenService;

    public JwtTokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "DevRadarSecretKey_SuperSecureKeyForJWTTokenGeneration_2026_SecureApp",
            Issuer = "DevRadarIdentityService",
            Audience = "DevRadarClients",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 30
        };

        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _tokenService = new JwtTokenService(
            Options.Create(_jwtSettings),
            _refreshTokenRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GenerateTokensAsync_ShouldReturnValidAuthResponse()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testdeveloper",
            Email = "dev@devradar.io",
            FullName = "Test Developer",
            Status = UserStatus.Active,
            PrimaryProvider = AuthProvider.Local
        };

        var roles = new[] { "Developer", "Admin" };
        var permissions = new[] { "repository.read", "metrics.view" };

        var result = await _tokenService.GenerateTokensAsync(user, roles, permissions);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(user.UserName, result.User.UserName);
        Assert.Equal(user.Email, result.User.Email);
        Assert.Contains("Developer", result.User.Roles);
        Assert.Contains("repository.read", result.User.Permissions);

        _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_ShouldExtractClaims()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "expuser",
            Email = "exp@devradar.io",
            Status = UserStatus.Active
        };

        var tokens = await _tokenService.GenerateTokensAsync(user, new[] { "User" }, Array.Empty<string>());
        var principal = _tokenService.GetPrincipalFromExpiredToken(tokens.AccessToken);

        Assert.NotNull(principal);
        Assert.Equal(user.Id.ToString(), principal.FindFirst("user_id")?.Value);
    }
}
