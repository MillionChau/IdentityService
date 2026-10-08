using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Application.Features.Auth.Commands.OAuthLogin;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;
using Xunit;

namespace Identity.UnitTests.Features.Auth;

public class OAuthLoginCommandHandlerTests
{
    private readonly Mock<IOAuthService> _oauthServiceMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IPermissionRepository> _permissionRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly OAuthLoginCommandHandler _handler;

    public OAuthLoginCommandHandlerTests()
    {
        _oauthServiceMock = new Mock<IOAuthService>();
        _userRepoMock = new Mock<IUserRepository>();
        _jwtServiceMock = new Mock<IJwtTokenService>();
        _permissionRepoMock = new Mock<IPermissionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new OAuthLoginCommandHandler(
            _oauthServiceMock.Object,
            _userRepoMock.Object,
            _jwtServiceMock.Object,
            _permissionRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingExternalLogin_ShouldAuthenticateAndReturnTokens()
    {
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "gh_dev",
            Email = "gh_dev@devradar.io",
            Status = UserStatus.Active
        };

        _oauthServiceMock.Setup(s => s.AuthenticateCodeAsync(AuthProvider.Github, "valid_code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthUserInfoDto
            {
                Provider = AuthProvider.Github,
                ProviderKey = "123456",
                Email = "gh_dev@devradar.io",
                Username = "gh_dev"
            });

        _userRepoMock.Setup(r => r.GetByExternalLoginAsync(AuthProvider.Github, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _jwtServiceMock.Setup(j => j.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResponseDto { AccessToken = "oauth_access_token", RefreshToken = "oauth_refresh_token" });

        var command = new OAuthLoginCommand
        {
            Provider = "github",
            Code = "valid_code"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("oauth_access_token", result.AccessToken);
    }

    [Fact]
    public async Task Handle_NewOAuthUser_ShouldAutoRegisterAndReturnTokens()
    {
        _oauthServiceMock.Setup(s => s.AuthenticateCodeAsync(AuthProvider.Github, "new_user_code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthUserInfoDto
            {
                Provider = AuthProvider.Github,
                ProviderKey = "999888",
                Email = "new_oauth@devradar.io",
                Username = "new_oauth_dev",
                FullName = "New OAuth Developer"
            });

        _userRepoMock.Setup(r => r.GetByExternalLoginAsync(AuthProvider.Github, "999888", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync("new_oauth@devradar.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByUserNameAsync("new_oauth_dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _jwtServiceMock.Setup(j => j.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResponseDto { AccessToken = "new_oauth_access", RefreshToken = "new_oauth_refresh" });

        var command = new OAuthLoginCommand
        {
            Provider = "github",
            Code = "new_user_code"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("new_oauth_access", result.AccessToken);
        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "new_oauth@devradar.io" && u.PrimaryProvider == AuthProvider.Github), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidProvider_ShouldThrowBadRequestException()
    {
        var command = new OAuthLoginCommand
        {
            Provider = "unsupported_provider",
            Code = "any_code"
        };

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.Handle(command, CancellationToken.None));
    }
}

